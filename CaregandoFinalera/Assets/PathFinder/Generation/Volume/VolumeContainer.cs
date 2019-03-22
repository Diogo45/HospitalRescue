using System;
using System.Collections.Generic;
using UnityEngine;
using K_PathFinder.GraphGeneration;
using K_PathFinder.VectorInt ;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

namespace K_PathFinder {
    //FIX: CaptureArea create not necesary objects
    //TODO: if it's just one terrain potentialy you can cut GenerateConnections() and make it faster by aprox 0.1 time

    public class VolumeContainer {
        //extra length values cause there is small overhead to call template.length_extra this much times. besides look nicer
        //public for debug purpose only
        public readonly int sizeX, sizeZ;
        NavMeshTemplateRecast template;
        NavmeshProfiler profiler;

        //public List<Volume> volumes = new List<Volume>();
        public Volume[] volumes = new Volume[0];
        public List<VolumeArea> volumeAreas = new List<VolumeArea>();

        float connectionDistance;
        bool doCrouch, doCover, doHalfCover;
        float halfCover, fullCover;
        int volumesAdded = 0;

        //HashSet<VolumePos> nearOsbtacleSet = new HashSet<VolumePos>();
        //HashSet<VolumePos> nearCrouchSet = new HashSet<VolumePos>();

        List<VolumePos> nearObstacle = new List<VolumePos>();
        List<VolumePos> nearCrouch = new List<VolumePos>();
 

        public BattleGrid battleGrid = null; //it's creates there and transfered further to graph. there is just no graph at this moment so it's end up here

        //here is just generated circular patterns
        //dont have much use but potentialy can
        static Dictionary<int, CirclePattern> patterns = new Dictionary<int, CirclePattern>();

        public VolumeContainer(NavMeshTemplateRecast template) {
            this.template = template;
            this.profiler = template.profiler;
            connectionDistance = template.properties.maxStepHeight;
            doCrouch = template.canCrouch;
            doCover = template.doCover;

            if (doCover) {
                fullCover = template.properties.fullCover;
                doHalfCover = template.doHalfCover;
                if (doHalfCover)
                    halfCover = template.properties.halfCover;
            }

            sizeX = template.lengthX_extra;
            sizeZ = template.lengthZ_extra;
        }
        
        //it apply this volume to all existed volumes and add it to the pool
        public void AddVolume(Volume volume) {
            ApplyVolume(volume, true);
        }

        /// <summary>
        /// function to apply volume to existed volumes
        /// </summary>
        /// <param name="volume">volume</param>
        /// <param name="addVolume">
        /// special case. sometimes volume need to be readded to apply it cause if added volumes under existed one then it change shape of already existed volume. 
        /// so there is way sround - just reapply changed volume </param>
        private void ApplyVolume(Volume volume, bool addVolume) {
            if (volume == null)
                return;

            if (sizeX != volume.sizeX || sizeZ != volume.sizeZ) {
                Debug.LogError("volume sizes dont match");
                return;
            }
            if(addVolume)
                volumesAdded++;

            //apply to volume all existed volumes
            bool[][] existance1 = volume.existance;
            float[][] max1 = volume.max;
            float[][] min1 = volume.min;
            Area[][] area1 = volume.area;
            int[][] passability1 = volume.passability;

            HashSet<Volume> reAddMe = new HashSet<Volume>();

            foreach (var applyedVolume in volumes) {
                if (applyedVolume == volume)
                    continue;

                bool[][] existance2 = applyedVolume.existance;
                float[][] max2 = applyedVolume.max;
                float[][] min2 = applyedVolume.min;
                Area[][] area2 = applyedVolume.area;
                int[][] passability2 = applyedVolume.passability;

                bool doCrouch = template.canCrouch;
                float walkableHeight = template.properties.height;
                float crouchHeight = template.properties.crouchHeight;

                Volume vHigh, vLow;
                for (int x = 0; x < sizeX; x++) {
                    for (int z = 0; z < sizeZ; z++) {
                        if (existance1[x][z] == false || existance2[x][z] == false)
                            continue;

                        float max1_v = max1[x][z];
                        float max2_v = max2[x][z];

                        if (max1_v == max2_v) {//here we fix z-fighting. bigger is better if height overlaping                                 
                            if (min1[x][z] > min2[x][z]) {
                                min2[x][z] = Math.Min(min1[x][z], min2[x][z]);
                                passability2[x][z] = Math.Max(passability1[x][z], passability2[x][z]);
                                if (area1[x][z].overridePriority > area2[x][z].overridePriority)
                                    area2[x][z] = area1[x][z];
                                existance1[x][z] = false;
                            }
                            else {
                                min1[x][z] = Math.Min(min1[x][z], min2[x][z]);
                                passability1[x][z] = Math.Max(passability1[x][z], passability2[x][z]);
                                if (area2[x][z].overridePriority > area1[x][z].overridePriority)
                                    area1[x][z] = area2[x][z];
                                existance2[x][z] = false;
                            }
                        }
                        else {
                            if (max1_v > max2_v) {
                                vHigh = volume;
                                vLow = applyedVolume;
                            }
                            else {
                                vHigh = applyedVolume;
                                vLow = volume;
                            }

                            if (vLow.max[x][z] < vHigh.min[x][z]) {
                                float heightDif = Math.Abs(vHigh.min[x][z] - vLow.max[x][z]);

                                //!!!!!!!!!!!!!!!!!!!!
                                if (heightDif < connectionDistance) {// then we remove it cause it cause lots of trouble
                                    vLow.existance[x][z] = false;
                                    vHigh.min[x][z] = vLow.min[x][z];
                                    if (vHigh != volume)
                                        reAddMe.Add(vHigh);
                                }
                                else {
                                    if (doCrouch) {
                                        if (heightDif <= crouchHeight)//if lower than crouch height
                                            vLow.passability[x][z] = (int)K_PathFinder.Passability.Unwalkable;
                                        else if (heightDif < walkableHeight && //if lower than full height
                                            heightDif > crouchHeight && //but highter than crouch height
                                            vLow.passability[x][z] > (int)K_PathFinder.Passability.Slope) //and if it passable at least
                                            vLow.passability[x][z] = (int)K_PathFinder.Passability.Crouchable;

                                    }
                                    else if (heightDif <= walkableHeight) { //just do check passable height
                                        vLow.passability[x][z] = (int)K_PathFinder.Passability.Unwalkable;
                                        //Vector3 l = GetRealMax(x, z, vLow);
                                        //Vector3 h = GetRealMin(x, z, vHigh);
                                        //Debuger_K.AddLine(l,h,Color.blue);
                                        //Debuger_K.AddLabel(SomeMath.MidPoint(l, h), heightDif);                 
                                    }
                                }
                            }
                            else if (vLow.max[x][z] >= vHigh.min[x][z] || vLow.min[x][z] > vHigh.min[x][z]) {
                                vLow.existance[x][z] = false;
                                vHigh.min[x][z] = Math.Min(min1[x][z], min2[x][z]);

                                if (vHigh != volume)
                                    reAddMe.Add(vHigh);
                                continue;
                            }
                        }
                    }
                }
            }

            //resizing array to new size
            if (addVolume) {
                volume.id = volumes.Length;
                Volume[] newVolumes = new Volume[volumes.Length + 1];
                for (int i = 0; i < volumes.Length; i++) {
                    newVolumes[i] = volumes[i];
                }
                newVolumes[volume.id] = volume;
                volumes = newVolumes;

                foreach (var item in volume.containsAreas) {
                    PathFinder.AddAreaHash(item);
                }
            }

            foreach (var v in reAddMe) {
                ApplyVolume(v, false);
            }
        }

        private void RemoveAllAndAddTestVolume() {
            Area defaultArea = PathFinder.getDefaultArea;
            Volume testVolume = new Volume(template.lengthX_extra, template.lengthZ_extra, defaultArea);

            int patternSize = 4;

            for (int x = 0; x < template.lengthX_extra; x++) {
                for (int z = 0; z < template.lengthZ_extra; z++) {
                    int patternZ = (int)(z / (float)patternSize);
                    int patternX = (int)(x / (float)patternSize);

                    bool oddX = (patternX & 2) != 0;
                    bool oddZ = (patternZ & 2) != 0;

                    testVolume.SetVoxel(x, z, 0, defaultArea, oddX | oddZ ? 3 : 0);

                    //Vector3 p = GetRealMax(x, z, testVolume);
                    //Debuger_K.AddLabel(p, (patternX & 2));
                }
            }


            volumes = new Volume[] { testVolume };
        }

        public void DoStuff() {
            //RemoveAllAndAddTestVolume();

            if (volumesAdded == 0) {
                if(profiler != null)
                    profiler.AddLogFormat("zero colliders to process on {0} chunk so we stop do stuff", template.gridPosition.ToString()); 
                return;
            }
            
            if(volumesAdded == 1 && volumesAmount == 1 && volumes[0].terrain) { //just single terrain volume. no need to do anything cool
                if (profiler != null)
                    profiler.AddLog("just single terrain. not even trees. build connections to itself");

                volumes[0].ConnectToItself();

                if (profiler != null)
                    profiler.AddLog("end building connections");
            }
            //else if (volumesAdded == 2 && volumesAmount == 2 && volumes[0].terrain && volumes[1].terrain && volumes[1].trees){//terrain with trees 
            //    if (profiler != null)
            //        profiler.AddLog("just single terrain with trees. now subtract trees volume and build connections");

            //    Volume mainTerrain = volumes[0];
            //    Volume terrainTrees = volumes[1];
            //    mainTerrain.Subtract(terrainTrees);
            //    mainTerrain.ConnectToItself();
            //    mainTerrain.Override(terrainTrees);
            //    volumes.Remove(terrainTrees);

            //    if (profiler != null)
            //        profiler.AddLog("end building connections");
            //}
            else {//lots of colliders
                if (profiler != null)
                    profiler.AddLogFormat("start merge volumes. now volumes: {0}", volumesAmount);

                MergeVolumes(); //reduce amount of volumes         

                //remove dead volumes and reassign IDs
                List<Volume> newVolumesList = new List<Volume>();
                for (int i = 0; i < volumesAmount; i++) {
                    if (!volumes[i].dead) {                    
                        newVolumesList.Add(volumes[i]); //move to temporary list
                    }
                }
                //vreate new array and reassign IDs
                volumes = new Volume[newVolumesList.Count];
                for (int i = 0; i < newVolumesList.Count; i++) {
                    volumes[i] = newVolumesList[i];
                    volumes[i].id = i;
                }

                if (profiler != null)
                    profiler.AddLogFormat("end merging. now volumes: {0}. start generating connections", volumesAmount);

                GenerateConnections(); //generates connection between voxels so each voxel know their neighbours   

                if (profiler != null)
                    profiler.AddLog("end generating connections, start generating obstacles");
            }

            GenerateObstacles(); //populating obstacles collection so we know where is obstacles are

            if (profiler != null)
                profiler.AddLog("end generating obstacles");

            //create jump spots
            if (template.canJump) {
                if (profiler != null)
                    profiler.AddLog("agent can jump. start capturing areas for jump");

                int sqrArea = template.agentRagius * template.agentRagius;
                int doubleAreaSqr = sqrArea + sqrArea + 2; //plus some extra

                foreach (var nearObstaclePos in nearObstacle) {
                    if (volumes[nearObstaclePos.volume].passability[nearObstaclePos.x][nearObstaclePos.z] >= (int)K_PathFinder.Passability.Crouchable &&
                        volumes[nearObstaclePos.volume].GetState(nearObstaclePos.x, nearObstaclePos.z, VoxelState.InterconnectionAreaflag) == false)
                        CaptureArea(nearObstaclePos, sqrArea, doubleAreaSqr, true, AreaType.Jump);
                }

                if (profiler != null)
                    profiler.AddLog("end capturing areas for jump");
            }

            if (doCover) {
                if (profiler != null)
                    profiler.AddLog("agent can cover. start generating covers");

                //important to check it before growth
                GenerateCovers(template.agentRagius + template.coverExtraSamples);

                for (int i = 0; i < volumesAmount; i++) {
                    GenerateCoverMaps(volumes[i]);
                }
      
                //foreach (var v in volumes) {
                //    var t = v.coverType;
                //    for (int x = 0; x < sizeX; x++) {
                //        for (int z = 0; z < sizeZ; z++) {
                //            if(t[x][z] > 0) {
                //                Debuger_K.AddDot(GetRealMax(x, z, v) + (Vector3.up * 0.1f), Color.green);
                //            }
                //        }
                //    }
                //}

                if (profiler != null)
                    profiler.AddLog("end generating covers");
            }

            if (profiler != null)
                profiler.AddLog("start growing obstacles");

            GrowthObstacles(
                nearObstacle,
                template.agentRagius * template.agentRagius,
                (VolumePos vp) => { return volumes[vp.volume].passability[vp.x][vp.z] < (int)K_PathFinder.Passability.Crouchable; },
                (VolumePos vp) => { return volumes[vp.volume].passability[vp.x][vp.z] >= (int)K_PathFinder.Passability.Crouchable; },
                (VolumePos vp) => { volumes[vp.volume].passability[vp.x][vp.z] = (int)K_PathFinder.Passability.Unwalkable; },
                template);

            if (profiler != null)
                profiler.AddLog("end growing obstacles");

            if (doCrouch) {
                if (profiler != null)
                    profiler.AddLog("agent can cover. start generating cover obstacles");

                GrowthObstacles(
                    nearCrouch,
                    template.agentRagius * template.agentRagius,
                    (VolumePos vp) => { return volumes[vp.volume].passability[vp.x][vp.z] == (int)K_PathFinder.Passability.Crouchable; },
                    (VolumePos vp) => { return volumes[vp.volume].passability[vp.x][vp.z] == (int)K_PathFinder.Passability.Walkable; },
                    (VolumePos vp) => { volumes[vp.volume].passability[vp.x][vp.z] = Math.Min((int)K_PathFinder.Passability.Crouchable, volumes[vp.volume].passability[vp.x][vp.z]); },
                    template);

                if (profiler != null)
                    profiler.AddLog("end generating cover obstacles");
            }


            if (template.doBattleGrid) {
                if (profiler != null)
                    profiler.AddLog("agent use battle grid. start creating battle grid");

                BattleGrid();

                if (profiler != null)
                    profiler.AddLog("end creating battle grid");
            }

            if (profiler != null)
                profiler.AddLog("start creating general maps");

            //as an end part thereis some map generated for graph generator
            for (int i = 0; i < volumesAmount; i++) {
                GenerateGenealMaps(volumes[i]);
            }

            if (profiler != null)
                profiler.AddLog("end creating general maps");

#if UNITY_EDITOR
            if (Debuger_K.doDebug && Debuger_K.debugOnlyNavMesh == false) {
                if (profiler != null)
                    profiler.AddLog("start adding volumes to debuger");

                Debuger_K.AddVolumes(template, this);

                if (profiler != null)
                    profiler.AddLog("end adding volumes to debuger");
            }
#endif
        }

        //reduce amount of volumes
        private void MergeVolumes() {
            Dictionary<int, HashSet<int>> banned = new Dictionary<int, HashSet<int>>(); //id, banned id's

            foreach (var volume in volumes) {
                HashSet<int> hs = new HashSet<int>();
                banned.Add(volume.id, hs);
                hs.Add(volume.id);//cause trying to merge with itself are dum-dum     
                if (volume.dead == false)
                    volume.CreateConnectionsArray();//to create array for storing connections data
            }

            foreach (var volume in volumes) {
                if(volume.terrain | volume.dead)//cause it's just waiste of time
                    foreach (var banHashSet in banned.Values) 
                        banHashSet.Add(volume.id);
            }

            while (true) {
                foreach (var curVolume in volumes) {
                    if (curVolume.dead)
                        continue;

                    bool[][] curE = curVolume.existance;

                    foreach (var otherVolume in volumes) {
                        if (otherVolume.dead || banned[curVolume.id].Contains(otherVolume.id))
                            continue;

                        bool[][] otherE = otherVolume.existance;

                        for (int x = 0; x < sizeX; x++) {
                            for (int z = 0; z < sizeZ; z++) {
                                //overlaping
                                if (curE[x][z] && otherE[x][z]) {
                                    banned[curVolume.id].Add(otherVolume.id);
                                    banned[otherVolume.id].Add(curVolume.id);
                                    goto SKIP;
                                }
                            }
                        }

                        //layer not skipped mean we connect two layers
                        for (int x = 0; x < sizeX; x++) {
                            for (int z = 0; z < sizeZ; z++) {
                                if (otherE[x][z])
                                    curVolume.OverrideVoxel(x, z, otherVolume.max[x][z], otherVolume.min[x][z], otherVolume.area[x][z], otherVolume.passability[x][z]);
                            }
                        }

                        foreach (var item in otherVolume.containsAreas)
                            curVolume.containsAreas.Add(item);

                        otherVolume.dead = true;

                        //nested loop exit label
                        SKIP:
                        {
                            continue;
                        }
                    }
                }
                break;
            }
        }

        //generates connection between voxels so each voxel know their neighbours        
        private void GenerateConnections() {
            //reminder: volume.id equals to volume index in volumes list
            int volumesCount = volumesAmount;//cause little overhead. we took this value too much times in code below

            //x
            for (int x = 0; x < sizeX - 1; x++) {
                for (int z = 0; z < sizeZ; z++) {
                    for (int curVolumeIndex = 0; curVolumeIndex < volumesCount; curVolumeIndex++) {
                        if (volumes[curVolumeIndex].existance[x][z] == false)
                            continue;

                        float curMax = volumes[curVolumeIndex].max[x][z];
                        float closestStep = float.MaxValue;
                        int closestVolume = 0;

                        for (int otherVolumeIndex = 0; otherVolumeIndex < volumesCount; otherVolumeIndex++) {    
                            if (volumes[otherVolumeIndex].existance[x + 1][z] == false)
                                continue;

                            float curStep = Math.Abs(curMax - volumes[otherVolumeIndex].max[x + 1][z]);

                            if (curStep < closestStep) {
                                closestStep = curStep;
                                closestVolume = otherVolumeIndex;
                            }
                        }

                        if (closestStep <= connectionDistance) {
                            volumes[curVolumeIndex].connections[(int)Directions.xPlus][x][z] = closestVolume;
                            volumes[closestVolume].connections[(int)Directions.xMinus][x + 1][z] = curVolumeIndex;
                            //SetConnection(x, z, curVolumeIndex, Directions.xPlus, closestVolume);
                        }
                    }
                }
            }

            //z
            for (int x = 0; x < sizeX; x++) {
                for (int z = 0; z < sizeZ - 1; z++) {
                    for (int curVolumeIndex = 0; curVolumeIndex < volumesCount; curVolumeIndex++) {            
                        if (volumes[curVolumeIndex].existance[x][z] == false)
                            continue;

                        float curMax = volumes[curVolumeIndex].max[x][z];
                        float closestStep = float.MaxValue;
                        int closestVolume = 0;

                        for (int otherVolumeIndex = 0; otherVolumeIndex < volumesCount; otherVolumeIndex++) {                 
                            if (volumes[otherVolumeIndex].existance[x][z + 1] == false)
                                continue;

                            float curStep = Math.Abs(curMax - volumes[otherVolumeIndex].max[x][z + 1]);

                            if (curStep < closestStep) {
                                closestStep = curStep;
                                closestVolume = otherVolumeIndex;
                            }
                        }

                        if (closestStep <= connectionDistance) {
                            volumes[curVolumeIndex].connections[(int)Directions.zPlus][x][z] = closestVolume;
                            volumes[closestVolume].connections[(int)Directions.zMinus][x][z + 1] = curVolumeIndex;
                            //SetConnection(x, z, curVolumeIndex, Directions.zPlus, closestVolume);
                        }
                    }
                }
            }
        }

        //populating obstacles collection so we know where is obstacles are
        private void GenerateObstacles() {
            //cause kinda long :D
            int crouchVal = (int)K_PathFinder.Passability.Crouchable;
            int walkVal = (int)K_PathFinder.Passability.Walkable;
            
            foreach (var volume in volumes) {
                var existance = volume.existance;
                var passability = volume.passability;
                int id = volume.id;

                for (int x = 1; x < sizeX - 1; x++) {
                    for (int z = 0; z < sizeZ; z++) {
                        if (existance[x][z] == false || passability[x][z] < crouchVal)
                            continue;

                        int value = volume.connections[(int)Directions.xPlus][x][z];

                        if (value == -1 || volumes[value].passability[x + 1][z] < crouchVal) {
                            volume.SetState(x, z, VoxelState.NearObstacle, true);
                            //nearOsbtacleSet.Add(new VolumePos(id, x, z));
                        }

                        value = volume.connections[(int)Directions.xMinus][x][z];
                        if (value == -1 || volumes[value].passability[x - 1][z] < crouchVal) {
                            volume.SetState(x, z, VoxelState.NearObstacle, true);
                            //nearOsbtacleSet.Add(new VolumePos(id, x, z));
                        }

                        if (doCrouch && passability[x][z] == walkVal) {
                            value = volume.connections[(int)Directions.xPlus][x][z];
                            if (value != -1 && volumes[value].passability[x + 1][z] == crouchVal) {
                                volume.SetState(x, z, VoxelState.NearCrouch, true);
                                this.nearCrouch.Add(new VolumePos(id, x, z));
                            }

                            value = volume.connections[(int)Directions.xMinus][x][z];
                            if (value != -1 && volumes[value].passability[x - 1][z] == crouchVal) {
                                volume.SetState(x, z, VoxelState.NearCrouch, true);
                                this.nearCrouch.Add(new VolumePos(id, x, z));
                            }
                        }
                    }
                }

                for (int x = 0; x < sizeX; x++) {
                    for (int z = 1; z < sizeZ - 1; z++) {
                        if (existance[x][z] == false || passability[x][z] < crouchVal)
                            continue;

                        int value = volume.connections[(int)Directions.zPlus][x][z];

                        if (value == -1 || volumes[value].passability[x][z + 1] < crouchVal) {
                            volume.SetState(x, z, VoxelState.NearObstacle, true);
                            //nearOsbtacleSet.Add(new VolumePos(id, x, z));
                        }

                        value = volume.connections[(int)Directions.zMinus][x][z];
                        if (value == -1 || volumes[value].passability[x][z - 1] < crouchVal) {
                            volume.SetState(x, z, VoxelState.NearObstacle, true);
                            //nearOsbtacleSet.Add(new VolumePos(id, x, z));
                        }

                        if (doCrouch && passability[x][z] == walkVal) {
                            value = volume.connections[(int)Directions.zPlus][x][z];
                            if (value != -1 && volumes[value].passability[x][z + 1] == crouchVal) {
                                volume.SetState(x, z, VoxelState.NearCrouch, true);
                                this.nearCrouch.Add(new VolumePos(id, x, z));
                            }

                            value = volume.connections[(int)Directions.zMinus][x][z];
                            if (value != -1 && volumes[value].passability[x][z - 1] == crouchVal) {
                                volume.SetState(x, z, VoxelState.NearCrouch, true);
                                this.nearCrouch.Add(new VolumePos(id, x, z));
                            }
                        }
                    }
                }

                //totaly worth it
                if (doCrouch) {
                    for (int x = 0; x < sizeX; x++) {
                        for (int z = 1; z < sizeZ - 1; z++) {
                            if (existance[x][z] == false || passability[x][z] < crouchVal)
                                continue;
                            if (volume.GetState(x, z, VoxelState.NearObstacle))
                                nearObstacle.Add(new VolumePos(id, x, z));
                            if (volume.GetState(x, z, VoxelState.NearCrouch))
                                nearCrouch.Add(new VolumePos(id, x, z));
                        }
                    }
                }
                else {
                    for (int x = 0; x < sizeX; x++) {
                        for (int z = 1; z < sizeZ - 1; z++) {
                            if (existance[x][z] == false || passability[x][z] < crouchVal)
                                continue;
                            if (volume.GetState(x, z, VoxelState.NearObstacle))
                                nearObstacle.Add(new VolumePos(id, x, z));
                        }
                    }
                }
            }

            //fix obstacle set
            //needed only if there covers generated
            if (template.doCover) {
                if (profiler != null)
                    profiler.AddLog("agent do cover so we need fix nearOsbtacleSet. start fixing");

                //int are number of neighbour.
                //at least 2 needed to make changes. 
                //to avoid extra changes start value is 5 cause 4 neighbours is max number

                Dictionary<VolumePos, int> dictionary = new Dictionary<VolumePos, int>();
                foreach (var nearObstacle in nearObstacle) {
                    dictionary.Add(nearObstacle, 5);
                }

                foreach (var nearObstacle in nearObstacle) {
                    for (int i = 0; i < 4; i++) {
                        VolumePos result;
                        if(TryGetLeveled(nearObstacle, (Directions)i, out result)){
                            if (dictionary.ContainsKey(result))
                                dictionary[result]++;
                            else
                                dictionary.Add(result, 1);
                        }
                    }
                }

                foreach (var extendedObstacle in dictionary) {
                    if (extendedObstacle.Value < 5 && extendedObstacle.Value > 1) {
                        VolumePos pos = extendedObstacle.Key;
                        nearObstacle.Add(pos);
                        volumes[pos.volume].SetState(pos.x, pos.z, VoxelState.NearObstacle, true);
                    }
                }

                if (profiler != null)
                    profiler.AddLog("end fixing");
            }
        }

        private void GenerateGenealMaps(Volume volume) {
            AreaPassabilityHashData hashData = template.hashData;
            int minPass = (int)K_PathFinder.Passability.Crouchable;
            int extra = template.extraOffset;

            //result values
            bool[][] heightInterest = new bool[sizeX][];
            int[][] hashMap = new int[sizeX][];


            for (int x = 0; x < sizeX; x++) {
                heightInterest[x] = new bool[sizeZ];
                hashMap[x] = new int[sizeZ];
            }

            volume.heightInterest = heightInterest;
            volume.hashMap = hashMap;

            //used values
            bool[][] existance = volume.existance;
            int[][] passability = volume.passability;
            Area[][] area = volume.area;

            for (int x = extra; x < sizeX - extra; x++) {
                for (int z = extra; z < sizeZ - extra; z++) {
                    if (existance[x][z]) {
                        if (passability[x][z] >= minPass) {
                            hashMap[x][z] = hashData.GetAreaHash(area[x][z], (Passability)passability[x][z]);
                            heightInterest[x][z] = true;
                        }           
                    }
                }
            }
        }
        
        public void GenerateCoverMaps(Volume volume) {
            int minPass = (int)K_PathFinder.Passability.Walkable;
            int extra = template.extraOffset;

            //used values
            bool[][] existance = volume.existance;
            int[][] passability = volume.passability;
            int[][] hashMap = new int[sizeX][];
            bool[][] heightInterest = new bool[sizeX][];
            for (int x = 0; x < sizeX; x++) {
                hashMap[x] = new int[sizeZ];
                heightInterest[x] = new bool[sizeZ];
            }
            
            volume.coverHeightInterest = heightInterest;
            volume.coverHashMap = hashMap;

            for (int x = extra; x < sizeX - extra; x++) {
                for (int z = extra; z < sizeZ - extra; z++) {
                    if (existance[x][z] == false ||
                       passability[x][z] < minPass ||
                       volume.GetState(x, z, VoxelState.CoverAreaNegtiveFlag) ||
                       volume.GetState(x, z, VoxelState.NearObstacle)) {
                        hashMap[x][z] = -1;
                    }
                    else {
                        hashMap[x][z] = MarchingSquaresIterator.COVER_HASH;
                        heightInterest[x][z] = true;
                    }
                }
            }
        }

        //this ugly function are shift voxels in growth pattern from obstacles and crouch areas
        private void GrowthObstacles(
            List<VolumePos> origins,
            float sqrDistance,
            Func<VolumePos, bool> positive,
            Func<VolumePos, bool> growTo,
            Action<VolumePos> grow,
            NavMeshTemplateRecast template) {

            Dictionary<VolumePos, VolumePos> originsDistionary = new Dictionary<VolumePos, VolumePos>();//position of value, position of origin
            HashSet<VolumePos> lastIteration = new HashSet<VolumePos>(origins);

            foreach (var item in origins) {
                originsDistionary[item] = item;
                grow(item);
            }

            Dictionary<VolumePos, HashSet<VolumePos>> borderDictionary = new Dictionary<VolumePos, HashSet<VolumePos>>();

            while (true) {
                foreach (var lastIterationPos in lastIteration) {
                    for (int i = 0; i < 4; i++) {
                        VolumePos value;
                        if (TryGetLeveled(lastIterationPos, (Directions)i, out value) == false
                            || positive(value) | growTo(value) == false)
                            continue;

                        if (borderDictionary.ContainsKey(value) == false)
                            borderDictionary.Add(value, new HashSet<VolumePos>());

                        borderDictionary[value].Add(originsDistionary[lastIterationPos]);
                    }
                }

                HashSet<VolumePos> newIteration = new HashSet<VolumePos>();

                foreach (var curPoint in borderDictionary) {
                    VolumePos? closest = null;
                    int dist = int.MaxValue;

                    foreach (var root in curPoint.Value) {
                        int curDist = SomeMath.SqrDistance(curPoint.Key.x, curPoint.Key.z, root.x, root.z);
                        if (curDist < sqrDistance & curDist < dist) {
                            dist = curDist;
                            closest = root;
                        }
                    }
                    if (closest.HasValue) {
                        newIteration.Add(curPoint.Key);
                        originsDistionary.Add(curPoint.Key, closest.Value);
                        grow(curPoint.Key);
                        //Debuger3.AddLine(curPoint.Key.GetRealMax(template), closest.GetRealMax(template), Color.red);
                    }
                }

                if (newIteration.Count == 0)
                    break;

                lastIteration = newIteration;
                borderDictionary.Clear();
            }
        }

        public VolumeArea CaptureArea(VolumePos basePos, int sqrArea, int sqrOffset, bool addAreaFlag, AreaType areaType, bool debug = false) {
            VolumeArea areaObject = new VolumeArea(GetRealMax(basePos), areaType);

            HashSet<VolumePos> area = new HashSet<VolumePos>();
            area.Add(basePos);

            HashSet<VolumePos> lastAreaIteration = new HashSet<VolumePos>();
            lastAreaIteration.Add(basePos);

            HashSet<VolumePos> doubleArea = new HashSet<VolumePos>();

            while (true) {
                if (lastAreaIteration.Count == 0)
                    break;

                HashSet<VolumePos> newAxisIteration = new HashSet<VolumePos>();

                foreach (var lastIterationPos in lastAreaIteration) {
                    //captured only further positions
                    int curDistance = SomeMath.SqrDistance(basePos.x, basePos.z, lastIterationPos.x, lastIterationPos.z);

                    for (int i = 0; i < 4; i++) {
                        VolumePos neighbour;
                        if (TryGetLeveled(lastIterationPos, (Directions)i, out neighbour) == false)
                            continue;

                        int neighbourDistance = SomeMath.SqrDistance(basePos.x, basePos.z, neighbour.x, neighbour.z);

                        if (neighbourDistance >= curDistance && neighbourDistance <= sqrOffset && doubleArea.Add(neighbour)) {
                            newAxisIteration.Add(neighbour);
                            if (neighbourDistance < sqrArea)
                                area.Add(neighbour);
                        }
                    }
                }
                lastAreaIteration = newAxisIteration;
            }

            foreach (var pos in area) {
                VolumePosSmall s = new VolumePosSmall(pos);
                HashSet<VolumeArea> a;
                Volume v = volumes[pos.volume];
                if (v.volumeArea.TryGetValue(s, out a) == false) {
                    a = new HashSet<VolumeArea>();
                    v.volumeArea.Add(s, a);
                }
                a.Add(areaObject);
                v.SetState(pos.x, pos.z, VoxelState.InterconnectionArea, true);
            }

            if (addAreaFlag) {
                foreach (var pos in doubleArea) {
                    volumes[pos.volume].SetState(pos.x, pos.z, VoxelState.InterconnectionAreaflag, true);
                }
            }
#if UNITY_EDITOR
            if (debug) {
                Vector3 basePosReal = GetRealMax(basePos);
                foreach (var item in area) {
                    Debuger_K.AddLine(basePosReal, GetRealMax(item), Color.cyan);
                }

                Debuger_K.AddLabel(basePosReal, string.Format("type: {0}\ncount: {1}\nsqr area: {2}\nsqr offset: {3}",
                    areaType,
                    area.Count,
                    sqrArea, sqrOffset));
            }
#endif


            volumeAreas.Add(areaObject);
            return areaObject;
        }

        /// some more spaghetti code. used to rate fragments from 0 to 2 in cover capability
        /// 0 is no cover, 1 is half cover, 2 is full cover. goto used to exit nested loops
        private void GenerateCovers(int sampleDistance) {       
            sampleDistance = Math.Max(2, sampleDistance);

            var pattern = GetPattern(sampleDistance);
            int patternSize = pattern.size;
            int patternRadius = pattern.radius - 1;
            bool[][] patternGrid = pattern.pattern;

            Volume volume;

            int minIndex = template.extraOffset;
            int maxIndexX = template.lengthX_extra - template.extraOffset;
            int maxIndexZ = template.lengthZ_extra - template.extraOffset;

            foreach (var v in volumes) {
                int[][] coverType = new int[sizeX][];
                for (int x = 0; x < sizeX; x++) {
                    coverType[x] = new int[sizeZ];
                }
                v.coverType = coverType;
            }                

            foreach (var item in nearObstacle) {
                volume = volumes[item.volume];
                if (volume.dead || item.x < minIndex || item.z < minIndex || item.x > maxIndexX || item.z > maxIndexZ || volume.passability[item.x][item.z] < (int)K_PathFinder.Passability.Crouchable)
                    continue;

                int baseX = item.x;
                int baseZ = item.z;
                float baseMax = volume.max[baseX][baseZ];

                for (int x_pattern = 0; x_pattern < patternSize; x_pattern++) {
                    for (int z_pattern = 0; z_pattern < patternSize; z_pattern++) {
                        if (patternGrid[x_pattern][z_pattern] == false)//we have base and we have to check this space to cover
                            continue;

                        int checkX = item.x - patternRadius + x_pattern;

                        if (checkX < 0 || checkX >= sizeX)
                            continue;

                        int checkZ = item.z - patternRadius + z_pattern;

                        if (checkZ < 0 || checkZ >= sizeZ)
                            continue;

                        #region debug
                        //Vector3 p1 = GetRealMax(baseX, baseZ, volume);
                        //Vector3 p2 = template.realPosition
                        //        + (new Vector3(checkX, 0, checkZ) * template.fragmentSize)
                        //        + (template.halfFragmentOffset)
                        //        + new Vector3(0, p1.y, 0);

                        //Debuger3.AddLine(p1, p2, Color.red);
                        #endregion

                        int cover = 0;

                        for (int i = 0; i < volumes.Length; i++) {
                            if (volumes[i].GetState(checkX, checkZ, VoxelState.Tree)) {
                                volume.SetState(baseX, baseZ, VoxelState.NearTree, true);
                                volume.coverType[baseX][baseZ] = 0;
                                goto CONTINUE;
                            }

                            if (volumes[i].min[checkX][checkZ] > baseMax)
                                continue;

                            float dif = volumes[i].max[checkX][checkZ] - baseMax;

                            if (doHalfCover && dif > halfCover)
                                cover = 1;

                            if (dif > fullCover) {
                                volume.coverType[baseX][baseZ] = 2;
                                goto CONTINUE;
                            }
                        }
                        volume.coverType[baseX][baseZ] = Math.Max(volume.coverType[baseX][baseZ], cover);
                        
                    }
                }
                CONTINUE:
                {
                    continue;
                }
            }
        }

        /// <summary>
        /// amount of volumes stored in container.
        /// this acessor exist cause i couple of times change it to array then to list then to array again. tired to change Count/Length everywhere
        /// </summary>
        public int volumesAmount {
            get { return volumes.Length; }
        }

        //have no use but it's readable and represent how it should work at least
        //was used in GenerateConnections() function
        public void SetConnection(int x, int z, int volume, Directions direction, int value) {
            volumes[volume].connections[(int)direction][x][z] = value;

            switch (direction) {
                case Directions.xPlus:
                    volumes[value].connections[(int)Directions.xMinus][x + 1][z] = volume;
                    break;
                case Directions.xMinus:
                    volumes[value].connections[(int)Directions.xPlus][x - 1][z] = volume;
                    break;
                case Directions.zPlus:
                    volumes[value].connections[(int)Directions.zMinus][x][z + 1] = volume;
                    break;
                case Directions.zMinus:
                    volumes[value].connections[(int)Directions.zPlus][x][z - 1] = volume;
                    break;
            }
        }

        // most of this code are about proper alighment in world
        // it's calculate all grid shifts in world space and then shift this grid to proper distance
        // (actualy code is far from optimal but it's not that bad so i never touch it)
        private void BattleGrid() {
            int density = template.battleGridDensity;
            var chunkPos = template.gridPosition;

            int fPosX = template.lengthX_central * chunkPos.x;
            int fPosZ = template.lengthZ_central * chunkPos.z;

            int lastGridLeftX = fPosX - (fPosX / template.battleGridDensity * template.battleGridDensity);
            int lastGridLeftZ = fPosZ - (fPosZ / template.battleGridDensity * template.battleGridDensity);

            int lastChunkLeftX = lastGridLeftX == 0 ? 0 : template.battleGridDensity - lastGridLeftX;
            int lastChunkLeftZ = lastGridLeftZ == 0 ? 0 : template.battleGridDensity - lastGridLeftZ;

            if (lastChunkLeftX > template.battleGridDensity) //negative chunk position
                lastChunkLeftX -= template.battleGridDensity;

            if (lastChunkLeftZ > template.battleGridDensity) //negative chunk position
                lastChunkLeftZ -= template.battleGridDensity;

            int lengthX = ((template.lengthX_central - lastChunkLeftX - 1) / template.battleGridDensity) + 1;
            int lengthZ = ((template.lengthZ_central - lastChunkLeftZ - 1) / template.battleGridDensity) + 1;

            int offsetX = template.extraOffset + lastChunkLeftX;
            int offsetZ = template.extraOffset + lastChunkLeftZ;

            Dictionary<VolumePos, VolumePos?[]> gridDic = new Dictionary<VolumePos, VolumePos?[]>();
            Dictionary<VolumePos, BattleGridPoint> bgpDic = new Dictionary<VolumePos, BattleGridPoint>();

            foreach (var volume in volumes) {
                for (int x = 0; x < lengthX; x++) {
                    for (int z = 0; z < lengthZ; z++) {
                        //volume pos 
                        VolumePos curPos = new VolumePos(volume.id, offsetX + (x * density), offsetZ + (z * density));

                        if (volume.Exist(curPos) == false || volume.Passability(curPos) < K_PathFinder.Passability.Crouchable)
                            continue;

                        gridDic.Add(curPos, new VolumePos?[4]);
                        bgpDic.Add(curPos, new BattleGridPoint(GetRealMax(curPos), volume.Passability(curPos), new VectorInt.Vector2Int(x, z)));
                    }
                }
            }

            //x
            foreach (var volume in volumes) {
                for (int x = 0; x < lengthX - 1; x++) {
                    for (int z = 0; z < lengthZ; z++) {
                        VolumePos curPos = new VolumePos(volume.id, offsetX + (x * density), offsetZ + (z * density));     //volume pos 

                        if (volume.Exist(curPos) == false || volume.Passability(curPos) < K_PathFinder.Passability.Crouchable)
                            continue;

                        VolumePos curChangedPos = curPos;

                        for (int i = 0; i < density; i++) {
                            if (TryGetLeveled(curChangedPos, Directions.xPlus, out curChangedPos) == false || Passability(curChangedPos) < K_PathFinder.Passability.Crouchable)
                                goto NEXT;//exit from nexted loop
                        }
                        gridDic[curPos][(int)Directions.xPlus] = curChangedPos;
                        gridDic[curChangedPos][(int)Directions.xMinus] = curPos;

                        NEXT: { continue; }
                    }
                }
            }


            //z
            foreach (var volume in volumes) {
                for (int x = 0; x < lengthX; x++) {
                    for (int z = 0; z < lengthZ - 1; z++) {
                        VolumePos curPos = new VolumePos(volume.id, offsetX + (x * density), offsetZ + (z * density));     //volume pos 

                        if (volume.Exist(curPos) == false || volume.Passability(curPos) < K_PathFinder.Passability.Crouchable)
                            continue;

                        VolumePos curChangedPos = curPos;

                        for (int i = 0; i < density; i++) {
                            if (TryGetLeveled(curChangedPos, Directions.zPlus, out curChangedPos) == false || Passability(curChangedPos) < K_PathFinder.Passability.Crouchable)
                                goto NEXT;//exit from nexted loop
                        }
                        gridDic[curPos][(int)Directions.zPlus] = curChangedPos;
                        gridDic[curChangedPos][(int)Directions.zMinus] = curPos;

                        NEXT: { continue; }
                    }
                }
            }


            //transfer connections
            foreach (var fragKeyValue in gridDic) {
                var bgp = bgpDic[fragKeyValue.Key];
                var ar = fragKeyValue.Value;

                for (int i = 0; i < 4; i++) {
                    if (ar[i].HasValue)
                        bgp.neighbours[i] = bgpDic[ar[i].Value];
                }
            }

            battleGrid = new BattleGrid(lengthX, lengthZ, bgpDic.Values);

#if UNITY_EDITOR
            //debug battle grid
            if (PFDebuger.Debuger_K.doDebug) {
                //since it's just bunch of lines i transfer it as list of vector 3
                List<Vector3> debugList = new List<Vector3>();
                foreach (var fragKeyValue in gridDic) {
                    var f = fragKeyValue.Key;
                    var v = fragKeyValue.Value;

                    for (int i = 0; i < 4; i++) {
                        if (v[i].HasValue) {
                            debugList.Add(bgpDic[f].positionV3);
                            debugList.Add(bgpDic[v[i].Value].positionV3);
                        }
                    }
                }
                //Debuger3.AddBattleGridConnection(template.chunk, template.properties, debugList);
            }
#endif
        }

        //public for debug purpose
        //max
        public Vector3 GetRealMax(int x, int z, Volume volume) {
            return template.realOffsetedPosition
                    + (new Vector3(x, 0, z) * template.voxelSize)
                    + (template.halfFragmentOffset)
                    + new Vector3(0, volume.max[x][z], 0);
        }
        public Vector3 GetRealMax(int x, int z, int volume) {
            return template.realOffsetedPosition
                    + (new Vector3(x, 0, z) * template.voxelSize)
                    + (template.halfFragmentOffset)
                    + new Vector3(0, volumes[volume].max[x][z], 0);
        }
        public Vector3 GetRealMax(VolumePos pos) {
            return template.realOffsetedPosition
                    + (new Vector3(pos.x, 0, pos.z) * template.voxelSize)
                    + (template.halfFragmentOffset)
                    + new Vector3(0, volumes[pos.volume].max[pos.x][pos.z], 0);
        }
        //min
        public Vector3 GetRealMin(int x, int z, Volume volume) {
            return template.realOffsetedPosition
                    + (new Vector3(x, 0, z) * template.voxelSize)
                    + (template.halfFragmentOffset)
                    + new Vector3(0, volume.min[x][z], 0);
        }
        public Vector3 GetRealMin(int x, int z, int volume) {
            return template.realOffsetedPosition
                    + (new Vector3(x, 0, z) * template.voxelSize)
                    + (template.halfFragmentOffset)
                    + new Vector3(0, volumes[volume].min[x][z], 0);
        }
        public Vector3 GetRealMin(VolumePos pos) {
            return template.realOffsetedPosition
                    + (new Vector3(pos.x, 0, pos.z) * template.voxelSize)
                    + (template.halfFragmentOffset)
                    + new Vector3(0, volumes[pos.volume].min[pos.x][pos.z], 0);
        }

        public bool TryGetLeveled(Volume volume, int x, int z, Directions direction, out int result) {
            result = volume.connections[(int)direction][x][z];
            return result != -1;
        }
        public bool TryGetLeveled(Volume volume, int x, int z, Directions direction, out VolumePos result) {
            int connection = volume.connections[(int)direction][x][z];
            if (connection != -1) {
                switch (direction) {
                    case Directions.xPlus:
                        result = new VolumePos(connection, x + 1, z);
                        break;
                    case Directions.xMinus:
                        result = new VolumePos(connection, x - 1, z);
                        break;
                    case Directions.zPlus:
                        result = new VolumePos(connection, x, z + 1);
                        break;
                    case Directions.zMinus:
                        result = new VolumePos(connection, x, z - 1);
                        break;
                    default:
                        result = new VolumePos();
                        break;
                }
                return true;
            }
            else {
                result = new VolumePos();
                return false;
            }
        }
        public bool TryGetLeveled(VolumePos volumePos, Directions direction, out int result) {
            return TryGetLeveled(volumes[volumePos.volume], volumePos.x, volumePos.z, direction, out result);
        }
        public bool TryGetLeveled(VolumePos volumePos, Directions direction, out VolumePos result) {
            return TryGetLeveled(volumes[volumePos.volume], volumePos.x, volumePos.z, direction, out result);
        }
        public bool TryGetLeveled(int volume, int x, int z, Directions direction, out int result) {
            return TryGetLeveled(volumes[volume], x, z, direction, out result);
        }
        public bool TryGetLeveled(int volume, int x, int z, Directions direction, out VolumePos result) {
            return TryGetLeveled(volumes[volume], x, z, direction, out result);
        }

        private Passability Passability(VolumePos pos) {
            return volumes[pos.volume].Passability(pos);
        }

        public bool GetClosestPos(Vector3 pos, out VolumePos closestPos) {
            float fragmentSize = template.voxelSize;

            Vector3 ajustedPos = pos - template.realOffsetedPosition;
            int x = Mathf.RoundToInt((ajustedPos.x - (fragmentSize * 0.5f)) / fragmentSize);
            int z = Mathf.RoundToInt((ajustedPos.z - (fragmentSize * 0.5f)) / fragmentSize);

            float curDist = float.MaxValue;
            VolumePos? curPos = null;

            //sample 3x3 an all grid
            foreach (var volume in volumes) {
                GetClosestPosReadableShortcut(ref pos, ref curDist, ref curPos, volume, x - 1, z - 1);
                GetClosestPosReadableShortcut(ref pos, ref curDist, ref curPos, volume, x - 1, z);
                GetClosestPosReadableShortcut(ref pos, ref curDist, ref curPos, volume, x - 1, z + 1);

                GetClosestPosReadableShortcut(ref pos, ref curDist, ref curPos, volume, x, z - 1);
                GetClosestPosReadableShortcut(ref pos, ref curDist, ref curPos, volume, x, z);
                GetClosestPosReadableShortcut(ref pos, ref curDist, ref curPos, volume, x, z + 1);

                GetClosestPosReadableShortcut(ref pos, ref curDist, ref curPos, volume, x + 1, z - 1);
                GetClosestPosReadableShortcut(ref pos, ref curDist, ref curPos, volume, x + 1, z);
                GetClosestPosReadableShortcut(ref pos, ref curDist, ref curPos, volume, x + 1, z + 1);
            }

            if (curPos.HasValue) {
                closestPos = curPos.Value;
                return true;
            }
            else {
                closestPos = new VolumePos();
                return false;
            }
        }

        private void GetClosestPosReadableShortcut(ref Vector3 pos, ref float curDist, ref VolumePos? curPos, Volume volume, int x, int z) {
            if (volume.existance[x][z]) {
                float dist = SomeMath.SqrDistance(GetRealMax(x, z, volume.id), pos);
                if (dist < curDist) {
                    curPos = new VolumePos(volume.id, x, z);
                    curDist = dist;
                }
            }
        }

        #region patterns
        private static CirclePattern GetPattern(int radius) {         
            CirclePattern result;
            lock (patterns) {
                if (!patterns.TryGetValue(radius, out result)) {
                    result = new CirclePattern(radius);
                    patterns.Add(radius, result);
                }
            }
            return result;
        }
        private class CirclePattern {
            public int radius;
            public bool[][] pattern;
            public int size;

            public CirclePattern(int radius) {
                this.radius = radius;
                size = radius + radius - 1;
                int sqrRadius = (radius - 1) * (radius - 1);
                pattern = new bool[size][];
                for (int x = 0; x < size; x++) {
                    pattern[x] = new bool[size];
                    for (int y = 0; y < size; y++) {
                        pattern[x][y] = SomeMath.SqrDistance(x, y, radius - 1, radius - 1) <= sqrRadius;
                    }
                }
            }
        }
        #endregion
    }
}
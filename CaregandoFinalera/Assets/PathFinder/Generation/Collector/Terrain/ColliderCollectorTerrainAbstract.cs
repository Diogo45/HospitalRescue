using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using K_PathFinder.VectorInt ;
using System;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

namespace K_PathFinder {
    public abstract class TerrainCollectorAbstract : ColliderCollectorTerrainAbstract {
        protected static Vector3[] fancyVerts;
        protected static int[] fancyTris;

        static TerrainCollectorAbstract() {
            float h = 1f;
            Vector3 fv = new Vector3(1, 0, 1).normalized * 0.5f;

            fancyVerts = new Vector3[8];
            fancyVerts[0] = new Vector3(-0.5f, h, 0);
            fancyVerts[1] = new Vector3(-fv.x, h, fv.z);
            fancyVerts[2] = new Vector3(0, h, 0.5f);
            fancyVerts[3] = new Vector3(fv.x, h, fv.z);
            fancyVerts[4] = new Vector3(0.5f, h, 0);
            fancyVerts[5] = new Vector3(fv.x, h, -fv.z);
            fancyVerts[6] = new Vector3(0, h, -0.5f);
            fancyVerts[7] = new Vector3(-fv.x, h, -fv.z);

            fancyTris = new int[18] {
              6, 4, 5,
              7, 4, 6,
              0, 4, 7,
              1, 4, 0,
              2, 4, 1,
              3, 4, 2, 
            };
        }
           
        public TerrainCollectorAbstract(NavMeshTemplateRecast template, Collider[] colliders) : base(template, colliders) {}
        
        protected override bool IsValid(Collider collider) {
            if (collider == null ||
                !collider.enabled ||
                !template.chunkOffsetedBounds.Intersects(collider.bounds) ||
                collider is TerrainCollider == false ||
                collider.isTrigger)
                return false;

            return !template.IgnoredTagsContains(collider.tag);
        }
         
        // in unity trees can only have capsule colliders so it's easy part
        public List<Bounds> GenerateTreeBounds(Terrain terrain) {
            List<Bounds> treeBounds = new List<Bounds>();
            TerrainData data = terrain.terrainData;
            TreePrototype[] prototypes = data.treePrototypes;
            TreeInstance[] instances = data.treeInstances;

            if (prototypes.Length == 0 | instances.Length == 0)
                return treeBounds;

            Transform terrainTransform = terrain.transform;

            bool[] validPrototype = new bool[prototypes.Length];
            Vector3[] prototypeBaseColiderSize = new Vector3[prototypes.Length];
            Vector3[] prototypeBaseColiderCenter = new Vector3[prototypes.Length];
            //List<TreeInstance>[] sortedByPrototype = new List<TreeInstance>[prototypes.Length];

            float biggestPrototypeRadius = 0;

            for (int i = 0; i < prototypes.Length; i++) {
                validPrototype[i] = IsValidTreePrototype(prototypes[i]);
                CapsuleCollider c = prototypes[i].prefab.GetComponent<Collider>() as CapsuleCollider;
                prototypeBaseColiderSize[i] = new Vector3(c.radius * 2f, c.height, c.radius * 2f);
                prototypeBaseColiderCenter[i] = c.center;
                biggestPrototypeRadius = Math.Max(biggestPrototypeRadius, c.radius);
            }
            
            float terrainX = terrainTransform.position.x;
            float terrainY = terrainTransform.position.y;
            float terrainZ = terrainTransform.position.z;

            float dataSizeX = data.size.x;
            float dataSizeY = data.size.y;
            float dataSizeZ = data.size.z;

            //fancy shmancy
            Bounds chunkBounds = template.chunkOffsetedBounds;
            float minX = chunkBounds.min.x - biggestPrototypeRadius;
            float minZ = chunkBounds.min.z - biggestPrototypeRadius;
            float maxX = chunkBounds.max.x + biggestPrototypeRadius;
            float maxZ = chunkBounds.max.z + biggestPrototypeRadius;

            foreach (var instance in instances) {
                float x = instance.position.x * dataSizeX + terrainX;

                if (x < minX || x > maxX)
                    continue;

                float z = instance.position.z * dataSizeZ + terrainZ;

                if (z < minZ || z > maxZ)
                    continue;

                Vector3 size = prototypeBaseColiderSize[instance.prototypeIndex];
                Vector3 center = prototypeBaseColiderCenter[instance.prototypeIndex];
                float widthScale = instance.widthScale;

                Bounds bounds = new Bounds(
                    new Vector3(
                        x + (center.x * widthScale), 
                        instance.position.y * dataSizeY + terrainY + (center.y * instance.heightScale),
                        z + (center.z * widthScale)), 
                    new Vector3(
                        size.x * widthScale, 
                        size.y * instance.heightScale, 
                        size.z * widthScale));
                
                treeBounds.Add(bounds);
            }

            //foreach (var item in treeBounds) {
            //    Debuger_K.AddRay(item.center, Vector3.up, Color.red);
            //}

            return treeBounds;
        }
        
        private bool IsValidTreePrototype(TreePrototype prototype) {
            if (prototype == null || prototype.prefab == null)
                return false;

            CapsuleCollider cc = prototype.prefab.GetComponent<Collider>() as CapsuleCollider;
            if (cc == null || cc.enabled == false || cc.isTrigger)
                return false;

            return true;
        }

        protected void GenerateTreesMesh(List<Bounds> treeBounds, out Vector3[] verts, out int[] tris) {
            //copy verts cause lots of reasons
            int FVLength = fancyVerts.Length;
            int FTLength = fancyTris.Length;
            int TBCount = treeBounds.Count;

            verts = new Vector3[FVLength * TBCount];
            tris = new int[FTLength * TBCount];

            for (int i = 0; i < TBCount; i++) {
                Bounds bound = treeBounds[i];
                Matrix4x4 m = Matrix4x4.TRS(bound.center, Quaternion.identity, bound.size);

                for (int v = 0; v < FVLength; v++) {
                    verts[(i * FVLength) + v] = m.MultiplyPoint3x4(fancyVerts[v]);
                }

                for (int t = 0; t < FTLength; t++) {
                    tris[(i * FTLength) + t] = fancyTris[t] + (i * FVLength);
                }
            }
        }

        protected Volume CollectTrees(TerrainColliderInfoAbstract terrain) {
            List<Bounds> treeBounds = terrain.treeData;

            if (treeBounds == null || treeBounds.Count == 0)
                return null;

            //Vector3[] trs;
            //int[] ind;
            //GenerateTreesMesh(treeBounds, out trs, out ind);


            //Debuger_K.AddMesh(trs, ind, new Color(1, 0, 0, 0.1f));

            Area defaultArea = PathFinder.getDefaultArea;
            Volume treeVolume = new Volume(template.lengthX_extra, template.lengthZ_extra, defaultArea);
            treeVolume.terrain = true;
            treeVolume.trees = true;

            Bounds chunkBounds = template.chunkOffsetedBounds;

            int l = fancyVerts.Length;
            Vector3[] treeBuffer = new Vector3[l];

            foreach (var bound in treeBounds) {
                if (!chunkBounds.Intersects(bound))
                    continue;

                Matrix4x4 m = Matrix4x4.TRS(bound.center, Quaternion.identity, new Vector3(bound.size.x, bound.size.y * 0.5f, bound.size.z));

                for (int i = 0; i < l; i++) {
                    treeBuffer[i] = m.MultiplyPoint3x4(fancyVerts[i]);
                }

#if UNITY_EDITOR
                if (Debuger_K.doDebug && Debuger_K.debugOnlyNavMesh == false) {
                    Debuger_K.AddTreeCollider(template.gridPosX, template.gridPosZ, template.properties, new Bounds(bound.center, bound.extents), treeBuffer, fancyTris);
                }
#endif

                for (int tris = 0; tris < fancyTris.Length; tris += 3) {
                    base.RasterizeTriangle(
                        treeVolume,
                        treeBuffer[fancyTris[tris]],
                        treeBuffer[fancyTris[tris + 1]],
                        treeBuffer[fancyTris[tris + 2]],
                        template.voxelSize,
                        template.startX_extra, template.endX_extra,
                        template.startZ_extra, template.endZ_extra,
                        defaultArea,
                        Passability.Unwalkable,
                        VoxelState.Terrain,
                        VoxelState.Tree);
                }
            }
            treeVolume.SetVolumeMinimum(-1000f);
            treeVolume.dead = true;
            return treeVolume;
        }

        #region area collection
        //add some values to target TerrainColliderInfoAbstract so later on we can process maps
        protected void SetTerrainSettings(
            TerrainColliderInfoAbstract info,
            Terrain terrain,
            int startXClamp, int startZClamp,
            int endXClamp, int endZClamp,
            int terrainStartX, int terrainStartZ,
            float terrainSizeX, float terrainSizeZ,
            Vector3 position) {

            var navmeshSettings = terrain.gameObject.GetComponent<TerrainNavmeshSettings>();

            if (navmeshSettings != null && navmeshSettings.isActiveAndEnabled && navmeshSettings.data.Any(x => x != 0)) {//0 is default so if there is settings full of deffault areas then dont need that
                TerrainData data = terrain.terrainData;
                Vector3 size = data.size;
                info.settings = navmeshSettings;
                info.startXClamp = startXClamp;
                info.startZClamp = startZClamp;

                info.endXClamp = endXClamp;
                info.endZClamp = endZClamp;

                info.terrainStartX = terrainStartX;
                info.terrainStartZ = terrainStartZ;

                info.terrainSizeX = terrainSizeX;
                info.terrainSizeZ = terrainSizeZ;

                //all this values needed in 2 places. here to use data.GetAlphamaps in main thread and later in not main thread
                //so we just store it in terrain collider info
                info.alphaWidth = data.alphamapWidth;
                info.alphaHeight = data.alphamapHeight;

                //normalized start and end
                //dont needed later
                float terNormStartX = Mathf.Clamp01((template.chunkData.realX - template.properties.radius - position.x) / size.x);
                float terNormStartZ = Mathf.Clamp01((template.chunkData.realZ - template.properties.radius - position.z) / size.z);

                float terNormEndX = Mathf.Clamp01((template.chunkData.realX + PathFinder.settings.gridSize + template.properties.radius - position.x) / size.x);
                float terNormEndZ = Mathf.Clamp01((template.chunkData.realZ + PathFinder.settings.gridSize + template.properties.radius - position.z) / size.z);

                //alpha map position of chunk
                info.alphaStartX = Mathf.RoundToInt(terNormStartX * info.alphaWidth);
                info.alphaStartZ = Mathf.RoundToInt(terNormStartZ * info.alphaHeight);

                //alpha map size of chunk
                //size needed only now
                info.alphaSizeX = Math.Min(Mathf.RoundToInt((terNormEndX - terNormStartX) * info.alphaWidth) + 1, info.alphaWidth - info.alphaStartX);
                info.alphaSizeZ = Math.Min(Mathf.RoundToInt((terNormEndZ - terNormStartZ) * info.alphaHeight) + 1, info.alphaHeight - info.alphaStartZ);

                info.alphaMap = data.GetAlphamaps(info.alphaStartX, info.alphaStartZ, info.alphaSizeX, info.alphaSizeZ);
                info.possibleArea = (from areaID in navmeshSettings.data select PathFinder.GetArea(areaID)).ToArray(); //else if doCollectAreaInfo == false than later it became defaul area
            }
        }

        //make area map from raw data. as input alpha map, as output int[][] where int is index of area in area library
        //used in threads splited to separate function cause it's just unreadable in other case
        //changes "areaMap" and "passabilityMap" variables inside terrainInfo
        protected int[][] ProcessAlphaMap(TerrainColliderInfoAbstract terrainInfo) {
            float[,,] alpha = terrainInfo.alphaMap;
            int alphaCount = alpha.GetLength(2);
            int[] alphaToArea = terrainInfo.settings.data;   

            //shome stored values
            int startXClamp = terrainInfo.startXClamp;
            int startZClamp = terrainInfo.startZClamp;

            int endXClamp = terrainInfo.endXClamp;
            int endZClamp = terrainInfo.endZClamp;

            int terrainStartX = terrainInfo.terrainStartX;
            int terrainStartZ = terrainInfo.terrainStartZ;

            float terrainSizeX = terrainInfo.terrainSizeX;
            float terrainSizeZ = terrainInfo.terrainSizeZ;

            int alphaWidth = terrainInfo.alphaWidth;
            int alphaHeight = terrainInfo.alphaHeight;

            int alphaStartX = terrainInfo.alphaStartX;
            int alphaStartZ = terrainInfo.alphaStartZ;

            int alphaSizeXIndexLimit = terrainInfo.alphaSizeX - 1;
            int alphaSizeZIndexLimit = terrainInfo.alphaSizeZ - 1;

            int[][] areaMap = new int[template.lengthX_extra][];

            for (int x = 0; x < template.lengthX_extra; x++) {
                areaMap[x] = new int[template.lengthZ_extra];
            }

            terrainInfo.areaMap = areaMap;

            //this done this way to take into account that a alpha map not aligned with chunk start
            for (int x = startXClamp; x < endXClamp; x++) {
                int curX = Mathf.Clamp(Mathf.RoundToInt((x - terrainStartX) / terrainSizeX * alphaWidth) - alphaStartX, 0, alphaSizeXIndexLimit);

                for (int z = startZClamp; z < endZClamp; z++) {
                    int curZ = Mathf.Clamp(Mathf.RoundToInt((z - terrainStartZ) / terrainSizeZ * alphaHeight) - alphaStartZ, 0, alphaSizeZIndexLimit);

                    int highest = 0;
                    for (int i = 0; i < alphaCount; i++) {
                        if (alpha[curZ, curX, i] > alpha[curZ, curX, highest])
                            highest = i;
                    }
       
                    areaMap[x - template.startX_extra][z - template.startZ_extra] = alphaToArea[highest];
                }
            }

            return areaMap;
        }
        
        //apply terrain area info if it exist 
        protected void SetTerrainArea(Volume volume, TerrainColliderInfoAbstract info, Area defaultArea) {
            if (info.alphaMap != null) {
                var areaLibrary = PathFinder.settings.areaLibrary;
                int[][] areaMap = ProcessAlphaMap(info);

                for (int x = 0; x < template.lengthX_extra; x++) {
                    for (int z = 0; z < template.lengthZ_extra; z++) {
                        int a = areaMap[x][z];
                        volume.SetArea(x, z, areaLibrary[a]);
                        if (a == 1)
                            volume.SetPassability(x, z, Passability.Unwalkable);
                    }
                }
            }
            else {
                volume.SetArea(defaultArea);
            }
        }
    }
    #endregion
}
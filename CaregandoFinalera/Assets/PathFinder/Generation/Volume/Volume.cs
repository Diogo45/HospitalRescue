using System;
using System.Collections.Generic;

namespace K_PathFinder {
    //TODO: find betterway to store connections other than int[][][]. it's just to many objects. maybe some byte shifting?

    /// <summary>
    /// class for storing all data about volume. it stores uper and lower height, flags, maps all good stuff. 
    /// right now arrays are visible but later on they probably wount be. before you take some data check if it are even exist using Exist() function
    /// </summary>
    public class Volume {
        //general
        public int id;
        public int sizeX, sizeZ;
        public bool dead = false; //if true than dont use it anymore. trees puted instantly to that state or example.
        public bool terrain = false; //flag to prevent it connection with other volumes cause it's waste of time. lots of time nothing can connect to terrain.
        public bool trees = false; //flag to prevent it connection with other volumes cause it's waste of time. lots of time nothing can connect to terrain.

        //areas
        public HashSet<Area> containsAreas;//what areas it contains
        public Area[][] area;//area map. no much use but userful to have

        //maps
        public bool[][]
            existance, //if true there some data
            heightInterest, //if true there some height data that may be from nearby volumes
            coverHeightInterest;//if true here height might be sampled for cover height. this done cause mixing with heightInterest array are not working that great for that purpose

        public float[][] //in world space
            max, //highest sample in that volume
            min; //lowest sample in that volume 

        public int[][]
            passability, //values from Enums/Passability. right now: Unwalkable = 0, Slope = 1, Crouchable = 2, Walkable = 3 
            flags, //flags are stored in bytes. bytes taken from Enums/VoxelState and there lots of flags. you might wanna add some!
            hashMap, //hash are just convenient word. here are numbers for marching squares. number represent area and passability. it used PathFinder.GetAreaHash to get number and PathFinder.GetAreaByHash to return values
            coverType, //0: none, 1: low, 2: high
            coverHashMap; //share function of hashmap but here is only 3 possible numbers: MarchingSquaresIterator.COVER_HASH are positive flag, -1 negative flag and 0 is "no data" flag. marching squares are target only first

        public int[][][] connections;//stored connected id on that volume. side, x, z. sides are from Enums/Directions. wich is xPlus = 0, xMinus = 1, zPlus = 2, zMinus = 3

        //not really a map but share that purpose. position and VolumeArea here;
        public Dictionary<VolumePosSmall, HashSet<VolumeArea>> volumeArea = new Dictionary<VolumePosSmall, HashSet<VolumeArea>>();

        //constructor
        public Volume(int sizeX, int sizeZ, params Area[] areas) {
            if (areas.Length == 0)
                throw new ArgumentException("Volume must contain at least 1 Area");

            containsAreas = new HashSet<Area>(areas);

            this.sizeX = sizeX;
            this.sizeZ = sizeZ;

            max = new float[sizeX][];
            min = new float[sizeX][];
            passability = new int[sizeX][];
            flags = new int[sizeX][];
            existance = new bool[sizeX][];
            area = new Area[sizeX][];

            for (int x = 0; x < sizeX; x++) {
                max[x] = new float[sizeZ];
                min[x] = new float[sizeZ];
                passability[x] = new int[sizeZ];
                flags[x] = new int[sizeZ];
                existance[x] = new bool[sizeZ];
                area[x] = new Area[sizeZ];
            }
        }

        //create connection array on demand. cause not all volumes need it
        public void CreateConnectionsArray() {
            connections = new int[4][][];
            for (int i = 0; i < 4; i++) {
                int[][] array = new int[sizeX][];

                for (int x = 0; x < sizeX; x++) {
                    array[x] = new int[sizeZ];
                    for (int z = 0; z < sizeZ; z++) {
                        array[x][z] = -1;
                    }
                }

                connections[i] = array;
            }
        }
        
        /// <summary>
        /// for setting voxel data. this data ADDED to existed
        /// </summary>
        public void SetVoxel(int x, int z, float max, float min, Area area, int passability) {
            if (existance[x][z]) {
                this.max[x][z] = Math.Max(max, this.max[x][z]);
                this.min[x][z] = Math.Min(min, this.min[x][z]);
            }
            else {
                existance[x][z] = true;
                this.max[x][z] = max;
                this.min[x][z] = min;
            }
            this.area[x][z] = area;
            this.passability[x][z] = Math.Max(passability, this.passability[x][z]);
        }
        public void SetVoxel(int x, int z, float max, Area area, int passability) {
            if (existance[x][z]) {
                this.max[x][z] = Math.Max(max, this.max[x][z]);
            }
            else {
                existance[x][z] = true;
                this.max[x][z] = max;
            }
            this.area[x][z] = area;
            this.passability[x][z] = Math.Max(passability, this.passability[x][z]);
        }
        public void SetVoxelLight(int x, int z, float Max, int Passability) {
            existance[x][z] = true;
            max[x][z] = Max;
            passability[x][z] = Passability;
        }
        public void SetVolumeMinimum(float value) {
            for (int x = 0; x < sizeX; x++) {
                for (int z = 0; z < sizeZ; z++) {
                    if (existance[x][z])
                        min[x][z] = value;
                }
            }
        }
        
        /// <summary>
        /// for setting voxel data. this data OVERRIDE existed data
        /// </summary>
        public void OverrideVoxel(int x, int z, float max, float min, Area area, int passability) {
            existance[x][z] = true;
            this.max[x][z] = max;
            this.min[x][z] = min;
            this.area[x][z] = area;
            this.passability[x][z] = passability;
        }

        public void SetArea(int x, int z, Area a) {
            area[x][z] = a;
        }
        //for all volume
        public void SetArea(Area a) {
            for (int x = 0; x < sizeX; x++) {
                for (int z = 0; z < sizeZ; z++) {
                    area[x][z] = a;
                }
            }         
        }

        public void SetPassability(int x, int z, Passability p) {
            passability[x][z] = (int)p;
        }

        /// <summary>
        /// set existance of voxel to false
        /// </summary>
        public void Remove(int x, int z) {
            existance[x][z] = false;
        }

        //existance
        public bool Exist(int x, int z) {
            return existance[x][z];
        }
        public bool Exist(VolumePos pos) {
            return existance[pos.x][pos.z];
        }
        public bool Exist(VolumePosSmall pos) {
            return existance[pos.x][pos.z];
        }

        //passability
        public int PassabilityInt(int x, int z) {
            return passability[x][z];
        }
        public int PassabilityInt(VolumePos pos) {
            return passability[pos.x][pos.z];
        }
        public int PassabilityInt(VolumePosSmall pos) {
            return passability[pos.x][pos.z];
        }

        public Passability Passability(int x, int z) {
            return (Passability)passability[x][z];
        }
        public Passability Passability(VolumePos pos) {
            return (Passability)passability[pos.x][pos.z];
        }
        public Passability Passability(VolumePosSmall pos) {
            return (Passability)passability[pos.x][pos.z];
        }
        
        //some map data
        public float GetMax(int x, int z) {
            return max[x][z];
        }
        public float GetMin(int x, int z) {
            return min[x][z];
        }
        public Area GetArea(int x, int z) {
            return area[x][z];
        }
        public int GetPassability(int x, int z) {
            return passability[x][z];
        }

        //flag data. flag stored in byte
        public void SetState(int x, int z, VoxelState state, bool value) {
            flags[x][z] = value ? (flags[x][z] | (int)state) : (flags[x][z] & ~(int)state);
        }
        public bool GetState(int x, int z, VoxelState state) {
            return (flags[x][z] & (int)state) != 0;
        }

        //fancy shmancy operations
        public void Subtract(Volume subtractThis) {
            bool[][] subtractExistance = subtractThis.existance;

            for (int x = 0; x < sizeX; x++) {
                for (int z = 0; z < sizeZ; z++) {
                    if (subtractExistance[x][z])
                        existance[x][z] = false;
                }
            }
        }

        public void Override(Volume overridedBy) {
            Area[][] oArea = overridedBy.area;
            bool[][] oExistance = overridedBy.existance;

            float[][] oMax = overridedBy.max; 
            float[][] oMin = overridedBy.min;

            int[][] oPassability = overridedBy.passability;
            int[][] oflags = overridedBy.flags;

            for (int x = 0; x < sizeX; x++) {
                for (int z = 0; z < sizeZ; z++) {
                    if (oExistance[x][z]) {
                        existance[x][z] = true;
                        area[x][z] = oArea[x][z];

                        max[x][z] = oMax[x][z];
                        min[x][z] = oMin[x][z];

                        passability[x][z] = oPassability[x][z];
                        flags[x][z] = oflags[x][z];
                    }
                }
            }
        }

        public void ConnectToItself() {
            CreateConnectionsArray();

            int[][] xPlus = connections[(int)Directions.xPlus];
            int[][] zPlus = connections[(int)Directions.zPlus];
            int[][] xMinus = connections[(int)Directions.xMinus];
            int[][] zMinus = connections[(int)Directions.zMinus];

            bool temp = false;
            for (int z = 0; z < sizeZ; z++) {
                temp = existance[0][z];
                for (int x = 1; x < sizeX; x++) {
                    bool val = existance[x][z];
                    if (temp && val) {
                        xPlus[x - 1][z] = id;
                        xMinus[x][z] = id;
                    }
                    temp = val;
                }
            }

            for (int x = 0; x < sizeZ; x++) {
                temp = existance[x][0];
                for (int z = 1; z < sizeX; z++) {
                    bool val = existance[x][z];
                    if (temp && val) {
                        zPlus[x][z - 1] = id;
                        zMinus[x][z] = id;
                    }
                    temp = val;
                }
            }

        }

        public void Clear() {
            for (int x = 0; x < sizeX; x++) {
                for (int z = 0; z < sizeZ; z++) {
                    existance[x][z] = false;
                }
            }
        }
    }
}
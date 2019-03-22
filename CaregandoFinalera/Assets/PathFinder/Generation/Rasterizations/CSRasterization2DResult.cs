using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.Rasterization {
    public class CSRasterization2DResult {
        Voxel2D[] voxels;

        public CSRasterization2DResult(Voxel2D[] voxels) {
            this.voxels = voxels;
        }

        public void Read(Volume volume) {
            int sizeX = volume.sizeX;
            int sizeZ = volume.sizeZ;

            for (int x = 0; x < sizeX; x++) {
                for (int z = 0; z < sizeZ; z++) {
                    var curVoxel = voxels[x + (z * sizeX)];
                    if (curVoxel.exist)
                        volume.SetVoxelLight(x, z, curVoxel.height, curVoxel.passability);
                }
            }
        }
    }
}
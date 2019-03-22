using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using K_PathFinder.Rasterization;

namespace K_PathFinder {
    public class ColliderCollectorPrimitivesComputeShader : ColliderCollectorPrimitivesAbstract {
        List<RData> templatesAfterComputeShader = new List<RData>();

        static ColliderCollectorPrimitivesComputeShader() {
            //to make shure it's loaded
            ColliderCollectorPrimitivesAbstract.EmptyStaticMethodToShureBaseStaticAreLOaded();
        }

        public ColliderCollectorPrimitivesComputeShader(NavMeshTemplateRecast template, Collider[] colliders) : base(template, colliders) {}

        //in main thread
        //generate voxels
        public void CollectUsingComputeShader() {
            float maxSlopeCos = Mathf.Cos((float)((double)template.maxSlope * Math.PI / 180.0));

            for (int i = 0; i < templates.Count; i++) {
                MeshColliderInfo curInfo = templates[i];
                Vector3 offsetedPos = template.realOffsetedPosition;
                
                CSRasterization3DResult result = PathFinder.scene.Rasterize3D(
                    curInfo.verts,
                    curInfo.tris,
                    curInfo.bounds,
                    curInfo.matrix,
                    template.lengthX_extra, 
                    template.lengthZ_extra,
                    offsetedPos.x,
                    offsetedPos.z, 
                    template.voxelSize,
                    maxSlopeCos);

                if(result != null)
                    templatesAfterComputeShader.Add(new RData(curInfo, result));
            }
        }
        
        //in not main thread
        //read voxels to volume
        public override void Collect(VolumeContainer container) {
            if (templatesAfterComputeShader == null) {
                Debug.LogWarning("expecting to recive things from compute shader but list was null");
                return;
            }
            for (int i = 0; i < templatesAfterComputeShader.Count; i++) {
                Volume curInfoVolume = new Volume(template.lengthX_extra, template.lengthZ_extra, templatesAfterComputeShader[i].info.area);
                templatesAfterComputeShader[i].result.Read(curInfoVolume, templatesAfterComputeShader[i].info.area);
                container.AddVolume(curInfoVolume);
            }
        }

        private class RData {
            public MeshColliderInfo info;
            public CSRasterization3DResult result;

            public RData(MeshColliderInfo info, CSRasterization3DResult result) {
                this.info = info;
                this.result = result;
            }
        }
    }
}

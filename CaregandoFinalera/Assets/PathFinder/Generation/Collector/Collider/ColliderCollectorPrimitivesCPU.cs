using UnityEngine;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif
 

namespace K_PathFinder {
    public class ColliderCollectorPrimitivesCPU : ColliderCollectorPrimitivesAbstract {
        static ColliderCollectorPrimitivesCPU() {
            //to make shure it's loaded
            ColliderCollectorPrimitivesAbstract.EmptyStaticMethodToShureBaseStaticAreLOaded();
        }


        //construtor are called in main thread but execute Collect(FragmentContainer) in own thread
        public ColliderCollectorPrimitivesCPU(NavMeshTemplateRecast template, Collider[] colliders) : base(template, colliders) {
        }       

        //threaded
        public override void Collect(VolumeContainer container) {
            float maxSlopeCos = Mathf.Cos((float)((double)template.maxSlope * Math.PI / 180.0));
            foreach (var colTemplate in templates) {
                Area area = colTemplate.area;
                Volume volume = new Volume(template.lengthX_extra, template.lengthZ_extra, area);

                Vector3[] templateVerts = colTemplate.verts;
                Vector3[] verts = new Vector3[templateVerts.Length];
                Matrix4x4 matrix = colTemplate.matrix;

                for (int i = 0; i < templateVerts.Length; i++) {
                    verts[i] = matrix.MultiplyPoint3x4(templateVerts[i]);
                }

                int[] tris = colTemplate.tris;

                for (int t = 0; t < colTemplate.tris.Length; t += 3) {
                    Vector3 A = verts[tris[t]];
                    Vector3 B = verts[tris[t + 1]];
                    Vector3 C = verts[tris[t + 2]];

                    bool unwalkableBySlope = !CalculateWalk(A, B, C, maxSlopeCos);
                    Passability currentPassability;

                    if (area.id == 1)//id of clear Area all time
                        currentPassability = Passability.Unwalkable;
                    else if (unwalkableBySlope)
                        currentPassability = Passability.Slope;
                    else
                        currentPassability = Passability.Walkable;

#if UNITY_EDITOR
                    if (currentPassability > Passability.Slope && Debuger_K.doDebug && Debuger_K.debugOnlyNavMesh == false)
                        Debuger_K.AddWalkablePolygon(template.gridPosX, template.gridPosZ, template.properties, A, B, C);
#endif

                    base.RasterizeTriangle(
                        volume, A, B, C,
                        template.voxelSize,
                        template.startX_extra,
                        template.endX_extra,
                        template.startZ_extra,
                        template.endZ_extra,
                        area, currentPassability);
                }

                container.AddVolume(volume);
            }
        }
    }
}


using K_PathFinder.PFDebuger;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder {
    public abstract class ColliderCollectorPrimitivesAbstract : ColliderCollectorTerrainAbstract {
        #region static things
        protected static Vector3[] _cubeVerts;
        protected static Vector3[] _sphereVerts;
        protected static Vector3[] _capsuleVerts;

        protected static int[] _cubeTris;
        protected static int[] _sphereTris;
        protected static int[] _capsuleTris;

        static ColliderCollectorPrimitivesAbstract() {
            CreatePrimitive(PrimitiveType.Cube, out _cubeVerts, out _cubeTris);
            CreatePrimitive(PrimitiveType.Sphere, out _sphereVerts, out _sphereTris);
            CreatePrimitive(PrimitiveType.Capsule, out _capsuleVerts, out _capsuleTris);
        }

        private static void CreatePrimitive(PrimitiveType type, out Vector3[] verts, out int[] tris) {
            GameObject primitive = GameObject.CreatePrimitive(type);
            Mesh result = primitive.GetComponent<MeshFilter>().sharedMesh;
            verts = result.vertices;
            tris = result.triangles;
            Object.DestroyImmediate(primitive);            
        }

        protected static void EmptyStaticMethodToShureBaseStaticAreLOaded() { }
        #endregion

        protected List<MeshColliderInfo> templates = new List<MeshColliderInfo>();

        protected ColliderCollectorPrimitivesAbstract(NavMeshTemplateRecast template, Collider[] colliders) : base(template, colliders) {
            GenerateTemplates();
        }

        protected void GenerateTemplates() {
            foreach (var collider in validColliders) {
                Matrix4x4 matrix = Matrix4x4.identity;
                Transform colliderTransform = collider.transform;
                Vector3[] verts = null;
                int[] tris = null;
                Bounds bounds = collider.bounds;

                if (collider is BoxCollider) {
                    verts = _cubeVerts;
                    tris = _cubeTris;
                    matrix = Matrix4x4.TRS(bounds.center, colliderTransform.rotation, Vector3.Scale(colliderTransform.lossyScale, (collider as BoxCollider).size));
                }

                if (collider is SphereCollider) {
                    verts = _sphereVerts;
                    tris = _sphereTris;
                    float r = bounds.extents.x / 0.5f;
                    matrix = Matrix4x4.TRS(bounds.center, Quaternion.identity, new Vector3(r, r, r));
                }

                //old
                //if (collider is CapsuleCollider | collider is CharacterController) {
                //    matrix = colliderTransform.localToWorldMatrix;
                //    verts = _capsuleVerts;
                //    tris = _capsuleTris;
                //}

                if(collider is CapsuleCollider) {
                    tris = _capsuleTris;
                    verts = new Vector3[_capsuleVerts.Length];
                    CapsuleCollider capsuleCollider = collider as CapsuleCollider;

                    Vector3 lossyScale = colliderTransform.lossyScale;
                    float height = capsuleCollider.height * lossyScale.y;
                    float radius = capsuleCollider.radius * Mathf.Max(lossyScale.x, lossyScale.z);
  
                    float size = Mathf.Max(1f, height / (radius * 2f)) - 2f;
                    Matrix4x4 capsuleShapeMatrix = Matrix4x4.Scale(new Vector3(radius * 2f, radius * 2f, radius * 2f));

                    for (int index = 0; index < verts.Length; ++index) {
                        if (_capsuleVerts[index].y > 0.0)
                            verts[index] = capsuleShapeMatrix.MultiplyPoint(
                                new Vector3(
                                    _capsuleVerts[index].x, 
                                    Mathf.Max((float)(_capsuleVerts[index].y + size * 0.5), 0.0f), 
                                    _capsuleVerts[index].z));

                        else if (_capsuleVerts[index].y < 0.0)
                            verts[index] = capsuleShapeMatrix.MultiplyPoint(
                                new Vector3(
                                    _capsuleVerts[index].x, 
                                    Mathf.Min((float)(_capsuleVerts[index].y - size * 0.5), 0.0f), 
                                    _capsuleVerts[index].z));
                    }
     
                    matrix = Matrix4x4.TRS(
                        capsuleCollider.bounds.center,//actual world position
                        colliderTransform.rotation * //world rotation
                        Quaternion.Euler(//plus capsule orientation
                            capsuleCollider.direction == 2 ? 90f : 0.0f,
                            0.0f,
                            capsuleCollider.direction == 0 ? 90f : 0.0f),
                        Vector3.one);
                }

                if (collider is CharacterController) {
                    tris = _capsuleTris;
                    verts = new Vector3[_capsuleVerts.Length];
                    CharacterController charControler = collider as CharacterController;

                    Vector3 lossyScale = colliderTransform.lossyScale;
                    float height = charControler.height * lossyScale.y;
                    float radius = charControler.radius * Mathf.Max(lossyScale.x, lossyScale.z);

                    float size = Mathf.Max(1f, height / (radius * 2f)) - 2f;
                    Matrix4x4 capsuleShapeMatrix = Matrix4x4.Scale(new Vector3(radius * 2f, radius * 2f, radius * 2f));

                    for (int index = 0; index < verts.Length; ++index) {
                        if (_capsuleVerts[index].y > 0.0)
                            verts[index] = capsuleShapeMatrix.MultiplyPoint(new Vector3(_capsuleVerts[index].x, Mathf.Max((float)(_capsuleVerts[index].y + size * 0.5), 0.0f), _capsuleVerts[index].z));
                        else if (_capsuleVerts[index].y < 0.0)
                            verts[index] = capsuleShapeMatrix.MultiplyPoint(new Vector3(_capsuleVerts[index].x, Mathf.Min((float)(_capsuleVerts[index].y - size * 0.5), 0.0f), _capsuleVerts[index].z));
                    }

                    matrix = Matrix4x4.TRS(charControler.bounds.center, colliderTransform.rotation, Vector3.one);
                }

                if (collider is MeshCollider) {
                    Mesh curMesh = (collider as MeshCollider).sharedMesh;
                    verts = curMesh.vertices;
                    tris = curMesh.triangles;
                    matrix = colliderTransform.localToWorldMatrix;
                }

                if (verts == null || tris == null) {
                    Debug.LogFormat("hey i dont know wich collider type is on {0}. please tell developer.", collider.gameObject.name);
                    return;
                }

#if UNITY_EDITOR
                if (Debuger_K.doDebug && Debuger_K.debugOnlyNavMesh == false)
                    Debuger_K.AddColliderBounds(template.gridPosition.x, template.gridPosition.z, template.properties, collider);
#endif

                var gameObjectArea = colliderTransform.GetComponent<AreaGameObject>();

                //if gameobject have area then use it if not get default area
                Area area = gameObjectArea != null ? gameObjectArea.GetArea() : PathFinder.getDefaultArea;
                templates.Add(new MeshColliderInfo(verts, tris, area, matrix, bounds));
            }

            if (template.profiler != null)
                template.profiler.AddLog("end collecting primitive colliders. collected: " + templates.Count);

        }

        public override int collectedCount {
            get { return templates.Count; }
        }

        protected override bool IsValid(Collider collider) {
            if (collider == null ||
               !collider.enabled ||
               !template.chunkOffsetedBounds.Intersects(collider.bounds) ||
               collider is TerrainCollider || //!!!
               collider.isTrigger)
                return false;

            return !template.IgnoredTagsContains(collider.tag);
        }
    }
}

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using K_PathFinder.Serialization;
using K_PathFinder.Rasterization;


#if UNITY_EDITOR
using UnityEditor;
using K_PathFinder.PFDebuger;
#endif


namespace K_PathFinder {
    //also this thing responsible for compute shader rasterization
    [ExecuteInEditMode()]
    public class PathFinderScene : MonoBehaviour {
        [SerializeField]public SceneNavmeshData sceneNavmeshData;      
        bool _areInit = false;
        Dictionary<int, IEnumerator> coroutineDictionary = new Dictionary<int, IEnumerator>();

        //compute shader stuff
        CSRasterization3D CSR3D;
        CSRasterization2D CSR2D;

#if UNITY_EDITOR
        void OnEditorUpdate() {
            if (_areInit == false)
                return;

            foreach (var item in coroutineDictionary.Values) {
                item.MoveNext();
            }
        }
        void OnEnable() {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }
#endif

        void OnDestroy() {
#if UNITY_EDITOR
            EditorApplication.update -= OnEditorUpdate;
#endif

            PathFinder.CallThisWhenSceneObjectWasGone();

            CSR3D = null;
            CSR2D = null;
        }
        
        public void AddCoroutine(int key, IEnumerator iEnumerator) {
            coroutineDictionary[key] = iEnumerator;
        }

        public void Init() {
            if (_areInit)
                return;

            _areInit = true;

            Debug.Log("PathFinderScene init");

            int maxCount = PathFinder.settings.areaLibrary.Count;
            foreach (var item in FindObjectsOfType<TerrainNavmeshSettings>()) {
                int[] data = item.data;

                for (int i = 0; i < item.data.Length; i++) {
                    if (data[i] > maxCount) {
                        Debug.LogWarningFormat("on {0} terrain in data index of area was higher than it possible can be. fix it! for now it will be default/", item.gameObject.name);
                        data[i] = 0;
                    }
                }
            }
                 
            StopAllCoroutines();

            foreach (var item in coroutineDictionary.Values) {
                StartCoroutine(item);
            }

            LoadCurrentData();
        }

        public void InitComputeShaderRasterization3D(ComputeShader shader) {
            if (CSR3D != null)
                return;
            CSR3D = new CSRasterization3D(shader);
        }
        public void InitComputeShaderRasterization2D(ComputeShader shader) {
            if (CSR2D != null)
                return;
            CSR2D = new CSRasterization2D(shader);
        }


        public CSRasterization3DResult Rasterize3D(Vector3[] verts, int[] tris, Bounds bounds, Matrix4x4 matrix, int volumeSizeX, int volumeSizeZ, float chunkPosX, float chunkPosZ, float voxelSize, float maxSlopeCos, bool debug = false) {
            return CSR3D.Rasterize(verts, tris, bounds, matrix, volumeSizeX, volumeSizeZ, chunkPosX, chunkPosZ, voxelSize, maxSlopeCos, debug);
        }
        public CSRasterization2DResult Rasterize2D(Vector3[] verts, int[] tris, int volumeSizeX, int volumeSizeZ, float chunkPosX, float chunkPosZ, float voxelSize, float maxSlopeCos, bool debug = false) {
            return CSR2D.Rasterize(verts, tris, volumeSizeX, volumeSizeZ, chunkPosX, chunkPosZ, voxelSize, maxSlopeCos, debug);
        }

        public void StopAll() {
            StopAllCoroutines();
        }
        public void Shutdown() {
            StopAll();
            _areInit = false;
        }
        
        public void LoadCurrentData() {
            if (sceneNavmeshData == null) {
#if UNITY_EDITOR
                if (Debuger_K.doDebug)
                    Debug.LogWarning("No data to load");
#endif
                return;
            }

#if UNITY_EDITOR
            if (Debuger_K.doDebug)
                Debug.LogWarning("Load current data");
            Debuger_K.ClearChunksDebug();
#endif

            for (int i = 0; i < sceneNavmeshData.properties.Count; i++) {
                if (sceneNavmeshData.properties[i] == null) {
                    Debug.LogWarning("properties == null");
                    continue;
                }
                PathFinder.Deserialize(sceneNavmeshData.navmesh[i], sceneNavmeshData.properties[i]);
            }
        }
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(PathFinderScene))]
    public class LevelScriptEditor : Editor {
        public override void OnInspectorGUI() {
            PathFinderScene myTarget = (PathFinderScene)target;

            myTarget.sceneNavmeshData = (SceneNavmeshData)EditorGUILayout.ObjectField(new GUIContent("Navmesh Data",
                    "Scriptable object with serialized NavMesh data. You can save all current data to it in pathfinder menu. Later it will be loaded from here"), 
                    myTarget.sceneNavmeshData, typeof(SceneNavmeshData), false);            

            if (GUILayout.Button("Load")) {
                PathFinder.LoadCurrentSceneData();
            }
        }
    }
#endif
}
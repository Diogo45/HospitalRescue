using UnityEngine;
using System.Collections;
using K_PathFinder.Settings;
using UnityEditor;
using K_PathFinder.PFDebuger;
using K_PathFinder;
using K_PathFinder.Graphs;

//debuger and settings
namespace K_PathFinder {
    public class PathFinderMenu : EditorWindow {
        const float LABEL_WIDTH = 160f;

        Vector2 scroll;

        //settings
        PathFinderSettings settings;
        private float desiredLabelWidth;

        [SerializeField]
        bool _showSettings = true;

        //debuger
        Vector3 start, end;
        bool sellectStart, sellectEnd;
        Vector3 pointer = Vector3.zero;

        [SerializeField]
        bool _showDebuger = true;
        [SerializeField]
        bool _redoRemovedGraphs = true;

        [MenuItem(PathFinderSettings.UNITY_TOP_MENU_FOLDER + "/Menu", false, 0)]
        public static void OpenWindow() {
            GetWindow<PathFinderMenu>("PathFinderMenu").Show();
        }

        void OnEnable() {
            Debuger_K.Init();

            settings = PathFinderSettings.LoadSettings();
            SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
            SceneView.onSceneGUIDelegate += this.OnSceneGUI;
            Repaint();
            this.autoRepaintOnSceneChange = true;
        }

        void OnDestroy() {
            EditorUtility.SetDirty(settings);
            SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
            Debuger_K.SetSettingsDirty();
        }

        void OnGUI() {
            scroll = GUILayout.BeginScrollView(scroll);
            float curLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            try {         
                ImportantButtons();
            }
            catch (System.Exception e) {
                GUILayout.Label(string.Format("Exception has ocured in importand buttons.\n\nException:\n{0}", e));
            }

            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });

            _showSettings = EditorGUILayout.Foldout(_showSettings, "Settings");
            if (_showSettings) {
                try {           
                    ShowSettings();
                }
                catch (System.Exception e) {
                    GUILayout.Label(string.Format("Exception has ocured while showing settings.\n\nException:\n{0}", e));      
                }
            }       

            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });

            _showDebuger = EditorGUILayout.Foldout(_showDebuger, "Debuger");
            if (_showDebuger) {
                try {
                    ShowDebuger();
                }
                catch (System.Exception e) {
                    GUILayout.Label(string.Format("Exception has ocured while showing debuger.\n\nException:\n{0}", e));
                }
            } 

            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });

            EditorGUIUtility.labelWidth = curLabelWidth;

            GUILayout.EndScrollView();

            if (GUI.changed) {
                EditorUtility.SetDirty(settings);
                Debuger_K.SetSettingsDirty();
                Repaint();
            }
        }

        void OnSceneGUI(SceneView sceneView) {
            Event curEvent = Event.current;

            Color col = Handles.color;
            Handles.color = Color.red;
            Handles.DrawLine(pointer, pointer + Vector3.up);
            Handles.color = col;

            if ((sellectStart | sellectEnd)) {
                RaycastHit hit;
                if (Physics.Raycast(HandleUtility.GUIPointToWorldRay(curEvent.mousePosition), out hit)) {
                    var somePos = PathFinder.ToChunkPosition(hit.point);

                    if (sellectStart) {
                        settings.startX = somePos.x;
                        settings.startZ = somePos.z;
                    }
                    if (sellectEnd) {
                        settings.sizeX = Mathf.Max(1, somePos.x - settings.startX + 1);
                        settings.sizeZ = Mathf.Max(1, somePos.z - settings.startZ + 1);
                    }
                    pointer = hit.point;
                }

                if (curEvent.type == EventType.MouseDown && curEvent.button == 0) {
                    sellectStart = false;
                    sellectEnd = false;
                }
                Repaint();
            }

            Debuger_K.DrawDebugLabels();

            Handles.BeginGUI();
            Debuger_K.DrawSceneGUI();
            Handles.EndGUI();
        }

        private void ImportantButtons() {       
            //properties
            settings.targetProperties = (AgentProperties)EditorGUILayout.ObjectField(new GUIContent("Properties", "Build navmesh using this properties"), settings.targetProperties, typeof(AgentProperties), false);

            //sellected chunks
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("start x:", GUILayout.MaxWidth(45));
            settings.startX = EditorGUILayout.IntField(settings.startX);
            EditorGUILayout.LabelField("z:", GUILayout.MaxWidth(15));
            settings.startZ = EditorGUILayout.IntField(settings.startZ);
            if (!sellectStart) {
                if (GUILayout.Button("sellect"))
                    sellectStart = true;
            }
            else
                GUILayout.Label("click");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("size x:", GUILayout.MaxWidth(45));
            settings.sizeX = EditorGUILayout.IntField(settings.sizeX);
            EditorGUILayout.LabelField("z:", GUILayout.MaxWidth(15));
            settings.sizeZ = EditorGUILayout.IntField(settings.sizeZ);

            if (!sellectEnd) {
                if (GUILayout.Button("Sellect"))
                    sellectEnd = true;
            }
            else
                GUILayout.Label("Click");
            EditorGUILayout.EndHorizontal();


            //upper row of cool buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Build", "Build navmesh in sellected area"))) {
                if (settings.targetProperties == null) {
                    Debug.LogWarning("forgot to add properties");
                    return;
                }

                PathFinder.QueueGraph(
                    settings.startX,
                    settings.startZ,
                    settings.targetProperties,
                    settings.sizeX,
                    settings.sizeZ);
            }


            if (GUILayout.Button(new GUIContent("Remove", "Remove navmesh from sellected area. Only target area will be removed."))) {
                PathFinder.RemoveGraph(
                    settings.startX,
                    settings.startZ,
                    settings.targetProperties,
                    settings.sizeX,
                    settings.sizeZ,
                    _redoRemovedGraphs);
            }
            _redoRemovedGraphs = GUILayout.Toggle(_redoRemovedGraphs, new GUIContent("", "Queue removed again? If true then we refresh sellected chunks"), GUILayout.MaxWidth(18));


            if (GUILayout.Button(new GUIContent("Clear", "Remove all NavMesh. Also stop all work"))) {
                PathFinder.ClearAll();
                Debuger_K.ClearChunksDebug();
            }
            GUILayout.EndHorizontal();

            //net row of cool buttons about serialization
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Save", "Save all current navmesh into SceneNavmeshData. If it not exist then suggest to create one and pass reference to it into scene helper.")))
                PathFinder.SaveCurrentSceneData();
            if (GUILayout.Button(new GUIContent("Load", "Load current SceneNavmeshData from scene helper")))
                PathFinder.LoadCurrentSceneData();
            if (GUILayout.Button(new GUIContent("Delete", "Remove all serialized data from current NavMesh data. Scriptable object remain in project")))
                PathFinder.ClearCurrentData();
            GUILayout.EndHorizontal();
        }

        private void ShowSettings() {
            if(settings == null)
                settings = PathFinderSettings.LoadSettings();
            
            settings.helperName = EditorGUILayout.TextField(new GUIContent("Helper name", "pathfinder need object in scene in order to use unity API. you can specify it's name here"), settings.helperName);
            settings.useMultithread = EditorGUILayout.Toggle(new GUIContent("Multithread", "you can on/off multithreading for debug purpose. cause debuging threads is pain"), settings.useMultithread);  

            if (settings.useMultithread)
                settings.maxThreads = EditorGUILayout.IntField(new GUIContent("Max threads", "limit how much threads are used"), settings.maxThreads);

            settings.terrainCollectionType = (TerrainCollectorType)EditorGUILayout.EnumPopup(new GUIContent("Terrain collection", "UnityWay - Collect data from terrain using Terrain.SampleHeight and TerrainData.GetSteepness. It's fast but it's all in main thread.\nCPU - Collect data by some fancy math using CPU. Not that fast but fully threaded.\nComputeShader - Superfast but in big chunks can be slow cause moving data from GPU is not that fast."), settings.terrainCollectionType);
            settings.colliderCollectionType = (ColliderCollectorType)EditorGUILayout.EnumPopup(new GUIContent("Collider collection", "CPU - Collect data using CPU rasterization. It's threaded so no FPS drops here. \nComputeShader - Collect data by ComputeShader. Superfast but in big chunks can be slow cause moving data from GPU is not that fast."), settings.colliderCollectionType);
            
            settings.gridSize = EditorGUILayout.FloatField(new GUIContent("World grid size", "Chunk size in world space. Good values are 10, 15, 20 etc."), settings.gridSize);
            settings.gridHighest = EditorGUILayout.IntField(new GUIContent("Chunk max height", "For autocreating chunks. World space value is grid size * this value."), settings.gridHighest);

            if (settings.gridHighest < settings.gridLowest)
                settings.gridHighest = settings.gridLowest;

            settings.gridLowest = EditorGUILayout.IntField(new GUIContent("Chunk min height", "For autocreating chunks. World space value is grid size * this value."), settings.gridLowest);

            if (settings.gridLowest > settings.gridHighest)
                settings.gridLowest = settings.gridHighest;

            settings.DrawAreaEditor();
        }

        private void ShowDebuger() {
            Debuger_K.settings.doDebug = EditorGUILayout.Toggle(new GUIContent("Do debug", "enable debuging. debuged values you can enable down here. generic values will be debuged anyway"), Debuger_K.settings.doDebug);
            if (Debuger_K.settings.doDebug) {
                Debuger_K.settings.debugOnlyNavmesh = EditorGUILayout.Toggle(new GUIContent("Full Debug", "if false will debug only resulted navmesh. prefer debuging only navmesh. and do not use unity profiler if you enable this option or else unity will die in horribly way. also do not enable it if area are too big. memory expensive stuff here!"), Debuger_K.settings.debugOnlyNavmesh);
            }

            Debuger_K.settings.doProfilerStuff = EditorGUILayout.Toggle(new GUIContent("Do profiler", "are we using some simple profiling? cause unity dont really profile threads. if true will write lots of stuff to console"), Debuger_K.settings.doProfilerStuff);
            Debuger_K.settings.doDebugPaths = EditorGUILayout.Toggle(new GUIContent("Debug Paths", "If true then pathfinder will put lot's of info into paths debug. Like cell path or cost of some other info"), Debuger_K.settings.doDebugPaths);
            Debuger_K.settings.showSceneGUI = EditorGUILayout.Toggle(new GUIContent("Scene GUI", "Enable or disable checkboxes in scene to on/off debug of certain chunks and properties. To apply changes push Update button"), Debuger_K.settings.showSceneGUI);
            Debuger_K.settings.clearGenericOnUpdate = EditorGUILayout.Toggle(new GUIContent("Clear Generic on Update", "Things listed below like Dots, Lines, Meshes, or even Path considered as Generic. if you want you can disable or enable clearing it on Update"), Debuger_K.settings.clearGenericOnUpdate);


            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });

            Debuger_K.GenericGUI();

            Debuger_K.settings.showSelector = EditorGUILayout.Foldout(Debuger_K.settings.showSelector, "Debug options");
            if (Debuger_K.settings.showSelector) {
                Debuger_K.SellectorGUI2();
                //Debuger_K.SellectorGUI();
            }
        }
    }
}

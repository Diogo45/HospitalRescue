using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace K_PathFinder.Settings {
    [Serializable]
    public class PathFinderSettings : ScriptableObject {
        public const string PROJECT_FOLDER = "PathFinder";
        public const string ASSETS_FOLDER = "Assets";
        public const string EDITOR_FOLDER = "Editor";
        public const string MANUAL_FOLDER = "Manual";
        public const string SHADERS_FOLDER = "Shaders";
        public const string UNITY_TOP_MENU_FOLDER = "Window/K-PathFinder";
        public const string RESOURSES_FOLDER = "Resources";
        public const string PROPERTIES_FOLDER = "Properties";
        public const string DEBUGER_FOLDER = "Debuger";
        public const string SETTINGS_ASSET_NAME = "PathfinderSettings";
        public const string DEBUGER_ASSET_NAME = "DebugerSettings";
        public const string MANUAL_ASSET_NAME = "ManualSettings";

        [SerializeField]public string helperName = "_pathFinderHelper";
        [SerializeField]public bool useMultithread = true;
        [SerializeField]public int maxThreads = 8;

        [SerializeField]public float gridSize = 10f;
        [SerializeField]public int gridLowest = -100;
        [SerializeField]public int gridHighest = 100;
        [SerializeField]public TerrainCollectorType terrainCollectionType = TerrainCollectorType.UnityWay;
        [SerializeField]public ColliderCollectorType colliderCollectionType = ColliderCollectorType.CPU;

        [SerializeField]public float terrainFastMinimalSize = 0.1f;

        [SerializeField]private bool drawAreaEditor;
        [SerializeField]public List<Area> areaLibrary;

        //area to build
        [SerializeField]public int startX = 0;
        [SerializeField]public int startZ = 0;
        [SerializeField]public int sizeX = 1;
        [SerializeField]public int sizeZ = 1;

        [SerializeField]private string lastLaunchedVersion;

        //properties to build
        [SerializeField]public AgentProperties targetProperties;

        [SerializeField]public ComputeShader ComputeShaderRasterization3D;
        [SerializeField]public ComputeShader ComputeShaderRasterization2D;

        public GUIContent[] areaNames;
        public int[] areaIDs;

        void OnEnable() {
            switch (lastLaunchedVersion) {
                default:
                    //kinda need to know last launched version but if i not use this value anywhere then unity will annoy with it
                    break;
            }
            lastLaunchedVersion = PathFinder.VERSION;
            ResetAreaPublicDara();
        }
        
        public static PathFinderSettings LoadSettings() {
            PathFinderSettings result = Resources.Load<PathFinderSettings>(SETTINGS_ASSET_NAME);
#if UNITY_EDITOR
            if (result == null) {
                result = CreateInstance<PathFinderSettings>();

                result.areaLibrary = new List<Area>();
                result.areaLibrary.Add(new Area("Default", 0, Color.green));
                result.areaLibrary.Add(new Area("Not Walkable", 1, Color.red) { cost = float.MaxValue });         

                AssetDatabase.CreateAsset(result, String.Format("{0}/{1}/{2}/{3}.asset", new string[] { "Assets", PROJECT_FOLDER, RESOURSES_FOLDER, SETTINGS_ASSET_NAME }));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
#endif
            return result;
        }

        void ResetAreaPublicDara() {
            areaNames = new GUIContent[areaLibrary.Count];
            areaIDs = new int[areaLibrary.Count];
            for (int i = 0; i < areaLibrary.Count; i++) {
                areaNames[i] = new GUIContent(areaLibrary[i].name);
                areaIDs[i] = areaLibrary[i].id;
            }
        }

        #region area manage
        public Area getDefaultArea {
            get { return areaLibrary[0]; }
        }        

        public int areasMaxID {
            get { return areaLibrary.Count - 1; }
        }

        public void AddArea() {
            areaLibrary.Add(new Area("Area " + areaLibrary.Count, areaLibrary.Count));
            ResetAreaPublicDara();
        }

        public void RemoveArea(int id) {
            if (id == 0 | areaLibrary.Count - 1 < id)
                return;

            areaLibrary.RemoveAt(id);

            for (int i = id; i < areaLibrary.Count; i++) {
                areaLibrary[i].id = i;
            }
            ResetAreaPublicDara();
        }
#if UNITY_EDITOR
        public int DrawAreaSellector(int currentValue) {
            if (currentValue > areasMaxID)
                currentValue = 0;

            GUILayout.BeginHorizontal();
            GUILayout.Label(currentValue.ToString() + ":", GUILayout.MaxWidth(15));

            Color curColor = GUI.color;
            GUI.color = areaLibrary[currentValue].color;
            GUILayout.Box("", GUILayout.MaxWidth(15));
            GUI.color = curColor;
            currentValue = EditorGUILayout.IntPopup(currentValue, areaNames, areaIDs);
            GUILayout.EndHorizontal();
            return currentValue;
        }

        public void DrawAreaEditor() {
            drawAreaEditor = EditorGUILayout.Foldout(drawAreaEditor, "area editor");

            if (!drawAreaEditor)
                return;

            if (GUILayout.Button("Add")) {
                AddArea();
            }

            for (int i = 0; i < areaLibrary.Count; i++) {
                bool remove;
                DrawAreaGUI(areaLibrary[i], out remove);
                if (remove) {
                    RemoveArea(i);
                    break;
                }
            }
        }

        private void DrawAreaGUI(Area area, out bool remove) {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(area.id.ToString(), GUILayout.MaxWidth(10f));
            if(area.id == 0 || area.id == 1) { //cant choose name to default areas to avoit confussion
                EditorGUILayout.LabelField(new GUIContent(area.name, "area name"), GUILayout.MaxWidth(80));
            }
            else {
                area.name = EditorGUILayout.TextField(area.name, GUILayout.MaxWidth(80));
            }
            area.color = EditorGUILayout.ColorField(area.color, GUILayout.MaxWidth(30f));

            EditorGUILayout.LabelField(new GUIContent("cost", "move cost"), GUILayout.MaxWidth(28));

            if (area.id == 1) 
                EditorGUILayout.LabelField("max", GUILayout.MaxWidth(30f));            
            else 
                area.cost = EditorGUILayout.FloatField(area.cost, GUILayout.MaxWidth(30f));


            EditorGUILayout.LabelField(new GUIContent("priority", "z-fighting cure. if one layer on another than this number matter"), GUILayout.MaxWidth(45f));

            if (area.id == 1)
                EditorGUILayout.LabelField(new GUIContent("-1", "clear area it always should be -1"), GUILayout.MaxWidth(30f));
            else {
                area.overridePriority = EditorGUILayout.IntField(area.overridePriority, GUILayout.MaxWidth(30f));
                if (area.overridePriority < 0)
                    area.overridePriority = 0;
            }            

            if (area.id == 0 || area.id == 1) 
                remove = false;            
            else 
                remove = GUILayout.Button("x", GUILayout.MaxWidth(20f));            

            EditorGUILayout.EndHorizontal();
        }

        [CustomEditor(typeof(PathFinderSettings))]
        public class PathFinderSettingsEditor : Editor {
            public override void OnInspectorGUI() {
                EditorGUILayout.LabelField("you probably should not edit this file in inspector");
                PathFinderSettings s = (PathFinderSettings)target;
                if (s == null)
                    return;

                EditorGUILayout.LabelField("some links to important files:");
                s.ComputeShaderRasterization2D = (ComputeShader)EditorGUILayout.ObjectField("CS Rasterization 2D", s.ComputeShaderRasterization2D, typeof(ComputeShader), false);
                s.ComputeShaderRasterization3D = (ComputeShader)EditorGUILayout.ObjectField("CS Rasterization 3D", s.ComputeShaderRasterization3D, typeof(ComputeShader), false);        
            }
        
        }
#endif
        #endregion
    }
}

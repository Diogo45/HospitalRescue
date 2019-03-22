#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System;
using System.Linq;

using K_PathFinder.PFDebuger.Helpers;
using K_PathFinder.NodesNameSpace;
using K_PathFinder.EdgesNameSpace;
using K_PathFinder.Graphs;
using K_PathFinder.CoverNamespace;
using System.Text;

using UnityEditor;
using K_PathFinder.GraphGeneration;

namespace K_PathFinder.PFDebuger {
    //public enum PFDOptionEnum{
    //    Cell,
    //    CellConnections,
    //    Edge,
    //    Node,
    //    Cover,
    //    BattleGrid,
    //    CellInfo,
    //    JumpBase,
    //    BoundsChunk,
    //    BoundsCollider,
    //    TreeWireMesh,
    //    WalkablePolygon,
    //    Layers,
    //    Voxels,
    //    VoxelConnections,
    //    VoxelVolumes,    
    //    TempNodePreRDP,
    //    TempNodeConsPreRDP,
    //    TempNodeLabelPreRDP,
    //    TempNode,
    //    TempNodeCons,  
    //    TempNodeLabel,
    //    EdgeLabel, 
    //    NodeLabel, 
    //    Triangilator,
    //    Hash,
    //    CoverHash,
    //    HeightInterest
    //}
    
    public enum DebugGroup {
        line = 0,
        dot = 1,
        label = 2,
        mesh = 3,
        path = 4
    }    

    public enum DebugOptions : int {
        Cell = 0,
        CellArea = 1,
        CellEdges = 2,
        CellConnection = 3,
        Cover = 4,
        Grid = 5,
        JumpBase = 6,
        Voxels = 7,
        VoxelMax = 8,
        VoxelMin = 9,
        VoxelVolume = 10,
        VoxelConnection = 11,
        VoxelHash = 12,
        ChunkBounds = 13,
        ColliderBounds = 14,
        NodesAndConnections = 15,
        NodesAndConnectionsPreRDP = 16,
        WalkablePolygons = 17,
        Triangulator = 18
    }

    public static class Debuger_K{
        public static PFDSettings settings;
        //private static Dictionary<Chunk, Dictionary<AgentProperties, ChunkDebugInfo>> data = new Dictionary<Chunk, Dictionary<AgentProperties, ChunkDebugInfo>>();
        //public static Dictionary<PFDOptionEnum, int> handlesCounter = new Dictionary<PFDOptionEnum, int>();
        private static Vector2 debugScrollPos;

        private static Dictionary<int, Color> hashColorDictionary = new Dictionary<int, Color>();

        private static PFDDebugerScene sceneObj;
        private static bool areInit = false;

        private static bool needPathUpdate, needGenericDotUpdate, needGenericLineUpdate, needGenericTrisUpdate;

        //gui stuff and debug arrays
        private static bool[] debugFlags;
        private const int FLAGS_AMOUNT = 19;
        private static GUIContent[] labels;
        private static GUIContent dividerBoxLabel = new GUIContent();
        private static GUILayoutOption[] dividerThing = new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) };
        private static string genericDebugToolTip = "Some options for control stuff added by Debuger.AddSomthing";

        private static Dictionary<GeneralXZData, ChunkDebugInfo> debugData = new Dictionary<GeneralXZData, ChunkDebugInfo>();


        //lock
        private static object lockObj = new object();

        #region generic
        private static List<HandleThing> labelsDebug = new List<HandleThing>();
        private static List<PointData> genericDots = new List<PointData>();
        private static List<LineData> genericLines = new List<LineData>();
        private static List<TriangleData> genericTris = new List<TriangleData>();
        private static List<LineData> pathDebug = new List<LineData>();
        #endregion

        private static int cellCounter;
        private static int coversCounter;
        private static int jumpBasesCounter;
        private static int voxelsCounter;


        static Debuger_K() {
            labels = new GUIContent[FLAGS_AMOUNT];
            labels[(int)DebugOptions.Cell] = new GUIContent("Cell", "");
            labels[(int)DebugOptions.CellArea] = new GUIContent("Cell Area", "");
            labels[(int)DebugOptions.CellConnection] = new GUIContent("Cell Connection", "");
            labels[(int)DebugOptions.CellEdges] = new GUIContent("Cell Edge", "");
            labels[(int)DebugOptions.Cover] = new GUIContent("Cover", "");
            labels[(int)DebugOptions.Grid] = new GUIContent("Grid", "");
            labels[(int)DebugOptions.JumpBase] = new GUIContent("Jumb Base", "");
            labels[(int)DebugOptions.Voxels] = new GUIContent("Voxels", "");
            labels[(int)DebugOptions.VoxelMax] = new GUIContent("Voxel Max", "");
            labels[(int)DebugOptions.VoxelMin] = new GUIContent("Voxel Min", "");
            labels[(int)DebugOptions.VoxelVolume] = new GUIContent("Voxel Volume", "");
            labels[(int)DebugOptions.VoxelConnection] = new GUIContent("Voxel Connection", "");
            labels[(int)DebugOptions.VoxelHash] = new GUIContent("Voxel Hash", "");
            labels[(int)DebugOptions.ChunkBounds] = new GUIContent("Chunk Bounds", "");
            labels[(int)DebugOptions.ColliderBounds] = new GUIContent("Collider Bounds", "");
            labels[(int)DebugOptions.NodesAndConnections] = new GUIContent("Nodes Info", "");
            labels[(int)DebugOptions.NodesAndConnectionsPreRDP] = new GUIContent("Nodes Info Pre RDP", "");
            labels[(int)DebugOptions.WalkablePolygons] = new GUIContent("Walkable Polygons", "");
            labels[(int)DebugOptions.Triangulator] = new GUIContent("Triangulator", "");
        } 
           
        public static void Init() {
            if (areInit)
                return;

            SetSettings();

            //scene object
            string sceneName = PFDSettings.LoadSettings().sceneName;
            GameObject go = GameObject.Find(sceneName);
            if (go == null) {
                go = new GameObject(sceneName);          
            }
            sceneObj = go.GetComponent<PFDDebugerScene>();
            if (sceneObj == null)
                go.AddComponent<PFDDebugerScene>();

            sceneObj.SetUpdateDeletage(() => {
                //path
                if (needPathUpdate) {
                    needPathUpdate = false;                    
                    sceneObj.UpdatePathData(settings.doDebugPaths ? pathDebug : new List<LineData>());
                }

                //generic dots
                if (needGenericDotUpdate) {
                    needGenericDotUpdate = false;
                    sceneObj.UpdateGenericDots(settings.drawGenericDots ? genericDots : new List<PointData>());
                }

                //generic lines
                if (needGenericLineUpdate) {
                    needGenericLineUpdate = false;
                    sceneObj.UpdateGenericLines(settings.drawGenericLines ? genericLines : new List<LineData>());
                }
          
                //generic tris
                if (needGenericTrisUpdate) {
                    needGenericTrisUpdate = false;
                    sceneObj.UpdateGenericTris(settings.drawGenericMesh ? genericTris : new List<TriangleData>());
                }
            });

            areInit = true;
        }

        public static void ForceInit() {
            areInit = false;
            Init();
        }

        private static void SetSettings() {
            //debugOptions = Enum.GetValues(typeof(PFDOptionEnum)).Cast<PFDOptionEnum>().ToList();
            settings = PFDSettings.LoadSettings();

            var curFlags = settings.debugFlags;
            if (settings.debugFlags == null) {
                settings.debugFlags = new bool[FLAGS_AMOUNT];
            }
            settings.debugFlags = new bool[FLAGS_AMOUNT]; 
            for (int i = 0; i < Math.Min(FLAGS_AMOUNT, curFlags.Length); i++) {
                settings.debugFlags[i] = curFlags[i];
            }

            //foreach (PFDOptionEnum option in debugOptions) {
            //    handlesCounter.Add(option, 0);
            //}

            if (settings.optionColors == null) {
                settings.optionColors = new List<Color>();
                settings.optionIsShows = new List<bool>();
            }

            if (settings.optionColors.Count != settings.optionIsShows.Count) {
                Debug.LogWarning("somehow debug options list count of colors and showings are not equal. fixing it");
                settings.optionColors = new List<Color>();
                settings.optionIsShows = new List<bool>();
            }

            //int targetCount = Enum.GetValues(typeof(PFDOptionEnum)).Length;
                    
            //if (settings.optionColors.Count > targetCount | settings.optionIsShows.Count > targetCount) {
            //    Debug.Log(settings.optionColors.Count > targetCount);
            //    Debug.Log(settings.optionIsShows.Count > targetCount);
            //    settings.optionColors.RemoveRange(targetCount, settings.optionColors.Count - targetCount);
            //    SetSettingsDirty();
            //}

            //if (settings.optionColors.Count < targetCount | settings.optionIsShows.Count < targetCount) {
            //    int targetAmount = targetCount - settings.optionIsShows.Count;     

            //    for (int i = 0; i < targetAmount; i++) {
            //        settings.optionColors.Add(Color.white);
            //        settings.optionIsShows.Add(false);
            //    }

            //    SetSettingsDirty();
            //}       
        }

        public static void SetSettingsDirty() {
            EditorUtility.SetDirty(settings);
        }
        
        public static void ClearChunksDebug() {
            cellCounter = 0;
            coversCounter = 0;
            jumpBasesCounter = 0;
            voxelsCounter = 0;

            foreach (var info in debugData.Values) {
                info.Clear();
            }  

            UpdateSceneImportantThings();
        }

        public static void ClearChunksDebug(int x, int z, AgentProperties properties) {
            ClearChunksDebug(new GeneralXZData(x, z, properties));
        }
        public static void ClearChunksDebug(GeneralXZData data) {
            bool changed = false;
            lock (lockObj) {
                ChunkDebugInfo info;
                if (debugData.TryGetValue(data, out info)) {
                    cellCounter -= info.cellCounter;
                    coversCounter -= info.coversCounter;
                    jumpBasesCounter -= info.jumpBasesCounter;
                    voxelsCounter -= info.voxelsCounter;
                    info.Clear();
                    changed = true;
                }                
            }
            if(changed)
                UpdateSceneImportantThings();
        }

        public static void DrawSceneGUI() {
            //if (PathFinder.activeCreationWork != 0 | PathFinder.haveActiveThreds | settings.showSceneGUI == false)
            //    return;

            //lock (debugData) {
            //    foreach (var chunkDictionary in debugData) {
            //        Vector3 pos = chunkDictionary.Key.centerV3;
            //        Vector3 screenPoint = Camera.current.WorldToViewportPoint(pos);
            //        if (screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1) {
            //            Vector3 screenPosition = Camera.current.WorldToScreenPoint(pos);

            //            GUILayout.BeginArea(new Rect(new Vector2(screenPosition.x, Screen.height - screenPosition.y), new Vector2(400, 400)));
            //            lock (chunkDictionary.Value) {
            //                foreach (var agentDictionary in chunkDictionary.Value) {
            //                    GUILayout.BeginHorizontal();
            //                    agentDictionary.Value.showMe = GUILayout.Toggle(agentDictionary.Value.showMe, "", GUILayout.MaxWidth(10));
            //                    GUILayout.Box(agentDictionary.Key.name);
            //                    GUILayout.EndHorizontal();
            //                }
            //            }
            //            GUILayout.EndArea();
            //        }
            //    }
            //}
        }

        public static bool doDebug {
            get {return areInit && settings.doDebug;}
        }
        public static bool debugOnlyNavMesh {
            get { return areInit && settings.debugOnlyNavmesh == false; }
        }
        public static bool useProfiler {
            get { return areInit && settings.doProfilerStuff; }
        }        
        public static bool debugPath {
            get { return areInit && settings.doDebugPaths; }
        }
                
        public static void DrawDebugLabels() {
            lock (labelsDebug) {
                if (settings.drawGenericLabels) {
                    for (int i = 0; i < labelsDebug.Count; i++) {
                        labelsDebug[i].ShowHandle();
                    }
                }
            }
        }
                        
        public static void SellectorGUI2() {
            settings.autoUpdateSceneView = GUILayout.Toggle(settings.autoUpdateSceneView, "auto update scene view");

            if (GUILayout.Button("Update")) {
                UpdateSceneImportantThings();
            }
            GUILayout.Box(dividerBoxLabel, dividerThing);
            var flags = settings.debugFlags;

            GUILayout.Label(string.Format(
                "Cells: {0}\nVoxels :{1}\nCovers: {2}\nJump Bases: {3}", cellCounter, voxelsCounter, coversCounter, jumpBasesCounter
                ), GUILayout.ExpandWidth(false));
            GUILayout.Box(dividerBoxLabel, dividerThing);
            //cells
            flags[(int)DebugOptions.Cell] = EditorGUILayout.Toggle(labels[(int)DebugOptions.Cell], flags[(int)DebugOptions.Cell]);
            if (flags[(int)DebugOptions.Cell]){
                flags[(int)DebugOptions.CellArea] = EditorGUILayout.Toggle(labels[(int)DebugOptions.CellArea], flags[(int)DebugOptions.CellArea]);
                flags[(int)DebugOptions.CellConnection] = EditorGUILayout.Toggle(labels[(int)DebugOptions.CellConnection], flags[(int)DebugOptions.CellConnection]);
                flags[(int)DebugOptions.CellEdges] = EditorGUILayout.Toggle(labels[(int)DebugOptions.CellEdges], flags[(int)DebugOptions.CellEdges]);
            }

            GUILayout.Box(dividerBoxLabel, dividerThing);

            //voxels
            flags[(int)DebugOptions.Voxels] = EditorGUILayout.Toggle(labels[(int)DebugOptions.Voxels], flags[(int)DebugOptions.Voxels]);
            if (flags[(int)DebugOptions.Voxels]) {
                flags[(int)DebugOptions.VoxelMax] = EditorGUILayout.Toggle(labels[(int)DebugOptions.VoxelMax], flags[(int)DebugOptions.VoxelMax]);
                flags[(int)DebugOptions.VoxelMin] = EditorGUILayout.Toggle(labels[(int)DebugOptions.VoxelMin], flags[(int)DebugOptions.VoxelMin]);
                flags[(int)DebugOptions.VoxelVolume] = EditorGUILayout.Toggle(labels[(int)DebugOptions.VoxelVolume], flags[(int)DebugOptions.VoxelVolume]);
                flags[(int)DebugOptions.VoxelConnection] = EditorGUILayout.Toggle(labels[(int)DebugOptions.VoxelConnection], flags[(int)DebugOptions.VoxelConnection]);
                flags[(int)DebugOptions.VoxelHash] = EditorGUILayout.Toggle(labels[(int)DebugOptions.VoxelHash], flags[(int)DebugOptions.VoxelHash]);
            }

            GUILayout.Box(dividerBoxLabel, dividerThing);
            flags[(int)DebugOptions.Cover] = EditorGUILayout.Toggle(labels[(int)DebugOptions.Cover], flags[(int)DebugOptions.Cover]);
            flags[(int)DebugOptions.Grid] = EditorGUILayout.Toggle(labels[(int)DebugOptions.Grid], flags[(int)DebugOptions.Grid]);
            flags[(int)DebugOptions.JumpBase] = EditorGUILayout.Toggle(labels[(int)DebugOptions.JumpBase], flags[(int)DebugOptions.JumpBase]);
            flags[(int)DebugOptions.ChunkBounds] = EditorGUILayout.Toggle(labels[(int)DebugOptions.ChunkBounds], flags[(int)DebugOptions.ChunkBounds]);
            flags[(int)DebugOptions.ColliderBounds] = EditorGUILayout.Toggle(labels[(int)DebugOptions.ColliderBounds], flags[(int)DebugOptions.ColliderBounds]);
            flags[(int)DebugOptions.NodesAndConnections] = EditorGUILayout.Toggle(labels[(int)DebugOptions.NodesAndConnections], flags[(int)DebugOptions.NodesAndConnections]);
            flags[(int)DebugOptions.NodesAndConnectionsPreRDP] = EditorGUILayout.Toggle(labels[(int)DebugOptions.NodesAndConnectionsPreRDP], flags[(int)DebugOptions.NodesAndConnectionsPreRDP]);
            flags[(int)DebugOptions.WalkablePolygons] = EditorGUILayout.Toggle(labels[(int)DebugOptions.WalkablePolygons], flags[(int)DebugOptions.WalkablePolygons]);
            flags[(int)DebugOptions.Triangulator] = EditorGUILayout.Toggle(labels[(int)DebugOptions.Triangulator], flags[(int)DebugOptions.Triangulator]);
            GUILayout.Box(dividerBoxLabel, dividerThing);            

            if (GUI.changed && settings.autoUpdateSceneView)
                UpdateSceneImportantThings();
        }

        public static void UpdateSceneImportantThings() {
            List<PointData> newPointData = new List<PointData>();
            List<LineData> newLineData = new List<LineData>();
            List<TriangleData> newTrisData = new List<TriangleData>();
            var flags = settings.debugFlags;
            lock (lockObj) {
                foreach (var info in debugData.Values) {
                    if (!info.showMe)
                        continue;


                    if (flags[(int)DebugOptions.Cell]) {
                        if (flags[(int)DebugOptions.CellArea])
                            newTrisData.AddRange(info.cellsArea);
                        if (flags[(int)DebugOptions.CellEdges])
                            newLineData.AddRange(info.cellEdges);
                        if (flags[(int)DebugOptions.CellConnection])
                            newLineData.AddRange(info.cellConnections);
                    }

                    if (flags[(int)DebugOptions.Voxels]) {
                        if (flags[(int)DebugOptions.VoxelMax])
                            newPointData.AddRange(info.voxelMax);
                        if (flags[(int)DebugOptions.VoxelMin])
                            newPointData.AddRange(info.voxelMin);
                        if (flags[(int)DebugOptions.VoxelHash])
                            newPointData.AddRange(info.voxelHash);
                        if (flags[(int)DebugOptions.VoxelVolume])
                            newLineData.AddRange(info.voxelVolume);
                        if (flags[(int)DebugOptions.VoxelConnection])
                            newLineData.AddRange(info.voxelConnections);
                    }

                    if (flags[(int)DebugOptions.JumpBase]) {
                        newLineData.AddRange(info.jumpBasesLines);
                        newPointData.AddRange(info.jumpBasesDots);
                    }

                    if (flags[(int)DebugOptions.Cover]) {
                        newPointData.AddRange(info.coverDots);
                        newLineData.AddRange(info.coverLines);
                        newTrisData.AddRange(info.coverSheets);
                    }

                    if (flags[(int)DebugOptions.Grid])
                        newLineData.AddRange(info.grid);

                    if (flags[(int)DebugOptions.ChunkBounds])
                        newLineData.AddRange(info.chunkBounds);

                    if (flags[(int)DebugOptions.ColliderBounds])
                        newLineData.AddRange(info.colliderBounds);

                    if (flags[(int)DebugOptions.NodesAndConnections]) {
                        newLineData.AddRange(info.nodesLines);
                        newPointData.AddRange(info.nodesPoints);
                    }

                    if (flags[(int)DebugOptions.NodesAndConnectionsPreRDP]) {
                        newLineData.AddRange(info.nodesLinesPreRDP);
                        newPointData.AddRange(info.nodesPointsPreRDP);
                    }

                    if (flags[(int)DebugOptions.WalkablePolygons]) {
                        newLineData.AddRange(info.walkablePolygonLine);
                        newTrisData.AddRange(info.walkablePolygonSheet);
                    }

                    if (flags[(int)DebugOptions.Triangulator]) {
                        newLineData.AddRange(info.triangulator);
                    }                    
                }
            }

            sceneObj.UpdateImportantData(newPointData, newLineData, newTrisData);
        }
           
        public static void GenericGUI() {
            lock (lockObj) {
                bool tempBool;

                tempBool = settings.drawGenericLines;
                settings.drawGenericLines = EditorGUILayout.Toggle(new GUIContent("Lines " + genericLines.Count, genericDebugToolTip), settings.drawGenericLines);
                if (tempBool != settings.drawGenericLines)
                    needGenericLineUpdate = true;

                tempBool = settings.drawGenericDots;
                settings.drawGenericDots = EditorGUILayout.Toggle(new GUIContent("Dots " + genericDots.Count, genericDebugToolTip), settings.drawGenericDots);
                if (tempBool != settings.drawGenericDots)
                    needGenericDotUpdate = true;

                tempBool = settings.drawGenericMesh;
                settings.drawGenericMesh = EditorGUILayout.Toggle(new GUIContent("Meshes " + genericTris.Count, genericDebugToolTip), settings.drawGenericMesh);
                if (tempBool != settings.drawGenericMesh)
                    needGenericTrisUpdate = true;

                tempBool = settings.drawPaths;
                settings.drawPaths = EditorGUILayout.Toggle(new GUIContent("paths " + pathDebug.Count, "this will debug paths. object to change"), settings.drawPaths);
                if (tempBool != settings.drawPaths)
                    needPathUpdate = true; 

                //update on it's own
                settings.drawGenericLabels = EditorGUILayout.Toggle(new GUIContent("labels " + labelsDebug.Count, genericDebugToolTip), settings.drawGenericLabels);
            }
        }
        
        #region generic
        public static void AddLabel(Vector3 pos, string text, DebugGroup group = DebugGroup.label) {
            lock (labelsDebug) {
                labelsDebug.Add(new DebugLabel(pos, text));
            }
        }
        public static void AddLabel(Vector3 pos, double number, int digitsRound = 2, DebugGroup group = DebugGroup.label) {
            AddLabel(pos, Math.Round(number, digitsRound).ToString(), group);
        }
        public static void AddLabel(Vector3 pos, object obj, DebugGroup group = DebugGroup.label) {
            AddLabel(pos, obj.ToString(), group);
        }

        //add things to lists
        private static void AddGenericDot(PointData data) {
            lock (genericDots)
                genericDots.Add(data);
            if(settings.drawGenericDots)
                needGenericDotUpdate = true;
        }
        private static void AddGenericDot(IEnumerable<PointData> datas) {
            lock (genericDots)
                genericDots.AddRange(datas);
            if (settings.drawGenericDots)
                needGenericDotUpdate = true;
        }
        private static void AddGenericLine(LineData data) {
            lock (genericLines)
                genericLines.Add(data);
            if (settings.drawGenericLines)
                needGenericLineUpdate = true;
        }
        private static void AddGenericLine(IEnumerable<LineData> datas) {
            lock (genericLines)
                genericLines.AddRange(datas);
            if (settings.drawGenericLines)
                needGenericLineUpdate = true;
        }
        private static void AddGenericTriangle(TriangleData data) {
            lock (genericTris)
                genericTris.Add(data);
            if (settings.drawGenericMesh)
                needGenericTrisUpdate = true;

        }
        private static void AddGenericTriangle(IEnumerable<TriangleData> datas) {
            lock (genericTris)
                genericTris.AddRange(datas);
            if (settings.drawGenericMesh)
                needGenericTrisUpdate = true;
        }
        
        //dot
        public static void AddDot(Vector3 pos, Color color, float size = 0.02f) {
            AddGenericDot(new PointData(pos, color, size));
        }
        public static void AddDot(IEnumerable<Vector3> pos, Color color, float size = 0.02f) {
            List<PointData> pd = new List<PointData>();
            foreach (var item in pos) {
                pd.Add(new PointData(item, color, size));
            }
            AddGenericDot(pd);
        }
        public static void AddDot(Vector3 pos, float size = 0.02f) {
            AddGenericDot(new PointData(pos, Color.black, size));
        }
        public static void AddDot(IEnumerable<Vector3> pos, float size = 0.02f) {
            List<PointData> pd = new List<PointData>();
            foreach (var item in pos) {
                pd.Add(new PointData(item, Color.black, size));
            }
            AddGenericDot(pd);
        }
        
        //line
        public static void AddLine(Vector3 v1, Vector3 v2, Color color, float addOnTop = 0f, float width = 0.001f) {
            AddGenericLine(new LineData(v1 + V3small(addOnTop), v2 + V3small(addOnTop), color, width));
        }        
        public static void AddLine(Vector3 v1, Vector3 v2, float addOnTop = 0f, float width = 0.001f) {
            AddLine(v1, v2, Color.black, addOnTop, width);
        }
        public static void AddLine(CellContentData data, Color color, float addOnTop = 0f, float width = 0.001f) {
            AddLine(data.leftV3 + V3small(addOnTop), data.rightV3 + V3small(addOnTop), color, width);
        }
        public static void AddLine(CellContentData data, float addOnTop = 0f, float width = 0.001f) {
            AddLine(data.leftV3 + V3small(addOnTop), data.rightV3 + V3small(addOnTop), Color.black, width);
        }
        public static void AddLine(Vector3[] chain, Color color, float addOnTop = 0f, float width = 0.001f) {
            int length = chain.Length;
            if (length < 2)
                return;
            LineData[] ld = new LineData[length - 1];
            for (int i = 0; i < length - 1; i++) {
                ld[i] = new LineData(chain[i] + V3small(addOnTop), chain[i + 1] + V3small(addOnTop), color, width);
            }
            AddGenericLine(ld);
        }
        public static void AddLine(Vector3[] chain, float addOnTop = 0f, float width = 0.001f) {
            AddLine(chain, Color.black, width);
        }
        public static void AddLine(float addOnTop = 0f, float width = 0.001f, params Vector3[] chain) {
            AddLine(chain, Color.black, width);
        }
        public static void AddLine(Color color, float addOnTop = 0f, float width = 0.001f, params Vector3[] chain) {
            AddLine(chain, color, width);
        }
        //some fancy expensive shit when no colors left
        public static void AddLine(Vector3 v1, Vector3 v2, Color color1, Color color2, int subdivisions, float addOnTop = 0f, float width = 0.001f) {
            List<LineData> ld = new List<LineData>();
            float step = 1f / subdivisions;
            bool flip = false;
            for (int i = 0; i < subdivisions; i++) {           
                ld.Add(new LineData(Vector3.Lerp(v1, v2, Mathf.Clamp01(step * i)) + V3small(addOnTop), Vector3.Lerp(v1, v2, Mathf.Clamp01(step * (i + 1))) + V3small(addOnTop), flip ? color1 : color2, width));
                flip = !flip;
            }
            AddGenericLine(ld);
        }
        public static void AddLine(Vector3 v1, Vector3 v2, Color color1, Color color2, float subdivisionLength, float addOnTop = 0f, float width = 0.001f) {
            AddLine(v1, v2, color1, color2, Mathf.FloorToInt(Vector3.Distance(v1, v2) / subdivisionLength), addOnTop, width);
        }


        public static void AddRay(Vector3 point, Vector3 direction, Color color, float length = 1f, float width = 0.001f) {
            AddLine(point, point + (direction.normalized * length), color, width);
        }
        public static void AddRay(Vector3 point, Vector3 direction, float length = 1f, float width = 0.001f) {
            AddRay(point, direction, Color.black, width);
        }

        public static void AddBounds(Bounds b, Color color, float width = 0.001f) {
            AddGenericLine(BuildParallelepiped(b.center - b.size, b.center + b.size, color, width));
        }
        public static void AddBounds(Bounds b, float width = 0.001f) {
            AddBounds(b, Color.blue, width);
        }

        //path
        public static void AddPath(Vector3 v1, Vector3 v2, Color color, float addOnTop = 0f, float width = 0.001f) {
            lock (pathDebug)
                pathDebug.Add(new LineData(v1 + V3small(addOnTop), v2 + V3small(addOnTop), color, width));
            needPathUpdate = true;
        }

        //geometry
        public static void AddTriangle(Vector3 A, Vector3 B, Vector3 C, Color color, bool outline = true, float outlineWidth = 0.001f) {
            AddGenericTriangle(new TriangleData(A, B, C, color));
            if (outline) {
                Color oColor = new Color(color.r, color.g, color.b, 1f);
                AddGenericLine(new LineData[]{
                    new LineData(A, B, oColor, outlineWidth),
                    new LineData(B, C, oColor, outlineWidth),
                    new LineData(C, A, oColor, outlineWidth)
                });
            }
        }
        public static void AddQuad(Vector3 bottomLeft, Vector3 upperLeft, Vector3 bottomRight, Vector3 upperRight, Color color, bool outline = true, float outlineWidth = 0.001f) {
            AddGenericTriangle(new TriangleData(bottomLeft, upperLeft, bottomRight, color));
            AddGenericTriangle(new TriangleData(upperLeft, bottomRight, upperRight, color));
            if (outline) {
                Color oColor = new Color(color.r, color.g, color.b, 1f);
                AddGenericLine(new LineData[]{
                    new LineData(bottomLeft, upperLeft, oColor, outlineWidth),
                    new LineData(upperLeft, upperRight, oColor, outlineWidth),
                    new LineData(upperRight, bottomRight, oColor, outlineWidth),
                    new LineData(bottomRight, bottomLeft, oColor, outlineWidth)
                });
            }
        }
        public static void AddMesh(Vector3[] verts, int[] tris, Color color, bool outline = true, float outlineWidth = 0.001f) {
            TriangleData[] td = new TriangleData[tris.Length / 3];
            for (int i = 0; i < tris.Length; i += 3) {
                td[i / 3] = new TriangleData(verts[tris[i]], verts[tris[i + 1]], verts[tris[i + 2]], color);
            }
            AddGenericTriangle(td);

            if (outline) {
                Color oColor = new Color(color.r, color.g, color.b, 1f);
                LineData[] ld = new LineData[tris.Length];

                for (int i = 0; i < tris.Length; i += 3) {
                    ld[i] = new LineData(verts[tris[i]], verts[tris[i + 1]], oColor, outlineWidth);
                    ld[i + 1] = new LineData(verts[tris[i + 1]], verts[tris[i + 2]], oColor, outlineWidth);
                    ld[i + 2] = new LineData(verts[tris[i + 2]], verts[tris[i]], oColor, outlineWidth);
                }
                AddGenericLine(ld);
            }
        }

        //public static void AddWireMesh(Vector3[] verts, int[] tris, DebugGroup group = DebugGroup.line) {
        //    AddGeneric(group, BuildWireMesh(verts, tris, Color.white));
        //}

        //public static void AddWireMesh(Vector3[] verts, int[] tris, Color color, DebugGroup group = DebugGroup.line) {
        //    AddGeneric(group, BuildWireMesh(verts, tris, color));
        //}

        //private static List<HandleThing> GenerateCapsule(Vector3 bottom, Vector3 top, float radius, float dotSize, Color color) {
        //    List<HandleThing> result = new List<HandleThing>();

        //    result.Add(new DebugDotColored(bottom, dotSize, color));
        //    result.Add(new DebugDotColored(top, dotSize, color));
        //    result.Add(new DebugLineAAColored(bottom, top, color));

        //    Vector3 normal = (top - bottom).normalized;
        //    result.Add(new DebugWireDisc(top, normal, radius, color));
        //    result.Add(new DebugWireDisc(bottom, normal, radius, color));

        //    Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.LookRotation(normal, Vector3.up), Vector3.one);

        //    result.Add(new DebugLineAAColored(matrix.MultiplyPoint3x4(new Vector3(radius, 0, 0)) + bottom, matrix.MultiplyPoint3x4(new Vector3(radius, 0, 0)) + top, color));
        //    result.Add(new DebugLineAAColored(matrix.MultiplyPoint3x4(new Vector3(-radius, 0, 0)) + bottom, matrix.MultiplyPoint3x4(new Vector3(-radius, 0, 0)) + top, color));

        //    result.Add(new DebugLineAAColored(matrix.MultiplyPoint3x4(new Vector3(0, radius, 0)) + bottom, matrix.MultiplyPoint3x4(new Vector3(0, radius, 0)) + top, color));
        //    result.Add(new DebugLineAAColored(matrix.MultiplyPoint3x4(new Vector3(0, -radius, 0)) + bottom, matrix.MultiplyPoint3x4(new Vector3(0, -radius, 0)) + top, color));

        //    result.Add(new DebugWireArc(top, matrix.MultiplyPoint3x4(new Vector3(radius, 0, 0)), matrix.MultiplyPoint3x4(new Vector3(0, radius, 0)), 180, radius, color));
        //    result.Add(new DebugWireArc(top, matrix.MultiplyPoint3x4(new Vector3(0, radius, 0)), matrix.MultiplyPoint3x4(new Vector3(-radius, 0, 0)), 180, radius, color));

        //    result.Add(new DebugWireArc(bottom, matrix.MultiplyPoint3x4(new Vector3(radius, 0, 0)), matrix.MultiplyPoint3x4(new Vector3(0, -radius, 0)), 180, radius, color));
        //    result.Add(new DebugWireArc(bottom, matrix.MultiplyPoint3x4(new Vector3(0, radius, 0)), matrix.MultiplyPoint3x4(new Vector3(radius, 0, 0)), 180, radius, color));
        //    return result;
        //}

        //public static void AddCapsule(Vector3 bottom, Vector3 top, float radius, Color color, float dotSize = 0.02f, DebugGroup group = DebugGroup.generic) {
        //    AddGeneric(group, GenerateCapsule(bottom, top, radius, dotSize, color));
        //}

        //public static void AddPolyLine(Vector3[] value, Color color, DebugGroup group = DebugGroup.line) {
        //    AddGeneric(group, new DebugPolyLine(color, false, value));
        //}




        //some clears
        public static void ClearGeneric(DebugGroup group) {
            switch (group) {
                case DebugGroup.line:
                    lock (genericLines) {
                        genericLines.Clear();
                    }
                    needGenericLineUpdate = true;
                    break;
                case DebugGroup.dot:
                    lock (genericDots) {
                        genericDots.Clear();
                    }
                    needGenericDotUpdate = true;
                    break;
                case DebugGroup.label: //labels are dont need update
                    lock (labelsDebug) {
                        labelsDebug.Clear();
                    }
                    break;
                case DebugGroup.mesh:
                    lock (genericTris) {
                        genericTris.Clear();
                    }
                    needGenericTrisUpdate = true;
                    break;
                case DebugGroup.path:
                    lock (pathDebug) {
                        pathDebug.Clear();
                    }
                    needPathUpdate = true;
                    break;       
            }
        }
        public static void ClearGeneric() {
            lock (pathDebug) {
                pathDebug.Clear();
            }
            lock (genericLines) {
                genericLines.Clear();
            }
            lock (genericDots) {
                genericDots.Clear();
            }
            lock (labelsDebug) {
                labelsDebug.Clear();
            }
            lock (genericTris) {
                genericTris.Clear();
            }
            needPathUpdate = true;
            needGenericDotUpdate = true;
            needGenericLineUpdate = true;
            needGenericTrisUpdate = true;
        }             
        public static void ClearLabels() {
            ClearGeneric(DebugGroup.label);           
        }
        public static void ClearLines() {
            ClearGeneric(DebugGroup.line);          
        }
        public static void ClearDots() {
            ClearGeneric(DebugGroup.dot);  
        }
        public static void ClearMeshes() {
            ClearGeneric(DebugGroup.mesh);
        }
        public static void ClearPath() {
            ClearGeneric(DebugGroup.path);
        }

        //error shortcuts //currently generic
        public static void AddErrorDot(Vector3 pos, Color color, float size = 0.1f) {
            AddDot(pos, color, size);
        }
        public static void AddErrorDot(Vector3 pos, float size = 0.1f) {
            AddErrorDot(pos, Color.red, 0.1f);
        }

        public static void AddErrorLine(Vector3 v1, Vector3 v2, Color color, float add = 0f) {
            AddLine(v1, v2, color, add);
        }
        public static void AddErrorLine(Vector3 v1, Vector3 v2, float add = 0f) {
            AddErrorLine(v1, v2, Color.red, add);
        }

        public static void AddErrorLabel(Vector3 pos, string text) {
            AddLabel(pos, text);
        }
        public static void AddErrorLabel(Vector3 pos, object text) {
            AddErrorLabel(pos, text.ToString());
        }
        #endregion

        #region add important
        //important
        private static ChunkDebugInfo GetInfo(GeneralXZData key) {
            lock (lockObj) {
                ChunkDebugInfo info;
                if (debugData.TryGetValue(key, out info) == false) {
                    info = new ChunkDebugInfo();
                    debugData.Add(key, info);
                    //Bounds bounds = chunk.bounds;
                    //info.chunkBounds.AddRange(BuildParallelepiped(bounds.center - bounds.size, bounds.center + bounds.size, Color.gray, 0.001f));
                }
                return info;
            }
        }

        private static ChunkDebugInfo GetInfo(int x, int z, AgentProperties properties) {
            return GetInfo(new GeneralXZData(x, z, properties));
        }

        public static void AddCells(int x, int z, AgentProperties properties, IEnumerable<Cell> cells) {
            Vector3 offsetLD = new Vector3(-0.015f, 0f, -0.015f);
            Vector3 offsetRT = new Vector3(0.015f, 0f, 0.015f);

            List<TriangleData> cellsAreaNewData = new List<TriangleData>();
            List<LineData> cellEdgesNewData = new List<LineData>();
            List<LineData> cellConnectionsNewData = new List<LineData>();

            foreach (var cell in cells) {
                Color areaColor = cell.area.color;

                if (cell.passability == Passability.Crouchable)
                    areaColor *= 0.2f;

                areaColor = new Color(areaColor.r, areaColor.g, areaColor.b, 0.1f);

                foreach (var oe in cell.originalEdges) {
                    cellEdgesNewData.Add(new LineData(oe.a, oe.b, Color.black, 0.001f));
                    cellsAreaNewData.Add(new TriangleData(oe.a, oe.b, cell.centerV3, areaColor));
                }


                foreach (var cutContent in cell.connections) {
                    if (cutContent is CellContentGenericConnection) {
                        var val = cutContent as CellContentGenericConnection;
                        cellConnectionsNewData.Add(new LineData(cell.centerV3, val.intersection, Color.white, 0.0008f));
                    }

                    if (cutContent is CellContentPointedConnection) {
                        var val = cutContent as CellContentPointedConnection;

                        Color color;             
                        if (val.jumpState == ConnectionJumpState.jumpUp) {
                            color = Color.yellow;
                            cellConnectionsNewData.Add(new LineData(val.enterPoint + offsetLD, val.lowerStandingPoint + offsetLD, color, 0.001f));
                            cellConnectionsNewData.Add(new LineData(val.lowerStandingPoint + offsetLD, val.axis + offsetLD, color, 0.001f));
                            cellConnectionsNewData.Add(new LineData(val.axis + offsetLD, val.exitPoint + offsetLD, color, 0.001f));
                        }
                        else {
                            color = Color.blue;
                            cellConnectionsNewData.Add(new LineData(val.enterPoint + offsetRT, val.axis + offsetRT, color, 0.001f));
                            cellConnectionsNewData.Add(new LineData(val.axis + offsetRT, val.lowerStandingPoint + offsetRT, color, 0.001f));
                            cellConnectionsNewData.Add(new LineData(val.lowerStandingPoint + offsetRT, val.exitPoint + offsetRT, color, 0.001f));                        
                        }                 
                    }
                }
            }

            ChunkDebugInfo info = GetInfo(x, z, properties);

            lock (lockObj) {
                cellCounter += cells.Count();
                info.cellCounter = cells.Count();
                info.cellsArea.AddRange(cellsAreaNewData);
                info.cellEdges.AddRange(cellEdgesNewData);
                info.cellConnections.AddRange(cellConnectionsNewData);
            }

        }
        public static void AddEdgesInterconnected(int x, int z, AgentProperties properties, CellContentGenericConnection connection) {
            ChunkDebugInfo info = GetInfo(x, z, properties);
            lock (lockObj) {
                info.cellConnections.Add(new LineData(connection.from.centerV3, connection.intersection, Color.white, 0.0008f));
            }
        }
        public static void AddVolumes(NavMeshTemplateRecast template, VolumeContainer volumeContainer) {
            float fragmentSize = 0.035f;

            bool doCover = template.doCover;
            int sizeX = volumeContainer.sizeX;
            int sizeZ = volumeContainer.sizeZ;

            VolumePos tempPos;

            System.Random random = new System.Random(template.GetHashCode());

            //////////////
            List<PointData> voxelMaxNewData = new List<PointData>();
            List<PointData> voxelMinNewData = new List<PointData>();
            List<LineData> voxelVolumeNewData = new List<LineData>();
            List<LineData> voxelConnectionsNewData = new List<LineData>();
            List<PointData> voxelHashNewData = new List<PointData>();
            /////////////

            foreach (var volume in volumeContainer.volumes) {
                //Color volumeColor = new Color((float)random.Next(0, 100) / 100f, (float)random.Next(0, 100) / 100f, (float)random.Next(0, 100) / 100f);

                lock (hashColorDictionary) {
                    foreach (var a in volume.containsAreas) {
                        int standHash = PathFinder.GetAreaHash(a, Passability.Walkable);
                        int crouchHash = PathFinder.GetAreaHash(a, Passability.Crouchable);

                        if (hashColorDictionary.ContainsKey(standHash) == false)
                            hashColorDictionary.Add(standHash, new Color((float)random.Next(0, 100) / 100f, (float)random.Next(0, 100) / 100f, (float)random.Next(0, 100) / 100f));

                        if (hashColorDictionary.ContainsKey(crouchHash) == false)
                            hashColorDictionary.Add(crouchHash, new Color((float)random.Next(0, 100) / 100f, (float)random.Next(0, 100) / 100f, (float)random.Next(0, 100) / 100f));
                    }
                }



                for (int x = 0; x < sizeX; x++) {
                    for (int z = 0; z < sizeZ; z++) {
                        if (volume.Exist(x, z) == false)
                            continue;

                        Vector3 posMax = volumeContainer.GetRealMax(x, z, volume);

                        Vector3 posMin = template.realOffsetedPosition
                            + (new Vector3(x, 0, z) * template.voxelSize)
                            + (template.halfFragmentOffset)
                            + new Vector3(0, volume.min[x][z], 0);

                        int passability = volume.passability[x][z];

                        switch (passability) {
                            case (int)Passability.Unwalkable:
                                voxelMaxNewData.Add(new PointData(posMax, Color.red, fragmentSize));
                                break;
                            case (int)Passability.Slope:
                                voxelMaxNewData.Add(new PointData(posMax, Color.magenta, fragmentSize));
                                break;
                            case (int)Passability.Crouchable:
                                voxelMaxNewData.Add(new PointData(posMax, volume.area[x][z].color * 0.2f, fragmentSize));
                                break;
                            case (int)Passability.Walkable:
                                voxelMaxNewData.Add(new PointData(posMax, volume.area[x][z].color, fragmentSize));
                                break;
                        }

                        voxelMinNewData.Add(new PointData(posMin, Color.black, fragmentSize));
                        voxelVolumeNewData.Add(new LineData(posMin, posMax, Color.gray, 0.001f));

                        int h = volume.hashMap[x][z];
                        if (h != 0)
                            voxelHashNewData.Add(new PointData(posMax, hashColorDictionary[h], fragmentSize));

                        for (int dir = 0; dir < 4; dir++) {
                            if (volumeContainer.TryGetLeveled(volume, x, z, (Directions)dir, out tempPos)) {
                                voxelConnectionsNewData.Add(new LineData(posMax + (Vector3.up * dir * 0.01f), volumeContainer.GetRealMax(tempPos) + (Vector3.up * dir * 0.01f), GetSomeColor(dir), 0.001f));
                            }
                        }

                        //if(volume.heightInterest[x][z])
                        //    heightInterest.Add(new DebugDotColored(posMax + (Vector3.up * 0.03f), fragmentSize, Color.white));
                        //if (doCover) {     
                        //    int c = volume.coverHashMap[x][z];

                        //    switch (c) {
                        //        case -1:
                        //            coverHash.Add(new DebugDotColored(posMax, fragmentSize, Color.black));
                        //            break;
                        //        case 0:
                        //            coverHash.Add(new DebugDotColored(posMax, fragmentSize, Color.red));
                        //            break;
                        //        case MarchingSquaresIterator.COVER_HASH:
                        //            coverHash.Add(new DebugDotColored(posMax, fragmentSize, Color.white));
                        //            break;
                        //        default:
                        //            coverHash.Add(new DebugDotColored(posMax, fragmentSize, Color.red));
                        //            break;
                        //    }
                        //}
                    }
                }
            }

            ChunkDebugInfo info = GetInfo(new GeneralXZData(template.gridPosition.x, template.gridPosition.z, template.properties));    

            lock (lockObj) {
                voxelsCounter += voxelMaxNewData.Count;
                info.voxelsCounter = voxelMaxNewData.Count;
                info.voxelMax.AddRange(voxelMaxNewData);
                info.voxelMin.AddRange(voxelMinNewData);
                info.voxelVolume.AddRange(voxelVolumeNewData);
                info.voxelConnections.AddRange(voxelConnectionsNewData);
                info.voxelHash.AddRange(voxelHashNewData);
            }
        }
        public static void AddBattleGrid(int x, int z, AgentProperties properties, BattleGrid bg) {
            List<LineData> gridNewData = new List<LineData>();

            foreach (var p in bg.points) {
                foreach (var n in p.neighbours) {
                    if (n != null)
                        gridNewData.Add(new LineData(p.positionV3, n.positionV3, Color.yellow, 0.001f) );       
                }
            }
            ChunkDebugInfo info = GetInfo(x, z, properties);

            lock (lockObj) {
                info.grid.AddRange(gridNewData);
            }
        }
        public static void AddCovers(int x, int z, AgentProperties properties, IEnumerable<Cover> covers) {
            List<PointData> coverDotsNewData = new List<PointData>();
            List<LineData> coverLinesNewData = new List<LineData>();
            List<TriangleData> coverSheetsNewData = new List<TriangleData>();

            Color hardColor = Color.magenta;
            Color softColor = new Color(hardColor.r, hardColor.g, hardColor.b, 0.2f);

            float slickLine = 0.0008f;
            float fatLine = 0.0015f;
            float dotSize = 0.04f;

            foreach (var cover in covers) {
                //bootom
                Vector3 BL = cover.right;
                Vector3 BR = cover.left;

                float height = 0;
                switch (cover.coverType) {
                    case 1:
                        height = properties.halfCover;
                        break;
                    case 2:
                        height = properties.fullCover;
                        break;
                    default:
                        break;
                }

                //top
                Vector3 TL = BL + (Vector3.up * height);
                Vector3 TR = BR + (Vector3.up * height);

                //top and bottom
                coverLinesNewData.Add(new LineData(BL, BR, hardColor, fatLine));
                coverLinesNewData.Add(new LineData(TL, TR, hardColor, fatLine));

                //sides
                coverLinesNewData.Add(new LineData(BL, TL, hardColor, slickLine));
                coverLinesNewData.Add(new LineData(BR, TR, hardColor, slickLine));

                coverSheetsNewData.Add(new TriangleData(BL, BR, TR, softColor));
                coverSheetsNewData.Add(new TriangleData(BL, TL, TR, softColor));          

                foreach (var point in cover.coverPoints) {
                    coverDotsNewData.Add(new PointData(point.positionV3, hardColor, dotSize));
                    coverDotsNewData.Add(new PointData(point.cellPos, hardColor, dotSize));

                    coverLinesNewData.Add(new LineData(point.positionV3, point.cellPos, hardColor, slickLine));
                    coverLinesNewData.Add(new LineData(TL, TR, hardColor, slickLine));
                }
            }

            ChunkDebugInfo info = GetInfo(x, z, properties);

            lock (lockObj) {
                coversCounter += covers.Count();
                info.coversCounter = covers.Count();
                info.coverDots.AddRange(coverDotsNewData);
                info.coverLines.AddRange(coverLinesNewData);
                info.coverSheets.AddRange(coverSheetsNewData);
            }
        }
        public static void AddPortalBases(int x, int z, AgentProperties properties, IEnumerable<JumpPortalBase> portalBases) {           
            List<PointData> jumpBasesDotsNewData = new List<PointData>();
            List<LineData> jumpBasesLinesNewData = new List<LineData>();

            foreach (var portalBase in portalBases) {
                foreach (var cellPoint in portalBase.cellMountPoints.Values) {
                    jumpBasesLinesNewData.Add(new LineData(portalBase.positionV3, cellPoint, Color.black, 0.001f));
                    jumpBasesDotsNewData.Add(new PointData(portalBase.positionV3,  Color.black, 0.04f));
                    jumpBasesDotsNewData.Add(new PointData(cellPoint, Color.black, 0.04f));
                }
            }


            ChunkDebugInfo info = GetInfo(x, z, properties);
            lock (lockObj) {
                jumpBasesCounter += portalBases.Count();
                info.jumpBasesCounter = portalBases.Count();
                info.jumpBasesDots.AddRange(jumpBasesDotsNewData);
                info.jumpBasesLines.AddRange(jumpBasesLinesNewData);
            }
        }

        //less important
        private static void GetNodesThings(AgentProperties properties, IEnumerable<NodeTemp> nodes, out List<PointData> nodesPos, out List<LineData> nodesConnectins, out List<HandleThing> nodesLabels) {
            nodesPos = new List<PointData>();
            nodesConnectins = new List<LineData>();
            nodesLabels = new List<HandleThing>();

            StringBuilder sb = new StringBuilder();

            foreach (var node in nodes) {
                nodesPos.Add(new PointData(node.positionV3, Color.blue, 0.01f));

                sb.Length = 0;
                foreach (var item in node.getData) {
                    NodeTemp connection = item.Value.connection;
                    int layer = item.Key.x;
                    int hash = item.Key.y;
                    sb.Append("L:{0}, H:{1}", layer, hash);

                    if (connection == null) {
                        Debug.LogError("NULL");
                        AddErrorLine(node.positionV3, node.positionV3, Color.red);
                        AddErrorLabel(node.positionV3, "NULL" + layer + " : " + hash);
                    }
                    else {
                        Vector3 conPos = connection.positionV3;
                        Vector3 midPoint = SomeMath.MidPoint(node.positionV3, conPos);
                        var edge = item.Value;

                        nodesConnectins.Add(
                            new LineData(node.positionV3, midPoint,
                            edge.GetFlag(EdgeTempFlags.DouglasPeukerMarker) ? Color.green : Color.blue, 0.001f));

                        nodesConnectins.Add(new LineData(midPoint, conPos, Color.red, 0.001f));
                    }
                }

                nodesLabels.Add(new DebugLabel(node.positionV3, sb.ToString()));
            }
        }
        public static void AddNodesTemp(int x, int z, AgentProperties properties, IEnumerable<NodeTemp> nodes) {
            List<HandleThing> nodesLabels;
            List<PointData> nodesPos;
            List<LineData> nodesConnectins;
            GetNodesThings(properties, nodes, out nodesPos, out nodesConnectins, out nodesLabels);

            ChunkDebugInfo info = GetInfo(x, z, properties);
            lock (lockObj) {
                info.nodesPoints.AddRange(nodesPos);
                info.nodesLines.AddRange(nodesConnectins);
            }            
        }
        public static void AddNodesTempPreRDP(int x, int z, AgentProperties properties, IEnumerable<NodeTemp> nodes) {
            List<HandleThing> nodesLabels;
            List<PointData> nodesPos;
            List<LineData> nodesConnectins;
            GetNodesThings(properties, nodes, out nodesPos, out nodesConnectins, out nodesLabels);

            ChunkDebugInfo info = GetInfo(x, z, properties);
            lock (lockObj) {
                info.nodesPointsPreRDP.AddRange(nodesPos);
                info.nodesLinesPreRDP.AddRange(nodesConnectins);
            }
        }

        //not important
        public static void AddColliderBounds(int x, int z, AgentProperties properties, Collider collider) {
            Bounds bounds = collider.bounds;
            var debugedBounds = BuildParallelepiped(bounds.center - bounds.size, bounds.center + bounds.size, Color.green, 0.001f);

            ChunkDebugInfo info = GetInfo(x, z, properties);
            lock (lockObj) {
                info.colliderBounds.AddRange(debugedBounds);
            }
        }
        public static void AddTreeCollider(int x, int z, AgentProperties properties, Bounds bounds, Vector3[] verts, int[] tris) {
            //AddHandle(chunk, properties, PFDOptionEnum.BoundsCollider, new DebugBounds(bounds));

            //List<HandleThing> mesh = new List<HandleThing>();
            //for (int i = 0; i < tris.Length; i += 3) {
            //    mesh.Add(new DebugLine(verts[tris[i]], verts[tris[i + 1]]));
            //    mesh.Add(new DebugLine(verts[tris[i]], verts[tris[i + 2]]));
            //    mesh.Add(new DebugLine(verts[tris[i + 1]], verts[tris[i + 2]]));
            //}

            //AddHandle(chunk, properties, PFDOptionEnum.TreeWireMesh, mesh);
      
            var debugedBounds = BuildParallelepiped(bounds.center - bounds.size, bounds.center + bounds.size, Color.green, 0.001f);

            ChunkDebugInfo info = GetInfo(x, z, properties);
            lock (lockObj) {
                info.colliderBounds.AddRange(debugedBounds);
            }
        }
        public static void AddWalkablePolygon(int x, int z, AgentProperties properties, Vector3 a, Vector3 b, Vector3 c) {
            List<LineData> walkablePolygonLineNewData = new List<LineData>();
            List<TriangleData> walkablePolygonSheetNewData = new List<TriangleData>();

            Color solidColor = Color.cyan;
            Color lightColor = new Color(solidColor.r, solidColor.g, solidColor.b, 0.2f);

            walkablePolygonLineNewData.Add(new LineData(a, b, solidColor, 0.001f));
            walkablePolygonLineNewData.Add(new LineData(b, c, solidColor, 0.001f));
            walkablePolygonLineNewData.Add(new LineData(c, a, solidColor, 0.001f));
            walkablePolygonSheetNewData.Add(new TriangleData(a, b, c, lightColor));

            ChunkDebugInfo info = GetInfo(x, z, properties);
            lock (lockObj) {
                info.walkablePolygonLine.AddRange(walkablePolygonLineNewData);
                info.walkablePolygonSheet.AddRange(walkablePolygonSheetNewData);
            }            
        }

        //triangulator important 
        public static void AddTriangulatorDebugLine(int x, int z, AgentProperties properties, Vector3 v1, Vector3 v2, Color color, float width = 0.001f) {
            ChunkDebugInfo info = GetInfo(x, z, properties);
            lock (lockObj) {
                info.triangulator.Add(new LineData(v1, v2, color, width));
            }
        }
        #endregion

        #region other
        private static List<LineData> BuildParallelepiped(Vector3 A, Vector3 B, Color color, float width) {
            List<LineData> result = new List<LineData>();
            result.Add(new LineData(new Vector3(A.x, A.y, A.z), new Vector3(A.x, A.y, B.z), color, width));
            result.Add(new LineData(new Vector3(A.x, A.y, B.z), new Vector3(B.x, A.y, B.z), color, width));
            result.Add(new LineData(new Vector3(B.x, A.y, B.z), new Vector3(B.x, A.y, A.z), color, width));
            result.Add(new LineData(new Vector3(B.x, A.y, A.z), new Vector3(A.x, A.y, A.z), color, width));

            result.Add(new LineData(new Vector3(A.x, A.y, A.z), new Vector3(A.x, B.y, A.z), color, width));
            result.Add(new LineData(new Vector3(A.x, A.y, B.z), new Vector3(A.x, B.y, B.z), color, width));
            result.Add(new LineData(new Vector3(B.x, A.y, B.z), new Vector3(B.x, B.y, B.z), color, width));
            result.Add(new LineData(new Vector3(B.x, A.y, A.z), new Vector3(B.x, B.y, A.z), color, width));

            result.Add(new LineData(new Vector3(A.x, B.y, A.z), new Vector3(A.x, B.y, B.z), color, width));
            result.Add(new LineData(new Vector3(A.x, B.y, B.z), new Vector3(B.x, B.y, B.z), color, width));
            result.Add(new LineData(new Vector3(B.x, B.y, B.z), new Vector3(B.x, B.y, A.z), color, width));
            result.Add(new LineData(new Vector3(B.x, B.y, A.z), new Vector3(A.x, B.y, A.z), color, width));
            return result;
        }
        //private static List<HandleThing> BuildWireMesh(Vector3[] verts, int[] tris) {
        //    List<HandleThing> result = new List<HandleThing>();
        //    for (int i = 0; i < tris.Length; i += 3) {
        //        result.Add(new DebugLine(verts[tris[i]], verts[tris[i + 1]]));
        //        result.Add(new DebugLine(verts[tris[i]], verts[tris[i + 2]]));
        //        result.Add(new DebugLine(verts[tris[i + 1]], verts[tris[i + 2]]));
        //    }
        //    return result;
        //}
        //private static List<HandleThing> BuildWireMesh(Vector3[] verts, int[] tris, Color color) {
        //    List<HandleThing> result = new List<HandleThing>();
        //    for (int i = 0; i < tris.Length; i += 3) {
        //        result.Add(new DebugLineAAColored(verts[tris[i]], verts[tris[i + 1]], color));
        //        result.Add(new DebugLineAAColored(verts[tris[i]], verts[tris[i + 2]], color));
        //        result.Add(new DebugLineAAColored(verts[tris[i + 1]], verts[tris[i + 2]], color));
        //    }
        //    return result;
        //}

        public static Color GetSomeColor(int index) {
            switch (index) {
                case 0:
                return Color.blue;
                case 1:
                return Color.red;
                case 2:
                return Color.green;
                case 3:
                return Color.magenta;
                case 4:
                return Color.yellow;
                case 5:
                return Color.cyan;
                default:
                return Color.white;

            }
        }
        private static Vector3 V3small(float val) {
            return new Vector3(0, val, 0);
        }
        private static Vector3 AngleToDirection(float angle, float length) {
            return new Vector3(Mathf.Sin(Mathf.Deg2Rad * angle) * length, 0, Mathf.Cos(Mathf.Deg2Rad * angle) * length);
        }
        #endregion


    }
}

namespace K_PathFinder.PFDebuger.Helpers {
    public class ChunkDebugInfo {
        public bool showMe = true;

        #region long list of list with important debuged stuff
        //Cells
        public int cellCounter;
        public List<TriangleData> cellsArea = new List<TriangleData>();
        public List<LineData> cellEdges = new List<LineData>();
        public List<LineData> cellConnections = new List<LineData>();

        //covers
        public int coversCounter;
        public List<PointData> coverDots = new List<PointData>();
        public List<LineData> coverLines = new List<LineData>();
        public List<TriangleData> coverSheets = new List<TriangleData>();

        //grid
        public List<LineData> grid = new List<LineData>();

        //jump bases
        public int jumpBasesCounter;
        public List<LineData> jumpBasesLines = new List<LineData>();
        public List<PointData> jumpBasesDots = new List<PointData>();

        //voxels
        public int voxelsCounter;
        public List<PointData> voxelMax = new List<PointData>();
        public List<PointData> voxelMin = new List<PointData>();
        public List<LineData> voxelVolume = new List<LineData>();
        public List<LineData> voxelConnections = new List<LineData>();
        public List<PointData> voxelHash = new List<PointData>();

        //nodes
        public List<PointData> nodesPoints = new List<PointData>();
        public List<LineData> nodesLines = new List<LineData>();
        public List<PointData> nodesPointsPreRDP = new List<PointData>();
        public List<LineData> nodesLinesPreRDP = new List<LineData>();

        //bounds
        public List<LineData> colliderBounds = new List<LineData>();
        public List<LineData> chunkBounds = new List<LineData>();

        //walkable polygons
        public List<LineData> walkablePolygonLine = new List<LineData>();
        public List<TriangleData> walkablePolygonSheet = new List<TriangleData>();

        //triangulator
        public List<LineData> triangulator = new List<LineData>();
        #endregion

        public void Clear() {
            cellsArea.Clear();
            cellEdges.Clear();
            cellConnections.Clear();
            coverDots.Clear();
            coverLines.Clear();
            coverSheets.Clear();
            grid.Clear();
            jumpBasesLines.Clear();
            jumpBasesDots.Clear();
            voxelMax.Clear();
            voxelMin.Clear();
            voxelVolume.Clear();
            voxelConnections.Clear();
            voxelHash.Clear();
            nodesPoints.Clear();
            nodesLines.Clear();
            nodesPointsPreRDP.Clear();
            nodesLinesPreRDP.Clear();
            colliderBounds.Clear();
            chunkBounds.Clear();
            walkablePolygonLine.Clear();
            walkablePolygonSheet.Clear();
            triangulator.Clear();
        }
    }

    #region HandlThings
    public abstract class HandleThing {
        public abstract void ShowHandle();
    }
    public class DebugLabel : HandleThing {
        Vector3 pos;
        string text;
        public DebugLabel(Vector3 pos, string text) {
            this.pos = pos;
            this.text = text;
        }

        public override void ShowHandle() {
            Handles.BeginGUI();
            Color color = Handles.color;
            Handles.color = Color.black;
            Handles.Label(pos, text);
            Handles.color = color;
            Handles.EndGUI();
        }
    }



    //currently no use outside labels



    //public class DebugLine : HandleThing {
    //    protected Vector3 v1, v2;
    //    public DebugLine(Vector3 v1, Vector3 v2) {
    //        this.v1 = v1;
    //        this.v2 = v2;
    //    }
    //    public override void ShowHandle() {
    //        Handles.DrawLine(v1, v2);
    //    }
    //}
    //public class DebugLineColored : DebugLine {
    //    Color color;
    //    public DebugLineColored(Vector3 from, Vector3 to, Color color) : base(from, to) {
    //        this.color = color;
    //    }
    //    public override void ShowHandle() {
    //        Handles.color = new Color(color.r, color.g, color.b, Handles.color.a);
    //        base.ShowHandle();
    //    }
    //}

    //public class DebugLineAA: DebugLine {
    //    public DebugLineAA(Vector3 from, Vector3 to) : base(from, to) {}
    //    public override void ShowHandle() {
    //        Handles.DrawAAPolyLine(v1, v2);
    //    }
    //}
    //public class DebugLineAAColored : DebugLine {
    //    protected Color color;
    //    public DebugLineAAColored(Vector3 from, Vector3 to, Color color) : base(from, to) {
    //        this.color = color;
    //    }
    //    public override void ShowHandle() {
    //        Handles.color = new Color(color.r, color.g, color.b, Handles.color.a);
    //        Handles.DrawAAPolyLine(v1, v2);
    //    }
    //}

    //public class DebugLineAASolid : DebugLine {
    //    public DebugLineAASolid(Vector3 from, Vector3 to) : base(from, to) { }

    //    public override void ShowHandle() {
    //        Color c = Handles.color;
    //        Handles.color = new Color(c.r, c.g, c.b, 1f);            
    //        Handles.DrawAAPolyLine(v1, v2);
    //        Handles.color = c;
    //    }
    //}
    //public class DebugLineAAColoredSolid : DebugLineAAColored {
    //    public DebugLineAAColoredSolid(Vector3 from, Vector3 to, Color color) : base(from, to, color) { }

    //    public override void ShowHandle() {
    //        Color c = Handles.color;
    //        Handles.color = base.color;
    //        Handles.DrawAAPolyLine(v1, v2);
    //        Handles.color = c;
    //    }
    //}
    //public class DebugBounds : HandleThing {
    //    Bounds bounds;
    //    bool haveColor;
    //    Color color; 

    //    public DebugBounds(Bounds bounds) {
    //        this.bounds = bounds;
    //        haveColor = false;
    //    }
    //    public DebugBounds(Bounds bounds, Color color) {
    //        this.bounds = bounds;
    //        haveColor = false;
    //        this.color = color;
    //    }

    //    public override void ShowHandle() {
    //        Color tColor = Handles.color;
    //        if (haveColor) 
    //            Handles.color = color;
            
    //        DrawParallelepiped(bounds.center - bounds.extents, bounds.center + bounds.extents);

    //        Handles.color = tColor;
    //    }

    //    private static void DrawParallelepiped(Vector3 A, Vector3 B) {    
    //        Handles.DrawAAPolyLine(new Vector3(A.x, A.y, A.z), new Vector3(A.x, A.y, B.z));
    //        Handles.DrawAAPolyLine(new Vector3(A.x, A.y, B.z), new Vector3(B.x, A.y, B.z));
    //        Handles.DrawAAPolyLine(new Vector3(B.x, A.y, B.z), new Vector3(B.x, A.y, A.z));
    //        Handles.DrawAAPolyLine(new Vector3(B.x, A.y, A.z), new Vector3(A.x, A.y, A.z));

    //        Handles.DrawAAPolyLine(new Vector3(A.x, A.y, A.z), new Vector3(A.x, B.y, A.z));
    //        Handles.DrawAAPolyLine(new Vector3(A.x, A.y, B.z), new Vector3(A.x, B.y, B.z));
    //        Handles.DrawAAPolyLine(new Vector3(B.x, A.y, B.z), new Vector3(B.x, B.y, B.z));
    //        Handles.DrawAAPolyLine(new Vector3(B.x, A.y, A.z), new Vector3(B.x, B.y, A.z));

    //        Handles.DrawAAPolyLine(new Vector3(A.x, B.y, A.z), new Vector3(A.x, B.y, B.z));
    //        Handles.DrawAAPolyLine(new Vector3(A.x, B.y, B.z), new Vector3(B.x, B.y, B.z));
    //        Handles.DrawAAPolyLine(new Vector3(B.x, B.y, B.z), new Vector3(B.x, B.y, A.z));
    //        Handles.DrawAAPolyLine(new Vector3(B.x, B.y, A.z), new Vector3(A.x, B.y, A.z));
    //    }
    //}

    //public abstract class DebugPosSize : HandleThing {
    //    protected Vector3 pos;
    //    protected float size;
    //    public DebugPosSize(Vector3 pos, float size) {
    //        this.pos = pos;
    //        this.size = size;
    //    }
    //}
    //public class DebugDotCap : DebugPosSize {
    //    public DebugDotCap(Vector3 pos, float size) : base(pos, size) { }

    //    public override void ShowHandle() {
    //        Handles.DotHandleCap(0, pos, Quaternion.identity, size, EventType.Repaint);
    //    }
    //}

    //public class DebugDotCapSolid : DebugPosSize {
    //    public DebugDotCapSolid(Vector3 pos, float size) : base(pos, size) { }

    //    public override void ShowHandle() {
    //        Color c = Handles.color;
    //        Handles.color = new Color(c.r, c.g, c.b, 1f);
    //        Handles.DotHandleCap(0, pos, Quaternion.identity, size, EventType.Repaint);
    //        Handles.color = c;
    //    }
    //}
    //public class DebugDisc : DebugPosSize {
    //    Vector3 normal;
    //    public DebugDisc(Vector3 pos, Vector3 normal, float radius) : base(pos, radius) {
    //        this.normal = normal;
    //    }
    //    public override void ShowHandle() {
    //        Handles.DrawSolidDisc(pos, normal, size);
    //    }
    //}
    //public class DebugDiscCameraFaced : DebugPosSize {
    //    public DebugDiscCameraFaced(Vector3 pos, float radius) : base(pos, radius) { }
    //    public override void ShowHandle() {
    //        Handles.DrawSolidDisc(pos, (pos - Camera.current.gameObject.transform.position), size);
    //    }
    //}
    //public class DebugSphere : DebugPosSize {
    //    public DebugSphere(Vector3 pos, float size) : base(pos, size) { }
    //    public override void ShowHandle() {
    //        Handles.SphereHandleCap(0, pos, Quaternion.identity, size, EventType.Repaint);
    //    }
    //}
    //public class DebugPolygon : HandleThing {
    //    Vector3 a, b, c;
    //    public DebugPolygon(Vector3 a, Vector3 b, Vector3 c) {
    //        this.a = a;
    //        this.b = b;
    //        this.c = c;
    //    }
    //    public override void ShowHandle() {
    //        Handles.DrawAAPolyLine(a, b, c, a);
    //    }
    //}
    //public class DebugCross3D : DebugPosSize {
    //    public DebugCross3D(Vector3 pos, float size) : base(pos, size) { }
    //    public override void ShowHandle() {
    //        Handles.DrawLine(new Vector3(pos.x - size, pos.y, pos.z), new Vector3(pos.x + size, pos.y, pos.z));
    //        Handles.DrawLine(new Vector3(pos.x, pos.y - size, pos.z), new Vector3(pos.x, pos.y + size, pos.z));
    //        Handles.DrawLine(new Vector3(pos.x, pos.y, pos.z - size), new Vector3(pos.x, pos.y, pos.z + size));
    //    }
    //}
    //public class DebugDotColored : DebugDotCap {
    //    Color color;
    //    public DebugDotColored(Vector3 pos, float size, Color color) : base(pos, size) {
    //        this.color = color;
    //    }

    //    public override void ShowHandle() {
    //        Handles.color = new Color(color.r, color.g, color.b, Handles.color.a);
    //        base.ShowHandle();
    //    }
    //}

    //public class DebugMeshFancy : HandleThing {
    //    Color color;
    //    Vector3[] points;
    //    public DebugMeshFancy(Vector3[] points, Color color) {
    //        this.color = color;
    //        this.points = points;
    //    }

    //    public override void ShowHandle() {
    //        Handles.color = new Color(color.r, color.g, color.b, Handles.color.a);
    //        Handles.DrawAAConvexPolygon(points);
    //    }
    //}
    //public class DebugMesh : HandleThing {
    //    Vector3[] points;
    //    public DebugMesh(params Vector3[] points) {
    //        this.points = points;
    //    }

    //    public override void ShowHandle() {
    //        Handles.DrawAAConvexPolygon(points);
    //    }
    //}

    //public class DebugWireArc : HandleThing {
    //    Vector3 center, normal, from;
    //    float radius, angle;

    //    bool colored;
    //    Color color;

    //    public DebugWireArc(Vector3 center, Vector3 normal, Vector3 from, float angle, float radius) {
    //        this.center = center;
    //        this.normal = normal;
    //        this.from = from;
    //        this.angle = angle;
    //        this.normal = normal;
    //        this.radius = radius;
    //    }

    //    public DebugWireArc(Vector3 center, Vector3 normal, Vector3 from, float angle, float radius, Color color) :this(center, normal, from, angle, radius) {
    //        this.colored = true;
    //        this.color = color;
    //    }

    //    public override void ShowHandle() {
    //        if (colored) {
    //            Color handlesColor = Handles.color;
    //            Handles.color = new Color(color.r, color.g, color.b, handlesColor.a);

    //            Handles.DrawWireArc(center, normal, from, angle, radius);
    //            Handles.color = handlesColor;
    //        }
    //        else {
    //            Handles.DrawWireArc(center, normal, from, angle, radius);
    //        }    
    //    }
    //}
    
    //public class DebugWireDisc : HandleThing {
    //    Vector3 position;
    //    Vector3 normal;
    //    float radius;

    //    bool colored;
    //    Color color;

    //    public DebugWireDisc(Vector3 position, Vector3 normal, float radius) {
    //        this.position = position;
    //        this.normal = normal;
    //        this.radius = radius;
    //    }

    //    public DebugWireDisc(Vector3 position, Vector3 normal, float radius, Color color) :this(position, normal, radius) {
    //        this.colored = true;
    //        this.color = color;
    //    }


    //    public override void ShowHandle() {
    //        if (colored) {
    //            Color handlesColor = Handles.color;
    //            Handles.color = new Color(color.r, color.g, color.b, handlesColor.a);

    //            Handles.DrawWireDisc(position, normal, radius);
    //            Handles.color = handlesColor;
    //        }
    //        else {
    //            Handles.DrawWireDisc(position, normal, radius);
    //        }
    //    }
    //}
    //public class DebugPolyLine : HandleThing {
    //    Vector3[] positions;
    //    bool colored, solid;
    //    Color color;

    //    public DebugPolyLine(bool solid = false, params Vector3[] positions) {
    //        this.solid = solid;
    //        this.positions = positions;
    //    }
    //    public DebugPolyLine(Color color, bool solid = false, params Vector3[] positions) {
    //        this.positions = positions;
    //        this.solid = solid;
    //        this.colored = true;
    //        this.color = color;
    //    }

    //    public override void ShowHandle() {
    //        if (colored) {
    //            Color handlesColor = Handles.color;
    //            if (solid)
    //                Handles.color = color;
    //            else
    //                Handles.color = new Color(color.r, color.g, color.b, handlesColor.a);

    //            Handles.DrawPolyLine(positions);
    //            Handles.color = handlesColor;
    //        }
    //        else {
    //            Handles.DrawPolyLine(positions);
    //        }
    //    }
    //}

    //public class DebugLineAAAwesome: HandleThing {
    //    Vector3 a,b;
    //    bool colored, solid;
    //    Color color;

    //    public DebugLineAAAwesome(Vector3 a, Vector3 b) {
    //        this.a = a;
    //        this.b = b;
    //    }
    //    public DebugLineAAAwesome(Vector3 a, Vector3 b, Color color, bool solid = false) {
    //        this.a = a;
    //        this.b = b;
    //        this.solid = solid;
    //        this.colored = true;
    //        this.color = color;
    //    }
        
    //    public override void ShowHandle() {
    //        if (colored) {
    //            Color handlesColor = Handles.color;
    //            if (solid)
    //                Handles.color = color;
    //            else
    //                Handles.color = new Color(color.r, color.g, color.b, handlesColor.a);

    //            Handles.DrawAAPolyLine(a,b);
    //            Handles.color = handlesColor;
    //        }
    //        else {
    //            Handles.DrawAAPolyLine(a, b);
    //        }
    //    }
    //}
    //public class DebugMesh : HandleThing {
    //    //Color color;
    //    Mesh mesh;
    //    Matrix4x4 matrix;
    //    public DebugMesh(Vector3[] verts, int[] tris, Color color) {
    //        //this.color = color;
    //        this.mesh = new Mesh();
    //        mesh.vertices = verts;
    //        mesh.triangles = tris;
    //    }

    //    public DebugMesh(Mesh mesh, Color color) {
    //        //this.color = color;
    //        this.mesh = mesh;
    //        matrix = Matrix4x4.identity;
    //    }

    //    public override void ShowHandle() {
    //        Graphics.DrawMeshNow(mesh, matrix, 2);
    //    }
    //}
    #endregion
}
#endif
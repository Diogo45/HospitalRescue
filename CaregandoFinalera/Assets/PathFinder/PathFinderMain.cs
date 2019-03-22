using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections;

using K_PathFinder.VectorInt;
using K_PathFinder.Settings;
using K_PathFinder.Graphs;
using K_PathFinder.PathGeneration;
using System.Text;
using K_PathFinder.Serialization;
using K_PathFinder.EdgesNameSpace;
using System.Linq;

#if UNITY_EDITOR
using UnityEngine.SceneManagement;
using K_PathFinder.PFDebuger;
using UnityEditor;
#endif

namespace K_PathFinder {
    public static class PathFinder {
        public const string VERSION = "0.39";
        public const int CELL_GRID_SIZE = 10; //hardcoded value. this value tell density of cell library in graph 10x10 is good enough

        //main values
        private static bool _acceptingWork = true;
        private static bool _areInit = false;
        private static int _mainThreadID;
        private static PathFinderSettings _settings;
        private static PathFinderScene _sceneInstance; //for coroutines          
        private static Dictionary<GeneralXZData, Graph> _chunkData = new Dictionary<GeneralXZData, Graph>(); //actual navmesh
        private static Dictionary<XZPosInt, YRangeInt> _chunkRange = new Dictionary<XZPosInt, YRangeInt>();  //chunk height difference
        private static int _activeThreads = 0, _activeCreationWorks = 0; //values to count current work
        private static AreaPassabilityHashData _hashData = new AreaPassabilityHashData(); //little thing to avoid accessing area library all time. we just send copy of it in every thread so it a lot less locked

        //queues. that where all type of tasks are sitting
        private static Queue<NavMeshTemplateRecast> _navMeshTemplateQueue = new Queue<NavMeshTemplateRecast>();
        private static Queue<GraphFinishTemplate> _connectionQueue = new Queue<GraphFinishTemplate>();
        private static Queue<TemplateChunkDisconnection> _disconnectionQueue = new Queue<TemplateChunkDisconnection>();
        private static Dictionary<GeneralXZData, WorkTemplate> _allTemplatesCreative = new Dictionary<GeneralXZData, WorkTemplate>();
        private static List<WorkTemplate> _allTemplatesDestructive = new List<WorkTemplate>();

        //info extraction (path generation and stuff)
        //all tasks
        private static Dictionary<PathFinderAgent, DataPair>[] _infoExtraction = new Dictionary<PathFinderAgent, DataPair>[] {
            new Dictionary<PathFinderAgent, DataPair>(), //move
            new Dictionary<PathFinderAgent, DataPair>(), //cover
            new Dictionary<PathFinderAgent, DataPair>() // battleGrid
        };

        //indexes
        enum InfoTaskType : int {
            move = 0,
            cover = 1,
            battleGrid = 2
        }
        enum NavMeshTaskType {
            navMesh = 0,
            graphFinish = 1,
            createChunk = 2,
            disconnect = 3
        }

        //struct to hold task template and callBack
        struct DataPair {
            public readonly InfoTemplateAbstract template;
            public readonly WaitCallback callBack;

            public DataPair(InfoTemplateAbstract template, WaitCallback callBack) {
                this.template = template;
                this.callBack = callBack;
            }
        }

        //values to simplify raycasting
        private static HashSet<CellContentData> raycastExclude = new HashSet<CellContentData>();
        private static List<RaycastSomeData> raycastTempData = new List<RaycastSomeData>();
        private struct RaycastSomeData {
            public readonly Vector3 point;
            public readonly Cell cell;

            public RaycastSomeData(Vector3 point, Cell cell) {
                this.point = point;
                this.cell = cell;
            }
        }

        #region editor
#if UNITY_EDITOR
        public static int DrawAreaSellector(int current) {
            return settings.DrawAreaSellector(current);
        }

        public static int activeCreationWork {
            get { return _activeCreationWorks; }
        }
#endif
        #endregion

        #region PathFinder management
        public static void SetMaxThreads(int value) {
            settings.maxThreads = Math.Max(value, 1);
        }
        public static void SetCurrentTerrainMethod(TerrainCollectorType type) {
            settings.terrainCollectionType = type;
        }
        //return if scene object was loaded
        public static bool Init() {
            if (!_areInit) {
                _areInit = true;
                //asume init was in main thread
                _mainThreadID = Thread.CurrentThread.ManagedThreadId;
            }

            if (Thread.CurrentThread.ManagedThreadId != _mainThreadID)
                return false;


            if (_settings == null) {
                _settings = PathFinderSettings.LoadSettings();
                foreach (var item in _settings.areaLibrary) {
                    _hashData.AddAreaHash(item);
                }
#if UNITY_EDITOR
                if (Debuger_K.doDebug)
                    Debug.LogFormat("settings init");
#endif
            }

            if (_sceneInstance == null || _sceneInstance.gameObject == null) {
                try {
                    GameObject go = GameObject.Find(_settings.helperName);
                    
                    if (go == null)
                        go = new GameObject(_settings.helperName);
                    
                    _sceneInstance = go.GetComponent<PathFinderScene>();
                    
                    if (_sceneInstance == null)
                        _sceneInstance = go.AddComponent<PathFinderScene>();

                    _sceneInstance.AddCoroutine((int)NavMeshTaskType.navMesh, TemplatePopulationLoop());
                    _sceneInstance.AddCoroutine((int)NavMeshTaskType.graphFinish, ChunkConnection());
                    _sceneInstance.AddCoroutine((int)NavMeshTaskType.disconnect, ChunkDisconnection());
                    
                    _sceneInstance.InitComputeShaderRasterization3D(_settings.ComputeShaderRasterization3D);
                    _sceneInstance.InitComputeShaderRasterization2D(_settings.ComputeShaderRasterization2D);

                    _sceneInstance.Init(); 
                }
                catch (Exception e) {
                    Debug.LogError(e);
                    throw;
                }

                return true;
            }
            else
                return false;
        }

        public static void CallThisWhenSceneObjectWasGone() {
            if (_sceneInstance == null)//if it's already null then do nothing
                return;

            Debug.Log("PathFinder: scene object was destroyed. clearing data and debug");
            _sceneInstance = null;

            ClearAll();
#if UNITY_EDITOR
            Debuger_K.ClearGeneric();
            Debuger_K.ClearChunksDebug();
#endif
        }

        public static PathFinderScene scene {
            get { return _sceneInstance; }
        }
        public static GameObject sceneGameObject {
            get { return _sceneInstance.gameObject; }
        }

        #region helper coroutines
        private static IEnumerator TemplatePopulationLoop() {
            NavMeshTemplateRecast nm_t = null;
            while (true) {
                //if (_disconnectionQueue.Count > 0)
                //    Debug.Log("NavMeshTemplateRecast blocked by _disconnectionQueue.Count > 0");

                if (_acceptingWork == false | _activeThreads >= _settings.maxThreads | _disconnectionQueue.Count > 0)
                    goto next;

                lock (_navMeshTemplateQueue)
                    nm_t = _navMeshTemplateQueue.Count > 0 ? _navMeshTemplateQueue.Dequeue() : null;

                if(nm_t == null)
                    goto next;

                _activeCreationWorks++;
                nm_t.Populate();

                if (multithread) {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadWorker), nm_t);
                    _activeThreads++; //increasing thread count but it's not really a thread. it's whole operation until connection
                }
                else
                    nm_t.Work();

                //Debug.LogFormat("taken {0}, now active creation is {1}", "NavMeshTemplateRecast", _activeCreationWorks);
                goto next;

                next:
                {
                    yield return new WaitForEndOfFrame();
                    continue;
                }
            }
        }
        private static IEnumerator ChunkConnection() {
            GraphFinishTemplate f_t = null;
            while (true) {
                //if (_disconnectionQueue.Count > 0)
                //    Debug.Log("GraphFinishTemplate blocked by _disconnectionQueue.Count > 0");

                if (_acceptingWork == false | _disconnectionQueue.Count > 0)
                    goto next;

                lock (_connectionQueue)
                    f_t = _connectionQueue.Count > 0 ? _connectionQueue.Dequeue() : null;

                if (f_t == null)
                    goto next;
                
                f_t.Work();

                //Debug.LogFormat("taken {0}, now active creation is {1}", "GraphFinishTemplate", _activeCreationWorks);
                goto next;

                next:
                {
                    yield return new WaitForEndOfFrame();
                    continue;
                }
            }
        }
        private static IEnumerator ChunkDisconnection() {
            TemplateChunkDisconnection d_t = null;
            while (true) {
                //if (_activeCreationWorks > 0)
                //    Debug.LogFormat("TemplateChunkDisconnection is blocked by _activeCreationWorks > 0, tasks: {0}", _disconnectionQueue.Count);

                if (_activeCreationWorks > 0)
                    goto next;

                d_t = _disconnectionQueue.Count > 0 ? _disconnectionQueue.Dequeue() : null;

                if (d_t == null)
                    goto next;
                
                d_t.Work();
                goto next;

                next:
                {
                    yield return new WaitForEndOfFrame();
                    continue;
                }
            }
        }
        #endregion
        
        public static void StopAcceptingWork() {
            _acceptingWork = false;
        }
        public static void StartAcceptingNewWork() {
            _acceptingWork = true;
        }        
        public static void ClearAll() {
            lock (_allTemplatesCreative) {
                foreach (var item in _allTemplatesCreative.Values) {
                    item.Stop();
                }
                _allTemplatesCreative.Clear();
            }

            lock (_navMeshTemplateQueue)
                _navMeshTemplateQueue.Clear();

            lock (_connectionQueue)
                _connectionQueue.Clear();

            //lock (_chunkCreationQueue)
            //    _chunkCreationQueue.Clear();

            lock (_chunkData)
                _chunkData.Clear();

            lock (_disconnectionQueue)
                _disconnectionQueue.Clear();

            _allTemplatesCreative.Clear();
            _allTemplatesDestructive.Clear();
            if(_sceneInstance != null)
                _sceneInstance.StopAll();

            _activeThreads = 0;
            _activeCreationWorks = 0;
    }
        public static void Shutdown() {
            StopAcceptingWork();
            ClearAll();
            _sceneInstance.Shutdown();
            _areInit = false;
        }
        #endregion
        
        private static bool AreTemplateInProcess(XZPosInt position, AgentProperties properties) {
            lock (_allTemplatesCreative) {
                return _allTemplatesCreative.ContainsKey(new GeneralXZData(position, properties)); 
            }
        }

        public static Area GetArea(int id) {
            return id <= settings.areaLibrary.Count ? settings.areaLibrary[id] : null;
        }
        public static Area getDefaultArea {
             get { return settings.areaLibrary[0]; }
        }

        #region path generation and info extraction
        public static void GetPath(PathFinderAgent agent, Vector3 target, Vector3 start, bool snapToNavMesh, Action callBack, bool applyRaycastBeforeFunnel = false, int maxRaycastIterations = 100, bool ignoreCrouchCost = false) {
            Init();

            if (_acceptingWork == false || ContainsKey(InfoTaskType.move, agent))
                return;

            QueueItem(InfoTaskType.move, agent, callBack, new PathTemplateMove(agent, target, start, snapToNavMesh, applyRaycastBeforeFunnel, maxRaycastIterations, ignoreCrouchCost));
        }
        public static void GetCover(PathFinderAgent agent, int minChunkDepth, float maxMoveCost, Action callBack, bool ignoreCrouchCost = false) {
            Init();

            if (_acceptingWork == false || ContainsKey(InfoTaskType.cover, agent))
                return;

            QueueItem(InfoTaskType.cover, agent, callBack, new InfoTemplateCover(agent, minChunkDepth, maxMoveCost, ignoreCrouchCost));
        }
        public static void GetBattleGrid(PathFinderAgent agent, int depth, Action callBack, params Vector3[] vectors) {
            Init();

            if (_acceptingWork == false || ContainsKey(InfoTaskType.battleGrid, agent))
                return;

            QueueItem(InfoTaskType.battleGrid, agent, callBack, new InfoTemplateBattleGrid(agent, depth, vectors));
        }

        private static bool ContainsKey(InfoTaskType t, PathFinderAgent agent) {
            return _infoExtraction[(int)t].ContainsKey(agent);
        }
        private static void QueueItem(InfoTaskType t, PathFinderAgent agent, Action callBackDelegate, InfoTemplateAbstract template) {
            template.AddCallBack(callBackDelegate);
            template.AddCallBack(() => {
                lock (_infoExtraction[(int)t])
                    _infoExtraction[(int)t].Remove(agent);
            });

            WaitCallback templateCallBack = new WaitCallback(InfoExtractionThreadWorker);
            DataPair pair = new DataPair(template, templateCallBack);

            lock (_infoExtraction[(int)t])
                _infoExtraction[(int)t].Add(agent, pair);

            ThreadPool.QueueUserWorkItem(templateCallBack, template);
        }
        private static void InfoExtractionThreadWorker(object obj) {
            InfoTemplateAbstract template = (InfoTemplateAbstract)obj;
            try {
                template.Work();
            }
            catch (Exception e) {
                Debug.LogError(e);
                throw;
            }
        }
        
        #region raycasting
        //generic version
        public static void Raycast(Vector3 origin, Vector3 direction, AgentProperties properties, out RaycastHitNavMesh hit,
                 float length = 1000f, bool triggeredByPassabilityChange = true, bool triggeredByAreaChange = true, int maxIterations = 100) {

            //try get cell at target position
            Cell cell;
            bool outsideCell;
            if(TryGetCell(origin, properties, out cell, out outsideCell) == false || outsideCell) {
                hit = new RaycastHitNavMesh(origin, false);
                return;
            }

            Raycast(origin, direction, properties, out hit, length, triggeredByPassabilityChange, triggeredByAreaChange, maxIterations, cell.passability, cell.area, cell);
        }

        //have target area
        public static void Raycast(Vector3 origin, Vector3 direction, AgentProperties properties, out RaycastHitNavMesh hit,
                Area expectedArea,
                float length = 1000f, bool triggeredByPassabilityChange = true, int maxIterations = 100) {

            //try get cell at target position
            Cell cell;
            bool outsideCell;
            if (TryGetCell(origin, properties, out cell, out outsideCell) == false || outsideCell) {
                hit = new RaycastHitNavMesh(origin, false);
                return;
            }

            //cell we found are not with expected area
            if(cell.area != expectedArea) {
                hit = new RaycastHitNavMesh(origin, true);
                return;
            }

            Raycast(origin, direction, properties, out hit, length, triggeredByPassabilityChange, true, maxIterations, cell.passability, expectedArea, cell);
        }

        //have target passability
        public static void Raycast(Vector3 origin, Vector3 direction, AgentProperties properties, out RaycastHitNavMesh hit,
                Passability expectedPassability,
                float length = 1000f, bool triggeredByAreaChange = true, int maxIterations = 100) {

            //try get cell at target position
            Cell cell;
            bool outsideCell;
            if (TryGetCell(origin, properties, out cell, out outsideCell) == false || outsideCell) {
                hit = new RaycastHitNavMesh(origin, false);
                return;
            }

            //cell we found are not with expected area
            if (cell.passability != expectedPassability) {
                hit = new RaycastHitNavMesh(origin, true);
                return;
            }

            Raycast(origin, direction, properties, out hit, length, true, triggeredByAreaChange, maxIterations, expectedPassability, cell.area, cell);
        }

        //targeted by area and passability
        public static void Raycast(Vector3 origin, Vector3 direction, AgentProperties properties, out RaycastHitNavMesh hit,
                Area expectedArea, Passability expectedPassability,
                float length = 1000f, int maxIterations = 100) {

            //try get cell at target position
            Cell cell;
            bool outsideCell;
            if (TryGetCell(origin, properties, out cell, out outsideCell) == false || outsideCell) {
                hit = new RaycastHitNavMesh(origin, false);
                return;
            }

            //cell we found are not with expected area or passability
            if (cell.area != expectedArea | cell.passability != expectedPassability ) {
                hit = new RaycastHitNavMesh(origin, true);
                return;
            }

            Raycast(origin, direction, properties, out hit, length, true, true, maxIterations, expectedPassability, expectedArea, cell);
        }

        //private raycasting to take input by things upside
        private static void Raycast(Vector3 origin, Vector3 direction,
            AgentProperties properties, out RaycastHitNavMesh hit,
            float length, bool triggeredByPassabilityChange, bool triggeredByAreaChange,
            int maxIterations, Passability expectedPassability, Area expectedArea, Cell cell) {
         
            raycastExclude.Clear();//excluded list of edges
            float maxLengthSqr = length * length;

            for (int iteration = 0; iteration < maxIterations; iteration++) {
                raycastTempData.Clear();//iteration data cleared

                foreach (var pair in cell.dataContentPairs) {
                    CellContentData curData = pair.Key;
                    if (!raycastExclude.Add(curData))//mean it's already contain this
                        continue;

                    Vector3 intersect;
                    if (SomeMath.RayIntersectXZ(origin, direction, curData.leftV3, curData.rightV3, out intersect)) {
                        if (pair.Value != null) {
                            Cell otherCell = pair.Value.connection;
                            if (otherCell == cell | !otherCell.canBeUsed)
                                continue;

                            if ((triggeredByPassabilityChange && cell.passability != otherCell.passability) ||
                                (triggeredByAreaChange && cell.area != otherCell.area)) {
                                hit = new RaycastHitNavMesh(intersect, SomeMath.SqrDistance(origin, intersect) < maxLengthSqr);
                                return;
                            }
                            raycastTempData.Add(new RaycastSomeData(intersect, otherCell));
                        }
                        else
                            raycastTempData.Add(new RaycastSomeData(intersect, null));
                    }
                }

                //check if there possible connection
                for (int i = 0; i < raycastTempData.Count; i++) {
                    if (raycastTempData[i].cell != null) {
                        cell = raycastTempData[i].cell;
                        goto CONTINUE;
                    }
                }

                //now we definetly hit something and now find furthest
                float furthestSqrDist = 0f;
                Vector3 furthest = origin;
                for (int i = 0; i < raycastTempData.Count; i++) {
                    float curSqrDist = SomeMath.SqrDistance(raycastTempData[i].point, origin);

                    if (curSqrDist > furthestSqrDist) {
                        furthestSqrDist = curSqrDist;
                        furthest = raycastTempData[i].point;
                    }
                }

                hit = new RaycastHitNavMesh(furthest, SomeMath.SqrDistance(origin, furthest) < maxLengthSqr);
                return;

                CONTINUE: { continue; }
            }
            hit = new RaycastHitNavMesh(origin, true, true);
            return;
        }
        #endregion
        #endregion

        #region management
        //give me Graph
        public static bool GetGraph(XZPosInt pos, AgentProperties properties, out Graph graph) {
            Init();
 
            lock (_chunkData) {
                GeneralXZData key = new GeneralXZData(pos, properties);

                if (_chunkData.TryGetValue(key, out graph))
                    return true;
                else {
                    if(AreTemplateInProcess(pos, properties) == false)
                        QueueNavMeshTemplateToPopulation(pos, properties);
                    return false;
                }
            }
        }
        public static bool GetGraph(int x, int z, AgentProperties properties, out Graph graph) {   
            return GetGraph(new XZPosInt(x, z), properties, out graph);
        }
        public static bool GetGraphFrom(XZPosInt pos, Directions direction, AgentProperties properties, out Graph graph) {
            switch (direction) {
                case Directions.xPlus:
                return GetGraph(pos.x + 1, pos.z, properties, out graph);

                case Directions.xMinus:
                return GetGraph(pos.x - 1, pos.z, properties, out graph);

                case Directions.zPlus:
                return GetGraph(pos.x, pos.z + 1, properties, out graph);

                case Directions.zMinus:
                return GetGraph(pos.x, pos.z - 1, properties, out graph);

                default:
                    Debug.LogError("defaul direction are not exist");
                    graph = null;
                    return false;
            }
        }

        //try give me Graph 
        public static bool TryGetGraph(XZPosInt pos, AgentProperties properties, out Graph graph) {
            return _chunkData.TryGetValue(new GeneralXZData(pos, properties), out graph);
        }
        public static bool TryGetGraph(int x, int z, AgentProperties properties, out Graph graph) {
            return TryGetGraph(new XZPosInt(x, z), properties, out graph);
        }
        public static bool TryGetGraphFrom(XZPosInt pos, Directions direction, AgentProperties properties, out Graph graph) {
            switch (direction) {
                case Directions.xPlus:
                    return TryGetGraph(pos.x + 1, pos.z, properties, out graph);

                case Directions.xMinus:
                    return TryGetGraph(pos.x - 1, pos.z, properties, out graph);

                case Directions.zPlus:
                    return TryGetGraph(pos.x, pos.z + 1, properties, out graph);

                case Directions.zMinus:
                    return TryGetGraph(pos.x, pos.z - 1, properties, out graph);

                default:
                    Debug.LogError("defaul direction are not exist");
                    graph = null;
                    return false;
            }
        }

        //try get Cell
        public static bool TryGetCell(Vector3 pos, AgentProperties properties, out Cell cell, out bool outsideCell, out Vector3 closestPoint) { 
            Graph graph;
            if (TryGetGraph(ToChunkPosition(pos), properties, out graph)) {            
                graph.GetClosestCell(pos, out cell, out outsideCell, out closestPoint);
                return true;
            }
            else {
                cell = null;
                outsideCell = false;
                closestPoint = Vector3.zero;
                return false;
            }
        }
        public static bool TryGetCell(Vector3 pos, AgentProperties properties, out Cell cell, out bool outsideCell) {
            Vector3 closestPoint;
            return TryGetCell(pos, properties, out cell, out outsideCell, out closestPoint);
        }
        
        //queue graph creation
        //functions to order navmesh at some space
        public static void QueueGraph(int x, int z, AgentProperties properties, int sizeX = 1, int sizeZ = 1) {
            Init();
            if (sizeX == 0 | sizeZ == 0) {
                Debug.LogWarning("you trying to create navmesh with zero size. are you mad?");
                return;
            }
            Graph graph;
            for (int _x = 0; _x < sizeX; _x++) {
                for (int _z = 0; _z < sizeZ; _z++) {
                    GetGraph(x + _x, z + _z, properties, out graph);
                }
            }
        }
        public static void QueueGraph(XZPosInt pos, AgentProperties properties) {
            QueueGraph(pos.x, pos.z, properties);
        }
        public static void QueueGraph(XZPosInt pos, VectorInt.Vector2Int size, AgentProperties properties) {
            QueueGraph(pos.x, pos.z, properties, size.x, size.y);
        }
        public static void QueueGraph(Bounds bounds, AgentProperties properties) {
            XZPosInt min = ToChunkPosition(bounds.min);
            XZPosInt max = ToChunkPosition(bounds.max);
            QueueGraph(min.x, min.z, properties, max.x - min.x + 1, max.z - min.z + 1);
        }

        //remove graph
        //function to remove graph at some space
        //IMPORTANT: if bool createNewGraphAfter == true then pathfinder are also add this graph to generation queue after it was removed
        public static void RemoveGraph(XZPosInt pos, AgentProperties properties, bool createNewGraphAfter = true) {
            Init();

            if (_acceptingWork == false)
                return;

            lock (_allTemplatesDestructive) {
                if (_allTemplatesDestructive.Exists(x => x.Match(pos, properties)))
                    return; //already in progress
            }
            Graph graph;
            if(TryGetGraph(pos, properties, out graph)) {
                TemplateChunkDisconnection template = new TemplateChunkDisconnection(graph);

                _allTemplatesDestructive.Add(template);

                template.SetCallBack(() => {
#if UNITY_EDITOR
                    Debuger_K.UpdateSceneImportantThings();
                    Debuger_K.ClearChunksDebug(pos.x, pos.z, properties);
#endif
                    lock (_chunkData) {
                        _chunkData.Remove(new GeneralXZData(pos, properties));
                    }
                    _allTemplatesDestructive.Remove(template);
                    if (createNewGraphAfter)
                        QueueNavMeshTemplateToPopulation(pos, properties);
                });

                _disconnectionQueue.Enqueue(template);
            }   
    
        }
        public static void RemoveGraph(int x, int z, AgentProperties properties, int sizeX = 1, int sizeZ = 1, bool createNewGraphAfter = true) {
            for (int _x = 0; _x < sizeX; _x++) {
                for (int _z = 0; _z < sizeZ; _z++) {
                    RemoveGraph(new XZPosInt(x + _x, z + _z), properties, createNewGraphAfter);
                }
            }
        }
        public static void RemoveGraph(Bounds bounds, AgentProperties properties, bool createNewGraphAfter = true) {
            float offset = properties.radius * properties.offsetMultiplier;
            Vector3 v3Offset = new Vector3(offset, 0, offset);
            XZPosInt min = ToChunkPosition(bounds.min - v3Offset);
            XZPosInt max = ToChunkPosition(bounds.max + v3Offset);
            VectorInt.Vector2Int size = new VectorInt.Vector2Int(Math.Max(1, max.x - min.x + 1), Math.Max(1, max.z - min.z + 1));
            RemoveGraph(min.x, min.z, properties, size.x, size.y, createNewGraphAfter);
        }
        public static void RemoveGraph(AgentProperties properties, bool createNewGraphAfter = true, params Bounds[] bounds) {
            for (int i = 0; i < bounds.Length; i++) {
                RemoveGraph(bounds[i], properties, createNewGraphAfter);
            }
        }
        #endregion

        #region enqueue and dequeue
        private static void QueueNavMeshTemplateToPopulation(XZPosInt pos, AgentProperties properties) {
            if (_acceptingWork == false)
                return;

            //Debug.Log("queue 1 " + pos);
            NavMeshTemplateRecast template = new NavMeshTemplateRecast(_chunkRange, pos, properties);

            Action<Graph> callBack = (Graph graph) => {
                //Debug.Log("callback 1 " + pos);
                QueueFinishGraphInMainThread(graph);    
            };
            
            template.SetCallBack(callBack);
            _allTemplatesCreative[new GeneralXZData(pos, properties)] = template;

            lock (_navMeshTemplateQueue)
                _navMeshTemplateQueue.Enqueue(template);
        }             

        private static void ThreadWorker(object obj) {
            NavMeshTemplateRecast template = (NavMeshTemplateRecast)obj;
            try {
                template.Work();
            }
            catch (Exception e) {
                if (template.profiler != null)
                    template.profiler.DebugLog(ProfilderLogMode.warning);
                Debug.LogError(e);     
                throw;
            }
        }                       
         
        private static void QueueFinishGraphInMainThread(Graph graph) {
            if (_acceptingWork == false)
                return;

            GraphFinishTemplate template = new GraphFinishTemplate(graph);
            
            template.SetCallBack(() => {                         
                graph.SetAsCanBeUsed();
                SetGraph(graph);
                _activeCreationWorks--;
                //decrease thread count in very end and in main thread
                _allTemplatesCreative.Remove(new GeneralXZData(graph.gridPosition, graph.properties));
                if (_settings.useMultithread)
                    _activeThreads--;
            });

            _allTemplatesCreative[new GeneralXZData(graph.gridPosition, graph.properties)] = template;
   
            lock (_connectionQueue)
                _connectionQueue.Enqueue(template);
        }

        private static void SetGraph(Graph graph) {
            //Debug.Log("set " + graph.gridPosition);
            lock (_chunkData) {
                _chunkData.Add(new GeneralXZData(graph.gridPosition, graph.properties), graph);
            }
        }
        #endregion

        #region public values acessors
        public static bool areAcceptingWork {
            get { return _acceptingWork; }
        }
        public static bool areInit {
            get { return _areInit; }
        }
        public static bool haveActiveThreds {
            get { return _activeThreads > 0; }
        }
        public static bool haveQueuedCreationTemplates {
            get {
                lock (_navMeshTemplateQueue)
                    if (_navMeshTemplateQueue.Count > 0)
                        return true;

                lock (_connectionQueue)
                    if (_connectionQueue.Count > 0)
                        return true;

                return false;
            }
        }
        public static bool haveQueuedDestructionTemplates {
            get {
                lock (_navMeshTemplateQueue)
                    return _navMeshTemplateQueue.Count > 0;
            }
        }

        public static PathFinderSettings settings {
            get {
                if (_settings == null)
                    _settings = PathFinderSettings.LoadSettings();
                return _settings;
            }
        }

        public static float gridSize {
            get { return settings.gridSize; }
        }
        public static int gridLowest {
            get { return settings.gridLowest; }
        }
        public static int gridHighest {
            get { return settings.gridHighest; }
        }

        public static bool multithread {
            get { return settings.useMultithread; }
        }

        public static TerrainCollectorType terrainCollectionType {
            get { return settings.terrainCollectionType; }
        }
        public static ColliderCollectorType colliderCollectorType {
            get { return settings.colliderCollectionType; }
        }
        #endregion 

        #region position convertation
        public static int ToGrid(float value) {
            return (int)Math.Floor(value / gridSize);
        }
        public static XZPosInt ToChunkPosition(float realX, float realZ) {
            return new XZPosInt(ToGrid(realX), ToGrid(realZ));
        }
        public static XZPosInt ToChunkPosition(Vector2 vector) {
            return ToChunkPosition(vector.x, vector.y);
        }
        public static XZPosInt ToChunkPosition(Vector3 vector) {
            return ToChunkPosition(vector.x, vector.z);
        }
        #endregion

        #region hash data
        public static void AddAreaHash(Area area) {
            lock (_hashData)
                _hashData.AddAreaHash(area);
        }

        //prefer cloning and use clone than this cause it lock
        public static int GetAreaHash(Area area, Passability passability) {
            lock (_hashData)
                return _hashData.GetAreaHash(area, passability);
        }
        public static void GetAreaByHash(int value, out Area area, out Passability passability) {
            lock (_hashData)
                _hashData.GetAreaByHash(value, out area, out passability);
        }

        public static AreaPassabilityHashData CloneHashData() {
            lock (_hashData)
                return _hashData.Clone();
        }
        #endregion

        #region serialization
#if UNITY_EDITOR
        //saving only for editor cause it saved in scriptable object
        //for existed scenes only
        //if you wanna save navmesh in save file then write how you store properties and get serialized navmesh using Serialize/Deserialize functions
        public static void SaveCurrentSceneData() {
            Init();

            SceneNavmeshData data = _sceneInstance.sceneNavmeshData;
            if (data == null) {
                string path = EditorUtility.SaveFilePanel("Save NavMesh", "Assets", SceneManager.GetActiveScene().name + ".asset", "asset");

                if (path == "")
                    return;

                path = FileUtil.GetProjectRelativePath(path);
                data = ScriptableObject.CreateInstance<SceneNavmeshData>();
                AssetDatabase.CreateAsset(data, path);
                AssetDatabase.SaveAssets();
                Undo.RecordObject(_sceneInstance, "Set SceneNavmeshData to NavMesh scene instance");
                _sceneInstance.sceneNavmeshData = data;
                EditorUtility.SetDirty(_sceneInstance);
            }

            HashSet<AgentProperties> allProperties = new HashSet<AgentProperties>();
            foreach (var key in _chunkData.Keys) {
                allProperties.Add(key.properties);
            }      

            List<AgentProperties> properties = new List<AgentProperties>();
            List<SerializedNavmesh> navmesh = new List<SerializedNavmesh>();

            foreach (var p in allProperties) {
                properties.Add(p);
                navmesh.Add(Serialize(p));
            }

            data.properties = properties;
            data.navmesh = navmesh;
            EditorUtility.SetDirty(data);
        }
#endif

        public static void LoadCurrentSceneData() {
            if(Init() == false)//cause if true it will load anyway
                _sceneInstance.LoadCurrentData();
        }

#if UNITY_EDITOR
        public static void ClearCurrentData() {
            Init();
            SceneNavmeshData data = _sceneInstance.sceneNavmeshData;

            if (data == null) {
                Debug.LogWarning("data == null");
                return;
            }

            if (data.properties != null) {
                if(data.properties.Count > 0) {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Cleared:");
                    for (int i = 0; i < data.properties.Count; i++) {
                        sb.AppendFormat("properties: {0}, graphs: {1}, cells: {2}", data.properties[i].name, data.navmesh[i].serializedGraphs.Count, data.navmesh[i].cellCount);
                    }
                    Debug.Log(sb);
                }
                else
                    Debug.Log("nothing to clear");
            }

            if (data.properties != null)
                data.properties.Clear();

            if (data.navmesh != null)
                data.navmesh.Clear();

            EditorUtility.SetDirty(data);            
        }
#endif
        public static SerializedNavmesh Serialize(AgentProperties properties) {         
            NavmeshLayserSerializer serializer = new NavmeshLayserSerializer(_chunkData, _chunkRange, properties);
            SerializedNavmesh result = serializer.Serialize();
            result.pathFinderVersion = VERSION;        
            return result;          
        }


        public static void Deserialize(SerializedNavmesh target, AgentProperties properties) {
            //remove all data if it exist
            lock (_chunkData) {
                List<XZPosInt> removeList = new List<XZPosInt>();
                foreach (var graph in _chunkData.Values) {
                    if (graph.properties == properties)
                        removeList.Add(graph.gridPosition);
                }
                foreach (var pos in removeList) {
                    _chunkData.Remove(new GeneralXZData(pos, properties));
                }

                NavmeshLayerDeserializer deserializer = new NavmeshLayerDeserializer(target, properties);
                var deserializedStuff = deserializer.Deserialize();

                //create chunk if needed and clamp size if it outside
                foreach (var deserialized in deserializedStuff) {
                    XZPosInt pos = deserialized.chunkPosition;
                    YRangeInt curRange;
                    if (_chunkRange.TryGetValue(pos, out curRange)) {
                        _chunkRange[pos] = new YRangeInt(
                            Mathf.Min(curRange.min, deserialized.chunkMinY),
                            Mathf.Max(curRange.max, deserialized.chunkMaxY));
                    }
                    else
                        _chunkRange.Add(pos, new YRangeInt(deserialized.chunkMinY, deserialized.chunkMaxY));
                }

                List<Graph> graphs = new List<Graph>();
                //put graphs inside chunks
                foreach (var deserialized in deserializedStuff) {
                    //Chunk chunk = _chunkData[deserialized.chunkPosition];

                    XZPosInt pos = deserialized.chunkPosition;
                    YRangeInt ran = _chunkRange[pos];

                    Graph graph = deserialized.graph;
                    graph.SetChunkAndProperties(new ChunkData(pos, ran), properties);
                    _chunkData[new GeneralXZData(pos, properties)] = graph;
                    graph.SetAsCanBeUsed();
                    graphs.Add(graph);
                }
                
                //connect chunks
                foreach (var graph in graphs) {
                    for (int i = 0; i < 4; i++) {
                        Graph neighbour;
                        if (TryGetGraphFrom(graph.gridPosition, (Directions)i, properties, out neighbour)){
                            graph.SetNeighbour((Directions)i, neighbour);
                        }
                    }
#if UNITY_EDITOR
                    if (Debuger_K.doDebug)
                        graph.DebugGraph();
#endif
                }
            }
        }
        #endregion

        #region things to help debug stuff
#if UNITY_EDITOR
        public static void CellTester(Vector3 origin, AgentProperties properties) {
            Graph graph;
            if (TryGetGraph(ToChunkPosition(origin), properties, out graph) == false || graph.canBeUsed == false)
                return;

            Cell cell;
            bool outsideCell;
            Vector3 closestPosToCell;

            graph.GetClosestCell(origin, out cell, out outsideCell, out closestPosToCell);

            foreach (var pair in cell.dataContentPairs) {
                Debuger_K.AddLine(pair.Key, Color.magenta);

                if (pair.Value == null)
                    continue;

                Cell connection = pair.Value.connection;
                Debuger_K.AddLabel(pair.Key.centerV3, connection.Contains(pair.Key));
            }
        }
#endif
        #endregion
    }

}
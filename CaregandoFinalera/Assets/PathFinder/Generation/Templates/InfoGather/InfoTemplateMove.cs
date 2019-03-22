using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using K_PathFinder.VectorInt ;

using K_PathFinder.Graphs;
using K_PathFinder.NodesNameSpace;


#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

namespace K_PathFinder.PathGeneration {
    public class PathTemplateMove : InfoTemplateAbstract {
        Vector3 end_v3;
        Graph endGraph;
        Cell endCell;

        List<CellPath> potentialPaths = new List<CellPath>();
        HashSet<Cell> excluded = new HashSet<Cell>();
        int maxPaths = 1;
        int maxIterations = 15;

        //general flags
        bool ignoreCrouchCost;

        //funnel values
        private Path funnelPath;
        bool snapToNavMesh;

        //raycast
        bool applyRaycast;
        int raycastIterations;

        //values to simplify raycasting
        private struct RaycastSomeData {
            public readonly Vector3 point;
            public readonly Cell cell;

            public RaycastSomeData(Vector3 point, Cell cell) {
                this.point = point;
                this.cell = cell;
            }
        }

        public PathTemplateMove(PathFinderAgent agent, Vector3 target, Vector3 start, bool snapToNavMesh, 
            bool applyRaycastBeforeFunnel, 
            int maxRaycastIterations,
            bool ignoreCrouchCost) : base(agent, start) {
            this.ignoreCrouchCost = ignoreCrouchCost;
            this.end_v3 = target;
            this.snapToNavMesh = snapToNavMesh;
            this.applyRaycast = applyRaycastBeforeFunnel;
            this.raycastIterations = maxRaycastIterations;
            //this.end_v2 = ToVector2(target);
        }

        public override void Work() {
            GetStartValues();
            CheckChunkPath();

            if (ReferenceEquals(startCell, endCell)) {
                Path path = new Path(start_v3, (MoveState)(int)startCell.passability);
                path.AddMove(end_v3, (MoveState)(int)startCell.passability);
                path.RemoveFirst();
                agent.ReceivePath(path);
                callBack.Invoke();
                return;
            }

            GeneratePaths();

            if (potentialPaths.Count == 0) {
                Debug.Log("no path");
                callBack.Invoke();
                return;
            }

            CellPath targetCellPath = potentialPaths.OrderBy(val => val.gh).First();
            funnelPath = new Path(start_v3, (MoveState)(int)targetCellPath.path.First().passability);
            if (applyRaycast) {
                RaycastHitNavMesh rhnm;
                Cell c = targetCellPath.connections[0].from;
                Raycast(start_v3, end_v3 - start_v3, out rhnm, Vector3.Distance(start_v3, end_v3), raycastIterations, c.passability, c.area, c);

                if (!rhnm.isOnGraph) {
                    //Debuger_K.AddLine(start_v3, rhnm.point, Color.green);
                    //Debuger_K.AddDot(rhnm.point, Color.green, 0.2f);
                    funnelPath.AddMove(end_v3, (MoveState)(int)c.passability);
                }
                else {
                    FunnelPath(targetCellPath, end_v3);
                    //Debuger_K.AddLine(start_v3, rhnm.point, Color.red);
                    //Debuger_K.AddDot(rhnm.point, Color.red, 0.2f);
                }
            }
            else {
                FunnelPath(targetCellPath, end_v3);
            }
            //FunnelPath(targetCellPath, end_v3);
            agent.ReceivePath(funnelPath);
            callBack.Invoke();
        }

        private void CheckChunkPath() {
            Vector3 closestPos;
            GetChunkValues(end_v3, out endGraph, out endCell, out closestPos, snapToNavMesh);

#if UNITY_EDITOR
            if (Debuger_K.debugPath) {
                Debuger_K.AddLine(end_v3, closestPos, Color.red);
                Debuger_K.AddLine(end_v3, endCell.centerV3, Color.cyan);
            }
#endif

            end_v3 = closestPos;

            if (PathFinder.ToChunkPosition(start_v3) == PathFinder.ToChunkPosition(end_v3))
                return;

            VectorInt.Vector2Int targetPosition = endGraph.positionChunk;
            ClearGraphList();

            AddGraphNode(new GraphPathSimple(startGraph, Vector2.Distance(start_v3, end_v3)));
            HashSet<Graph> usedGraphs = new HashSet<Graph>();

            for (int v = 0; v < 10; v++) {
                if (base.linkedGraph.Count == 0) {
                    UnityEngine.Debug.Log("no path. count");
                    break;
                }

                GraphPathSimple current = TakeGraphNode();
                Graph currentGraph = current.graph;

                //Debuger3.AddLine(start_v3, currentGraph.positionWorldCenter, Color.red);
                //Debuger3.AddLabel(currentGraph.positionWorldCenter, linkedGrap.Count);

                if (currentGraph.positionChunk == targetPosition) 
                    break;                

                for (int dir = 0; dir < 4; dir++) {
                    Graph neighbourGraph;
                    while (true) {
                        if (PathFinder.GetGraphFrom(currentGraph.gridPosition, (Directions)dir, properties, out neighbourGraph) && neighbourGraph.canBeUsed)
                            break;
                        Thread.Sleep(10);
                    }

                    if (neighbourGraph != null && usedGraphs.Contains(neighbourGraph) == false) {
                        AddGraphNode(new GraphPathSimple(neighbourGraph, Vector3.Distance(end_v3, neighbourGraph.positionCenter)));
                        usedGraphs.Add(neighbourGraph);
                    }
                }
            }

            if (endGraph == null) {
                Debug.LogWarning("graph path > 500");
                Debug.LogWarning("chunk path result are null");
                return;
            }
        }
        
        #region potential paths
        private void GeneratePaths() {
            CellPath path = new CellPath(startCell, start_v3);

            if (startCell == endCell) {
                potentialPaths.Add(path);
                return;
            }

#if UNITY_EDITOR
            float totalDist = Debuger_K.doDebug ? Vector3.Distance(start_v3, end_v3) : 0f;
#endif

            path.h = EuclideanDistance(start_v3);
            excluded.Clear();
            excluded.Add(startCell);

            foreach (var connection in startCell.connections) {
                CellPath newPath = new CellPath(path, connection);
                newPath.g = connection.Cost(properties, ignoreCrouchCost);
                if (connection is CellContentPointedConnection)
                    newPath.h = EuclideanDistance((connection as CellContentPointedConnection).exitPoint);
                else
                    newPath.h = EuclideanDistance(connection.connection);
                AddCellNode(newPath);
            }

            int limit = 0;
            while (true) {
                limit++;
                if (limit > 1500) {
                    Debug.Log("limit > 1500");
                    break;
                }

                CellPath current = TakeCellNode();
                if (current == null)
                    break;

                Cell currentCell = current.last;

                if (currentCell == endCell) {
                    potentialPaths.Add(current);
#if UNITY_EDITOR
                    if (Debuger_K.doDebug) {
                        float lerped = Mathf.InverseLerp(0, totalDist, Vector3.Distance(end_v3, currentCell.centerV3));
                        Debuger_K.AddPath(current.path[current.path.Count - 2].centerV3 + Vector3.up, current.path[current.path.Count - 1].centerV3 + Vector3.up, new Color(lerped, 1 - lerped, 0, 1f));
                    }
#endif
                    if (potentialPaths.Count >= maxPaths)
                        break;
                    else
                        continue;
                }

                if (excluded.Contains(currentCell))
                    continue;
                else
                    excluded.Add(currentCell);

#if UNITY_EDITOR
                if (Debuger_K.doDebug) {
                    float lerped = Mathf.InverseLerp(0, totalDist, Vector3.Distance(end_v3, currentCell.centerV3));
                    Debuger_K.AddPath(current.path[current.path.Count - 2].centerV3 + (Vector3.up * 0.3f), current.path[current.path.Count - 1].centerV3 + (Vector3.up * 0.3f), new Color(lerped, 1 - lerped, 0, 1f));
                }
#endif

                foreach (var connection in currentCell.connections) {
                    Cell newCell = connection.connection;

                    if (current.Contains(newCell) == false) {
                        CellPath newPath = new CellPath(current, connection);
#if UNITY_EDITOR
                        if (Debuger_K.debugPath)
                            Debuger_K.AddLabel(SomeMath.MidPoint(current.last.centerV3, newCell.centerV3), connection.Cost(properties, ignoreCrouchCost), DebugGroup.path);
#endif

                        newPath.g = current.g + connection.Cost(properties, ignoreCrouchCost);
                        if (connection is CellContentPointedConnection) {
                            newPath.h = EuclideanDistance((connection as CellContentPointedConnection).exitPoint);
                            //Debuger3.AddLabel((connection as CellPointedConnection).exitPoint, newPath.h);
                        }
                        else
                            newPath.h = EuclideanDistance(connection.connection);

                        AddCellNode(newPath);
                    }
                }
            }
        }
        #endregion

        #region funnel
        protected void FunnelPath(CellPath path, Vector3 endV3) {
            List<Cell> cellPath = path.path;        

            List<CellContent> cellPathConnections = path.connections;
#if UNITY_EDITOR
            if (Debuger_K.debugPath) {
                for (int i = 0; i < cellPath.Count - 1; i++)
                    Debuger_K.AddPath(cellPath[i].centerV3 + Vector3.up, cellPath[i + 1].centerV3 + Vector3.up, Color.magenta, 0.1f);
            }
#endif
            int keyGate = 0;

            while (true) {
                if (keyGate >= cellPathConnections.Count)
                    break;

                int curKeyGate;

                List<CellContentGenericConnection> gateSiquence = new List<CellContentGenericConnection>();
                for (curKeyGate = keyGate; curKeyGate < cellPathConnections.Count; curKeyGate++) {
                    var c = cellPathConnections[curKeyGate];
                    if (c is CellContentGenericConnection)
                        gateSiquence.Add((CellContentGenericConnection)c);
                    else
                        break;
                }

                if (keyGate != curKeyGate) {
                    DoFunnelIteration(funnelPath.lastV3, curKeyGate == cellPathConnections.Count ? endV3 : (cellPathConnections[curKeyGate] as CellContentPointedConnection).enterPoint, gateSiquence);
                }

                if (curKeyGate != cellPathConnections.Count) {
                    if (cellPathConnections[curKeyGate] is CellContentPointedConnection) {
                        CellContentPointedConnection ju = cellPathConnections[curKeyGate] as CellContentPointedConnection;
                        if(ju.jumpState == ConnectionJumpState.jumpUp) {
                            funnelPath.AddMove(ju.lowerStandingPoint, (MoveState)(int)ju.from.passability);
                            funnelPath.AddJumpUp(ju.lowerStandingPoint, ju.axis);
                            funnelPath.AddMove(ju.exitPoint, (MoveState)(int)ju.connection.passability);
                        }
                        else{
                            funnelPath.AddMove(ju.enterPoint, (MoveState)(int)ju.from.passability);
                            funnelPath.AddMove(ju.axis, (MoveState)(int)ju.from.passability);
                            funnelPath.AddJumpDown(ju.axis, ju.lowerStandingPoint);
                            funnelPath.AddMove(ju.exitPoint, (MoveState)(int)ju.connection.passability);
                        }
                    }          
                    else {
                        Debug.LogErrorFormat("idk type of CellConnectionAbstract node {0}", cellPathConnections[curKeyGate].GetType().Name);
                    }
                }

                keyGate = curKeyGate + 1;
            }

            funnelPath.AddMove(endV3, (MoveState)(int)cellPath[cellPath.Count - 1].passability);

#if UNITY_EDITOR
            if (Debuger_K.debugPath) {
                var resultNodes = funnelPath.nodes;
                for (int i = 0; i < resultNodes.Count - 1; i++) {
                    Debuger_K.AddPath(resultNodes[i].positionV3, resultNodes[i + 1].positionV3, Color.green);
                }
                //for (int i = 0; i < resultNodes.Count; i++) {
                //    Debuger3.AddDot(resultNodes[i].positionV3, Color.green, 0.03f, DebugGroup.path);
                //    Debuger3.AddLabel(resultNodes[i].positionV3, resultNodes[i].ToString(), DebugGroup.path);
                //}
            }
#endif
            funnelPath.RemoveFirst();
        }
        
        private void DoFunnelIteration(Vector3 startV3, Vector3 endV3, List<CellContentGenericConnection> targetConnections) {
            List<CellContentData> gd = new List<CellContentData>();
            for (int i = 0; i < targetConnections.Count; i++) {
                gd.Add(targetConnections[i].cellData);
            }

            gd.Add(new CellContentData(endV3));
            gd.Add(new CellContentData(endV3));

            int curCycleEnd = 0, curCycleStart = 0, curIterationGateCount = targetConnections.Count + 1;
            Vector2 left, right;

            for (int i = 0; i < maxIterations; i++) {
                if (curCycleEnd == curIterationGateCount)
                    break;

                Vector2 startV2 = funnelPath.lastV2;

                for (curCycleStart = curCycleEnd; curCycleStart < curIterationGateCount; curCycleStart++) {
                    if (gd[curCycleStart].leftV2 != startV2 & gd[curCycleStart].rightV2 != startV2)
                        break;
                }
                
                left = gd[curCycleStart].leftV2;
                right = gd[curCycleStart].rightV2;
                Vector2 lowestLeftDir = left - startV2;
                Vector2 lowestRightDir = right - startV2;
                float lowestAngle = Vector2.Angle(lowestLeftDir, lowestRightDir);

                #region gate iteration
                int stuckLeft = curCycleStart;
                int stuckRight = curCycleStart;
                Vector3? endNode = null;

                for (int curGate = curCycleStart; curGate < curIterationGateCount; curGate++) {
                    right = gd[curGate].rightV2;
                    left = gd[curGate].leftV2;

                    Vector2 curLeftDir = left - startV2;
                    Vector2 curRightDir = right - startV2;

                    if (SomeMath.V2Cross(lowestLeftDir, curRightDir) >= 0) {
                        float currentAngle = Vector2.Angle(lowestLeftDir, curRightDir);

                        if (currentAngle < lowestAngle) {
                            lowestRightDir = curRightDir;
                            lowestAngle = currentAngle;
                            stuckRight = curGate;
                        }
                    }
                    else {
                        endNode = gd[stuckLeft].leftV3;
                        curCycleEnd = stuckLeft;
                        break;
                    }

                    if (SomeMath.V2Cross(curLeftDir, lowestRightDir) >= 0) {
                        float currentAngle = Vector2.Angle(curLeftDir, lowestRightDir);
                        if (currentAngle < lowestAngle) {
                            lowestLeftDir = curLeftDir;
                            lowestAngle = currentAngle;
                            stuckLeft = curGate;
                        }
                    }
                    else {
                        endNode = gd[stuckRight].rightV3;
                        curCycleEnd = stuckRight;
                        break;
                    }
                }
                #endregion

                //flag to reach next point
                if (endNode.HasValue) {
                    if (curCycleStart != curCycleEnd) //move inside multiple cells
                        AddGate(curCycleStart, curCycleEnd, targetConnections, funnelPath.lastV3, endNode.Value);

                    funnelPath.AddMove(endNode.Value, (MoveState)(int)targetConnections[curCycleEnd].from.passability);
                }
            }

            if (curCycleEnd < gd.Count)
                AddGate(curCycleEnd, targetConnections.Count, targetConnections, funnelPath.lastV3, endV3);
        }


        void AddGate(int startCycle, int endCycle, List<CellContentGenericConnection> gates, Vector3 startPos, Vector3 endPos) {
            for (int cycle = startCycle; cycle < endCycle; cycle++) {
                if (gates[cycle].from.passability == gates[cycle].connection.passability)
                    continue;

                Vector3 ccInt;
                SomeMath.LineIntersectXZ(
                    gates[cycle].leftV3,
                    gates[cycle].rightV3,
                    startPos, endPos,
                    out ccInt);

                funnelPath.AddMove(ccInt, (MoveState)(int)gates[cycle].from.passability);
            }

        }
        #endregion

        #region distance   
        protected float ManhattanDistance(Cell cell) {
            return (Math.Abs(cell.centerV3.x - end_v3.x) + Math.Abs(cell.centerV3.y - end_v3.y) + Math.Abs(cell.centerV3.z - end_v3.z));
        }
        protected float ManhattanDistance(Vector3 pos) {
            return (Math.Abs(pos.x - end_v3.x) + Math.Abs(pos.y - end_v3.y) + Math.Abs(pos.z - end_v3.z));
        }
        protected float EuclideanDistance(Cell cell) {
            return Vector3.Distance(cell.centerV3, end_v3);
        }
        protected float EuclideanDistance(Vector3 pos) {
            return Vector3.Distance(pos, end_v3);
        }
        #endregion

        private void Raycast(Vector3 origin, Vector3 direction, out RaycastHitNavMesh hit,
            float length, int maxIterations, Passability expectedPassability, Area expectedArea, Cell cell) {
            HashSet<CellContentData> raycastExclude = new HashSet<CellContentData>();
            List<RaycastSomeData> raycastTempData = new List<RaycastSomeData>();
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

                            if (cell.passability != otherCell.passability || cell.area != otherCell.area) {
                                hit = new RaycastHitNavMesh(intersect, SomeMath.SqrDistance(origin, intersect) < maxLengthSqr);//!!!
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
    }
}
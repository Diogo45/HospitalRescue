using UnityEngine;
using System.Collections.Generic;
using System.Threading;

using K_PathFinder.Graphs;
using K_PathFinder.CoverNamespace;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

namespace K_PathFinder.PathGeneration {
    public class InfoTemplateCover : InfoTemplateAbstract {
        private HashSet<Cell> excluded = new HashSet<Cell>();
        private float maxMoveCost;
        private int maxChunkDepth;
        private bool ignoreCrouchCost;

        public InfoTemplateCover(PathFinderAgent agent, int maxChunkDepth, float maxMoveCost, bool ignoreCrouchCost) : base(agent, agent.position) {
            this.maxMoveCost = maxMoveCost;
            this.maxChunkDepth = maxChunkDepth;
            this.ignoreCrouchCost = ignoreCrouchCost;
        }

        public override void Work() {
            base.GetStartValues();
            CheckChunkDepth(); 
            agent.ReceiveCovers(SearchCover());
            callBack.Invoke();
        }

        private void CheckChunkDepth() {
            HashSet<Graph> checkedGraphs = new HashSet<Graph>();
            HashSet<Graph> lastIteration = new HashSet<Graph>();

            checkedGraphs.Add(startGraph);
            lastIteration.Add(startGraph);

            for (int i = 0; i < maxChunkDepth; i++) {
                HashSet<Graph> curIteration = new HashSet<Graph>();

                foreach (var lastGraph in lastIteration) {
                    for (int n = 0; n < 4; n++) {
                        Graph neighbourGraph;
                        while (true) {
                            if (PathFinder.GetGraphFrom(lastGraph.gridPosition, (Directions)n, properties, out neighbourGraph) && neighbourGraph.canBeUsed)
                                break;
                            Thread.Sleep(20);
                        }

                        if (neighbourGraph != null && checkedGraphs.Add(neighbourGraph))
                            curIteration.Add(neighbourGraph);
                    }
                }
                lastIteration = curIteration;
            }
        }

        //just use Dijkstra to find nearest cover
        private List<NodeCoverPoint> SearchCover() {
            List<NodeCoverPoint> result = new List<NodeCoverPoint>();
            CellPath path = new CellPath(startCell, start_v3);

            if (startCell.covers != null)
                result.AddRange(startCell.covers);

            excluded.Add(startCell);

            foreach (var connection in startCell.connections) {
                CellPath newPath = new CellPath(path, connection);
                newPath.g = connection.Cost(start_v3, properties, ignoreCrouchCost);
                AddCellNode(newPath);
            }
            
            while (true) {
                CellPath current = TakeCellNode();
                if (current == null || current.g > maxMoveCost)
                    break;

                Cell currentCell = current.last;

                if (excluded.Contains(currentCell))
                    continue;
                else
                    excluded.Add(currentCell);

#if UNITY_EDITOR
                if(Debuger_K.debugPath)
                    Debuger_K.AddPath(current.path[current.path.Count - 2].centerV3, current.path[current.path.Count - 1].centerV3, Color.green);                
#endif

                if(currentCell.covers != null)
                    result.AddRange(currentCell.covers);
  
                foreach (var connection in currentCell.connections) {
                    Cell newCell = connection.connection;

                    if (current.Contains(newCell) == false) {
                        CellPath newPath = new CellPath(current, connection);
                        newPath.g = current.g + connection.Cost(properties, ignoreCrouchCost);
                        AddCellNode(newPath);
                    }
                }                
            }
            return result;
        }
    }
}
using K_PathFinder.Graphs;
using System.Collections.Generic;
using UnityEngine;
//using PathFinder3.Debuger;

namespace K_PathFinder.PathGeneration {
    public class InfoTemplateBattleGrid : InfoTemplateAbstract {
        int depth;
        Vector3[] points;

        public InfoTemplateBattleGrid(PathFinderAgent agent, int depth, params Vector3[] points) : base(agent, agent.position) {
            this.depth = depth;
            this.points = points;
        }

        public override void Work() {
            HashSet<BattleGridPoint> result = new HashSet<BattleGridPoint>();

            for (int i = 0; i < points.Length; i++) {
                Graph curGraph;
                Cell curCell;
                Vector3 closest;
                GetChunkValues(points[i], out curGraph, out curCell, out closest);

                if (curGraph != null && curGraph.battleGrid != null)
                    result.Add(curGraph.battleGrid.GetClosestPoint(closest));
            }

            HashSet<BattleGridPoint> lastIteration = new HashSet<BattleGridPoint>();
            foreach (var item in result) {
                lastIteration.Add(item);
            }           

            HashSet<BattleGridPoint> curIteration;

            for (int i = 0; i < depth; i++) {
                curIteration = new HashSet<BattleGridPoint>();

                foreach (var item in lastIteration) {
                    foreach (var nb in item.neighbours) {
                        if (nb == null)
                            continue;

                        if (result.Add(nb)) 
                            curIteration.Add(nb);                        
                    }
                }
                lastIteration = curIteration;
            }

            agent.RecieveBattleGrid(result);
            callBack.Invoke();
        }
        
    }
}
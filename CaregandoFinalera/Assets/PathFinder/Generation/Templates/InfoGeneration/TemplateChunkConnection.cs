using UnityEngine;
using K_PathFinder.VectorInt ;
using System;
using K_PathFinder.Graphs;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

namespace K_PathFinder {
    public class GraphFinishTemplate : WorkTemplate {
        public Graph graph { get; private set; }
        Action callBack;

        public GraphFinishTemplate(Graph graph) : base(graph.gridPosition, graph.properties) {
            this.graph = graph;      
        }

        public void SetCallBack(Action callBack) {
            this.callBack = callBack;
        }

        public override void Work() {        
            if (graph == null) {
                Debug.LogWarning("graph null");
                callBack.Invoke();
            }
        
            graph.FunctionsToFinishGraphInMainThread();
            callBack.Invoke();

#if UNITY_EDITOR
            if (Debuger_K.doDebug) {
                graph.DebugGraph();
                //Debuger3.AddCells(graph.chunk, graph.properties, graph.cells);
                //Debuger3.AddEdges(graph.chunk, graph.properties, graph.edges);
                //Debuger3.AddNodes(graph.chunk, graph.properties, graph.nodes);
                //Debuger3.AddCovers(graph.chunk, graph.properties, graph.covers);
                //Debuger3.AddPortalBases(graph.chunk, graph.properties, graph.portalBases);
            }
#endif
        }  
    }
}

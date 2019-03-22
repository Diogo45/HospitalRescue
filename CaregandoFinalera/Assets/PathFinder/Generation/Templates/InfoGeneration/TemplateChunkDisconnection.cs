using System;
using K_PathFinder.Graphs;

namespace K_PathFinder {
    public class TemplateChunkDisconnection : WorkTemplate {       
        Graph graph;
        Action callBack = null;

        public TemplateChunkDisconnection(Graph graph) : base(graph.gridPosition, graph.properties) {
            this.graph = graph;
        }

        public void SetCallBack(Action callBack) {
            this.callBack = callBack;
        }

        public override void Work() {
            lock (graph) {
                graph.SetAsCanNotBeUsed();
                graph.FunctionToDisconnectGraphFromNeightboursInMainThread();
            }

            if(callBack != null)
                callBack.Invoke();
        }
    }
}

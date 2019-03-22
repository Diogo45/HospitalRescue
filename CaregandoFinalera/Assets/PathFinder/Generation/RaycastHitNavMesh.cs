using K_PathFinder.Graphs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder {
    public struct RaycastHitNavMesh {
        public readonly Vector3 point;
        public readonly bool isOnGraph;//is starting point on graph at all
        public readonly bool reachMaxIterations;

        public RaycastHitNavMesh(Vector3 point, bool isOnGraph, bool reachMaxIterations = false) {
            this.point = point;
            this.isOnGraph = isOnGraph;
            this.reachMaxIterations = reachMaxIterations;
        }
    }
}

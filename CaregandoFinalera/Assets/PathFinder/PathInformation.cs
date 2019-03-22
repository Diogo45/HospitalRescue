using UnityEngine;
using System.Collections.Generic;
using System;

namespace K_PathFinder.Graphs {
    public enum MoveState : int {
        crouch = 2,
        walk = 3
    }

    public class Path {
        public List<PathPoint> nodes;

        public Path(Vector3 start, MoveState state) {
            nodes = new List<PathPoint> { new PathPointMove(start, state) };
        }

        public void AddMove(Vector3 position, MoveState state) {
            nodes.Add(new PathPointMove(position, state));
        }

        public void AddJumpUp(Vector3 position, Vector3 axis) {
            nodes.Add(new PathPointJumpUp(position, axis));
        }

        public void AddJumpDown(Vector3 position, Vector3 landPoint) {
            nodes.Add(new PathPointJumpDown(position, landPoint));
        }

        public PathPoint last {
            get { return nodes[nodes.Count - 1]; }
        }
        public Vector2 lastV2 {
            get { return last.positionV2; }
        }
        public Vector3 lastV3 {
            get { return last.positionV3; }
        }
        
        public PathPoint first {
            get {
                if (nodes == null || nodes.Count == 0)
                    return null;
                else
                    return nodes[0];
            }
        }
        public Vector2 firstV2 {
            get { return nodes[0].positionV2; }
        }
        public Vector3 firstV3 {
            get { return nodes[0].positionV3; }
        }

        public void RemoveFirst() {
            if(nodes.Count > 0)
                nodes.RemoveAt(0);
        }

        public int count {
            get { return nodes == null ? 0 : nodes.Count; }
        }
    }

    public abstract class PathPoint : IGraphPoint {
        readonly float _x, _y, _z;

        public PathPoint(int x, int y, int z) {
            _x = x;
            _y = y;
            _z = z;
        }

        public PathPoint(Vector3 pos) {
            _x = pos.x;
            _y = pos.y;
            _z = pos.z;
        }

        public Vector2 positionV2 {
            get { return new Vector2(_x, _z); }
        }
        public Vector3 positionV3 {
            get { return new Vector3(_x, _y, _z); }
        }

        public float x {
            get { return _x; }
        }
        public float y {
            get { return _y; }
        }
        public float z {
            get { return _z; }
        }
    }

    public class PathPointMove : PathPoint {
        public readonly MoveState state;

        public PathPointMove(int x, int y, int z, MoveState state) : base(x, y, z) {
            this.state = state;
        }

        public PathPointMove(Vector3 position, MoveState state) : base(position) {
            this.state = state;
        }

        public override string ToString() {
            return state.ToString();
        }
    }

    public class PathPointJumpUp : PathPoint {
        public readonly Vector3 axis;

        public PathPointJumpUp(int x, int y, int z, Vector3 axis) : base(x, y, z) {
            this.axis = axis;
        }

        public PathPointJumpUp(Vector3 position, Vector3 axis) : base(position) {
            this.axis = axis;
        }        

        public override string ToString() {
            return "Jump Up";
        }
    }

    public class PathPointJumpDown : PathPoint {
        public readonly Vector3 landPoint;

        public PathPointJumpDown(int x, int y, int z, Vector3 landPoint) : base(x, y, z) {
            this.landPoint = landPoint;
        }

        public PathPointJumpDown(Vector3 position, Vector3 landPoint) : base(position) {
            this.landPoint = landPoint;
        }
        public override string ToString() {
            return "Jump Down";
        }
    }

}
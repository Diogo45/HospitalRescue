using UnityEngine;
using System;
using System.Collections.Generic;

using K_PathFinder.Graphs;
using K_PathFinder.CoverNamespace;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

namespace K_PathFinder {
    public class PathFinderAgent : MonoBehaviour {
        //general values
        public AgentProperties properties;

        //public flags
        [HideInInspector]
        public bool ignoreCrouchCost = false;      

        //private flags
        private bool _canRecieveGoals = true;
        private bool _canRecieveResults = true;

        //recived values
        private Path _path = null;
        private IEnumerable<BattleGridPoint> _battleGrid = null;
        private List<NodeCoverPoint> _covers = null;    

        //on recive delegates not thread safe
        private Action<Path> _recievePathDelegate_NTS = null;
        private Action<IEnumerable<BattleGridPoint>> _recieveBattleGridDelegate_NTS = null;
        private Action<IEnumerable<NodeCoverPoint>> _recieveCoverPointDelegate_NTS = null;

        //on recive delegates thread safe
        private Action<Path> _recievePathDelegate_TS = null;
        private Action<IEnumerable<BattleGridPoint>> _recieveBattleGridDelegate_TS = null;
        private Action<IEnumerable<NodeCoverPoint>> _recieveCoverPointDelegate_TS = null;
        private bool _recievePathUsed = true, _recieveBattleGridUsed = true, _recieveCoverPointUsed = true;

        public virtual Vector3 position {
            get { return transform.position; }
        }

        //on and off
        public void On() {
            if (properties == null) {
                Debug.LogErrorFormat("properties == null on {0}", gameObject.name);
                return;
            }

            _canRecieveResults = true;
            _canRecieveGoals = true; 
        }
        public void Off() {
            _canRecieveResults = false;
            _canRecieveGoals = false;   
        }

        //more precise on and off
        public void StartRecieveGoals() {
            _canRecieveGoals = true;
        }
        public void StartRecieveResults() {
            _canRecieveResults = true;
        }
        public void StopRecieveGoals() {
            _canRecieveGoals = false;
        }
        public void StopRecieveResults() {
            _canRecieveResults = false;
        }

        //userful values for controler
        public Path path {
            get {
                lock (this)
                    return _path;
            }
        }
        public PathPoint nextPoint {
            get {
                if (path == null)
                    return null;

                return path.first;
            }
        }
        public void RemoveNextPoint() {
            if (path == null)
                return;

            path.RemoveFirst();
        }

        public List<NodeCoverPoint> covers {
            get {
                lock (this)
                    return _covers;
            }
        }
        public IEnumerable<BattleGridPoint> battleGrid {
            get {
                lock (this)
                    return _battleGrid;
            }
        }

        //execute threadsafe delegates
        void Update() {
            if (!_recievePathUsed && _recievePathDelegate_TS != null) {
                _recievePathUsed = true;
                _recievePathDelegate_TS.Invoke(_path);
            }

            if (!_recieveBattleGridUsed && _recieveBattleGridDelegate_TS != null) {
                _recieveBattleGridUsed = true;
                _recieveBattleGridDelegate_TS.Invoke(_battleGrid);
            }

            if (!_recieveCoverPointUsed && _recieveCoverPointDelegate_TS != null) {
                _recieveCoverPointUsed = true;
                _recieveCoverPointDelegate_TS.Invoke(_covers);
            }
        }

        //recieve delegates
        public void SetRecievePathDelegate(Action<Path> pathDelegate, AgentDelegateMode mode = AgentDelegateMode.NotThreadSafe) {
            switch (mode) {
                case AgentDelegateMode.ThreadSafe:
                    _recievePathDelegate_TS = pathDelegate;
                    break;
                case AgentDelegateMode.NotThreadSafe:
                    _recievePathDelegate_NTS = pathDelegate;
                    break;
            }  
        }
        public void RemoveRecievePathDelegate(AgentDelegateMode mode = AgentDelegateMode.NotThreadSafe) {
            switch (mode) {
                case AgentDelegateMode.ThreadSafe:
                    _recievePathDelegate_TS = null;
                    _recievePathUsed = true;
                    break;
                case AgentDelegateMode.NotThreadSafe:
                    _recievePathDelegate_NTS = null;
                    break;
            }
        }

        public void SetRecieveBattleGridDelegate(Action<IEnumerable<BattleGridPoint>> gridDelegate, AgentDelegateMode mode = AgentDelegateMode.NotThreadSafe) {
            switch (mode) {
                case AgentDelegateMode.ThreadSafe:
                    _recieveBattleGridDelegate_TS = gridDelegate;
                    _recieveBattleGridUsed = true;
                    break;
                case AgentDelegateMode.NotThreadSafe:
                    _recieveBattleGridDelegate_NTS = gridDelegate;
                    break;
            }      
        }
        public void RemoveRecieveBattleGridDelegate(AgentDelegateMode mode = AgentDelegateMode.NotThreadSafe) {
            switch (mode) {
                case AgentDelegateMode.ThreadSafe:
                    _recieveBattleGridDelegate_TS = null;
                    break;
                case AgentDelegateMode.NotThreadSafe:
                    _recieveBattleGridDelegate_NTS = null;
                    break;
            }
        }

        public void SetRecieveCoverDelegate(Action<IEnumerable<NodeCoverPoint>> coverDelegate, AgentDelegateMode mode = AgentDelegateMode.NotThreadSafe) {
            switch (mode) {
                case AgentDelegateMode.ThreadSafe:
                    _recieveCoverPointDelegate_TS = coverDelegate;
                    break;
                case AgentDelegateMode.NotThreadSafe:
                    _recieveCoverPointDelegate_NTS = coverDelegate;
                    break;
            }
        }
        public void RemoveRecieveCoverDelegate(AgentDelegateMode mode = AgentDelegateMode.NotThreadSafe) {
            switch (mode) {
                case AgentDelegateMode.ThreadSafe:
                    _recieveCoverPointDelegate_TS = null;
                    _recieveCoverPointUsed = true;
                    break;
                case AgentDelegateMode.NotThreadSafe:
                    _recieveCoverPointDelegate_NTS = null;
                    break;
            }
        }

        //set goals and recieve results
        //threadsafe
        public void SetGoalMoveHere(Vector3 destination, bool snapToNavMesh = false, bool applyRaycastBeforeFunnel = false, int maxRaycastIterations = 100) {
            if (_canRecieveGoals == false)
                return;

            if (!properties.doNavMesh) {
                Debug.LogWarning("you trying to get path when you dont even set properties to generate NavMesh");
                return;
            }

            PathFinder.GetPath(this, destination, position, snapToNavMesh, null, applyRaycastBeforeFunnel, maxRaycastIterations, ignoreCrouchCost);
        }
        public void SetGoalMoveHere(Vector3 start, Vector3 destination, bool snapToNavMesh = false, bool applyRaycastBeforeFunnel = false, int maxRaycastIterations = 100) {
            if (_canRecieveGoals == false)
                return;

            if (!properties.doNavMesh) {
                Debug.LogWarning("you trying to get path when you dont even set properties to generate NavMesh");
                return;
            }

            PathFinder.GetPath(this, destination, start, snapToNavMesh, null, applyRaycastBeforeFunnel, maxRaycastIterations);
        }
        
        public void SetGoalGetBattleGrid(int depth, params Vector3[] positions) {
            if (_canRecieveGoals == false)
                return;

            if (!properties.doNavMesh) {
                Debug.LogWarning("you trying to get battle grid when you dont even set properties to generate battle grid");
                return;
            }

            if (positions.Length == 0)
                PathFinder.GetBattleGrid(this, depth, null, transform.position);
            else
                PathFinder.GetBattleGrid(this, depth, null, positions);
        }
        public void SetGoalFindCover(int minChunkDepth, float maxCost) {
            if (_canRecieveGoals == false)
                return;

            if (!properties.doNavMesh) {
                Debug.LogWarning("you trying to find cover when you dont even set properties to generate covers");
                return;
            }

            PathFinder.GetCover(this, minChunkDepth, maxCost, null, ignoreCrouchCost);
        }

        //used for recieve stuff from pathfinder
        //not threadsafe
        public void ReceivePath(Path path) {
            lock (this) {
                if (_canRecieveResults == false)
                    return;

                _path = path;

                if (_recievePathDelegate_NTS != null)
                    _recievePathDelegate_NTS.Invoke(path);

                if (_recievePathDelegate_TS != null)
                    _recievePathUsed = false;
            }
        }
        public void ReceiveCovers(List<NodeCoverPoint> covers) {
            lock (this) {
                if (_canRecieveResults == false)
                    return;

                _covers = covers;

                if (_recieveCoverPointDelegate_NTS != null)
                    _recieveCoverPointDelegate_NTS.Invoke(covers);

                if (_recieveCoverPointDelegate_TS != null)
                    _recieveCoverPointUsed = false;
            }             
        }
        public void RecieveBattleGrid(IEnumerable<BattleGridPoint> battleGrid) {
            lock (this) {
                if (_canRecieveResults == false)
                    return;

                _battleGrid = battleGrid;

                if (_recieveBattleGridDelegate_NTS != null) 
                    _recieveBattleGridDelegate_NTS.Invoke(battleGrid);

                if (_recieveBattleGridDelegate_TS != null)
                    _recieveBattleGridUsed = false;
            }
        }
        
        /// <summary>
        /// iterate through nodes and return if there is node other than move node. movable mean it's only when you move. not jump. so you can tell if agent about to jump
        /// </summary>
        public bool MovableDistanceLesserThan(float targetDistance, out float distance, out PathPoint lastPoint, out bool reachLastPoint) {         
            if (path == null || path.count == 0) {        
                distance = 0;
                lastPoint = null;
                reachLastPoint = true;
                return false;
            }
            Vector2 agentPos = ToV2(position);

            var p = path.nodes;
            distance = Vector2.Distance(agentPos, p[0].positionV2);

            if (p[0] is PathPointMove == false) {
                lastPoint = p[0];
                reachLastPoint = p.Count == 1;
                return true;
            }

            if (p.Count == 1) {
                lastPoint = p[0];
                reachLastPoint = true;
                return distance < targetDistance;
            }

#if UNITY_EDITOR
            //Debuger.AddLabel(p[0].positionV3, 0 + ":" + distance + ":" + p[0].GetType() + ": count:" + p.Count);
            //Debuger.AddDot(p[0].positionV3, Color.green, 0.01f);
#endif

            for (int i = 1; i < p.Count; i++) {          

                distance += Vector2.Distance(p[i - 1].positionV2, p[i].positionV2);

#if UNITY_EDITOR
                //Debuger.AddLabel(p[i].positionV3 + (Vector3.up * 0.02f * i), i + ":" + distance + ":" + p[i].GetType() + ": count:" + p.Count);
                //Debuger.AddDot(p[i].positionV3 + (Vector3.up * 0.02f * i), Color.green, 0.01f);
#endif


                if (distance > targetDistance) {
                    lastPoint = p[i];
                    reachLastPoint = i == p.Count - 1; 
                    return false;
                }

                if (p[i] is PathPointMove == false) {
                    lastPoint = p[i];
                    reachLastPoint = i == p.Count - 1;
                    return true;
                }
            }

            lastPoint = p[p.Count - 1];
            reachLastPoint = true;
            return true;
        }
        public bool MovableDistanceLesserThan(float targetDistance, out float distance, out bool reachLastPoint) {
            PathPoint node;
            return MovableDistanceLesserThan(targetDistance, out distance, out node, out reachLastPoint);
        }
        public bool MovableDistanceLesserThan(float targetDistance, out bool reachLastPoint) {
            float distance;
            PathPoint node;
            return MovableDistanceLesserThan(targetDistance, out distance, out node, out reachLastPoint);
        }
        public bool MovableDistanceLesserThan(float targetDistance, out PathPoint node) {
            float distance;
            bool reachLastPoint;
            return MovableDistanceLesserThan(targetDistance, out distance, out node, out reachLastPoint);
        }
        public bool MovableDistanceLesserThan(float targetDistance, out float distance) {
            PathPoint node;
            bool reachLastPoint;
            return MovableDistanceLesserThan(targetDistance, out distance, out node, out reachLastPoint);
        }
        public bool MovableDistanceLesserThan(float targetDistance) {
            float distance;
            PathPoint node;
            bool reachLastPoint;
            return MovableDistanceLesserThan(targetDistance, out distance, out node, out reachLastPoint);
        }
        
        //acessors
        public bool haveNextPoint {
            get { return nextPoint != null; }
        }

        //shortcuts
        private static Vector2 ToV2(Vector3 v3) {
            return new Vector2(v3.x, v3.z);
        }
        private static Vector3 ToV3(Vector2 v2) {
            return new Vector3(v2.x, 0, v2.y);
        }
    }
}
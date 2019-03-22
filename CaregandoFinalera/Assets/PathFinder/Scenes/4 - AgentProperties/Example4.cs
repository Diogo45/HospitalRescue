using K_PathFinder.Graphs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.Samples {
    public class Example4 : MonoBehaviour {
        public GameObject cameraGameObject, targetGameObject, linePrefab;
        public GameObject[] agents;

        private LineRenderer[] _lines;
        private PathFinderAgent[] _agents;
        private Camera _camera;
        private bool update; //used as flag. if true then update

        // Use this for initialization
        void Start() {
            _camera = cameraGameObject.GetComponent<Camera>();
            _agents = new PathFinderAgent[agents.Length];
            _lines = new LineRenderer[agents.Length];

            for (int i = 0; i < agents.Length; i++) {         
                GameObject lineGameObject = Instantiate(linePrefab);
                _lines[i] = lineGameObject.GetComponent<LineRenderer>();
                _agents[i] = agents[i].GetComponent<PathFinderAgent>();
                int tempValue = i;//or else delegates wound work as expected
                _agents[i].SetRecievePathDelegate((Path path) => { RecivePathDlegate(path, tempValue); }, AgentDelegateMode.ThreadSafe);
            }

            update = true;
        }

        // Update is called once per frame
        void Update() {
            RaycastHit hit;
            if (Input.GetMouseButtonDown(0) && Physics.Raycast(_camera.ScreenPointToRay(Input.mousePosition), out hit, 10000f, 1)) {
                targetGameObject.transform.position = hit.point;
                update = true;
            }

            if (update) {
                update = false;
                for (int i = 0; i < _agents.Length; i++) {
                    _agents[i].SetGoalMoveHere(targetGameObject.transform.position, true);
                }
            }
        }

        //path are path to target
        //it return points
        //first point are next point agent need to reach
        //here we also add agent start position to make green line
        private void RecivePathDlegate(Path path, int index) {
            Vector3[] points = new Vector3[path.count + 1];
            Vector3 add = Vector3.up * 0.2f; //some height offset. otherwise line will be too close to floor
            
            points[0] = _agents[index].transform.position + add;
            for (int i = 0; i < path.count; i++) {
                points[i + 1] = path.nodes[i].positionV3 + add;
            }

            _lines[index].positionCount = path.count + 1;
            _lines[index].SetPositions(points);
        }
    }
}
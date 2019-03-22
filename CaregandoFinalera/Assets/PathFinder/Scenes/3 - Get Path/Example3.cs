using K_PathFinder.Graphs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace K_PathFinder.Samples {
    public class Example3 : MonoBehaviour {
        public GameObject 
            cameraGameObject, 
            agentGameObject, 
            targetGameObject;

        PathFinderAgent _agent;
        Camera _camera;
        LineRenderer _line;
        bool update; //used as flag. if true then update

        // Use this for initialization
        void Start() {   
            _camera = cameraGameObject.GetComponent<Camera>();
            _line = GetComponent<LineRenderer>();
            _agent = agentGameObject.GetComponent<PathFinderAgent>();           
            _agent.SetRecievePathDelegate(RecivePathDlegate, AgentDelegateMode.ThreadSafe); //setting here delegate to update line renderrer
            update = true;
        }

        // Update is called once per frame
        void Update() {
            RaycastHit hit;
            if (Input.GetMouseButtonDown(0) && Physics.Raycast(_camera.ScreenPointToRay(Input.mousePosition), out hit, 10000f, 1)) {
                agentGameObject.transform.position = hit.point;
                update = true;
            }

            if (Input.GetMouseButtonDown(1) && Physics.Raycast(_camera.ScreenPointToRay(Input.mousePosition), out hit, 10000f, 1)) {
                targetGameObject.transform.position = hit.point;
                update = true;
            }

            if (update) {
                update = false;
                _agent.SetGoalMoveHere(targetGameObject.transform.position, true);//here we requesting path
            }
        }

        //path are path to target
        //it return points
        //first point are next point agent need to reach
        //here we also add agent start position to make green line
        private void RecivePathDlegate(Path path) {    
            Vector3[] points = new Vector3[path.count + 1];
            Vector3 add = Vector3.up * 0.2f; //some height offset. otherwise line will be too close to floor

            points[0] = _agent.transform.position + add;
            for (int i = 0; i < path.count; i++) {
                points[i + 1] = path.nodes[i].positionV3 + add;
            }

            _line.positionCount = path.count + 1;
            _line.SetPositions(points);
        }
    }
}
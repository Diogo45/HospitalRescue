using K_PathFinder;
using K_PathFinder.Graphs;
using K_PathFinder.PFDebuger;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

//example of basic agent
//it creates two pathfinder agents in process. one for get actual path and one for debug path
namespace K_PathFinder.Samples {
    [RequireComponent(typeof(CharacterController))]
    public class AgentUsage : MonoBehaviour {
        const float cameraMaxDistance = 50f, cameraMinDistance = 5f;

        //camera control
        public Camera myCamera;
        [Range(10f, 90f)]
        public float cameraAngle = 45;
        [Range(cameraMinDistance, cameraMaxDistance)]
        public float cameraDistance = 10;
        private float cameraTargetDistance;

        [Range(1, 5)]
        public float speed = 2;
        public AgentProperties properties;
        PathFinderAgent 
            _agentForPath, //this agent for actual navigation
            _agentForDebugPath; //this agent for debuging path
        CharacterController _controler;

        //debug values 
        public bool debugLineRenderer = true;
        public Material debugMaterial, pathMaterial;
        [Range(0f, 1f)]
        public float debugWidth = 0.1f;
        private LineRenderer _lineDebuger, _linePath;

        //some stuff for ignoring UI
        PointerEventData pointerEventData;
        List<RaycastResult> eventHits = new List<RaycastResult>();

        void Start() {
            //create agents
            //you can just add agent as normal component to your prefab. just dont forget to put PathFinderAgent in it
            //in this case we just need two agents so i add it in code to avoid agent mixing          
            _agentForPath = gameObject.AddComponent<PathFinderAgent>();
            _agentForPath.properties = properties;            

            _agentForDebugPath = gameObject.AddComponent<PathFinderAgent>();
            _agentForDebugPath.properties = properties;

            _controler = GetComponent<CharacterController>();

            transform.rotation = Quaternion.Euler(Vector3.zero);
            cameraTargetDistance = cameraDistance;

            _linePath = GetLineRenderer(pathMaterial, debugWidth);
            _lineDebuger = GetLineRenderer(debugMaterial, debugWidth); 

            pointerEventData = new PointerEventData(EventSystem.current);
        }

        void Update() {
            //fancy camera
            cameraTargetDistance -= Input.GetAxis("Mouse ScrollWheel") * 5;//add mouse wheel
            cameraTargetDistance = Mathf.Clamp(cameraTargetDistance, cameraMinDistance, cameraMaxDistance);//clamp camera distance
            cameraDistance = Mathf.Lerp(cameraDistance, cameraTargetDistance, 20 * Time.deltaTime);//lerp camera to avoid jiggling
            myCamera.transform.position = transform.position + (Quaternion.Euler(cameraAngle, 0, 0) * Vector3.back) * cameraDistance;//our position + camera direction * camera distance 
            myCamera.transform.LookAt(transform.position, Vector3.up);
            
            //detecting if over button (not like it's lots of buttons but still)      
            pointerEventData.position = Input.mousePosition;
            EventSystem.current.RaycastAll(pointerEventData, eventHits);

            //raycasting
            Ray ray = myCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            //exclude anything that include buttons
            if (eventHits.Exists(x => x.gameObject.GetComponent<Button>() != null) == false && Physics.Raycast(ray, out hit, 10000f, _agentForPath.properties.includedLayers)) {
                _agentForDebugPath.SetGoalMoveHere(hit.point, true, true);

                //setting goal
                if (Input.GetMouseButtonDown(0))
                    _agentForPath.SetGoalMoveHere(hit.point, true, true);
            }

            //updating lines
            UpdateLineRenderer(_lineDebuger, _agentForDebugPath.path, debugLineRenderer); //
            UpdateLineRenderer(_linePath, _agentForPath.path, debugLineRenderer);

            if (_agentForPath.nextPoint == null)
                return;

            Vector3 position = transform.position;
            Vector2 myPos = new Vector2(position.x, position.z);//top view

            //if next point are close enougle then we remove it
            if (Vector2.Distance(myPos, _agentForPath.nextPoint.positionV2) < properties.radius)
                _agentForPath.RemoveNextPoint();

            //if next point still exist then we move towards it
            if (_agentForPath.haveNextPoint) 
                _controler.SimpleMove(new Vector3(_agentForPath.nextPoint.x - position.x, 0, _agentForPath.nextPoint.z - position.z).normalized * speed);            
        }

        //create and return LineRenderer 
        private LineRenderer GetLineRenderer(Material material, float width) {
            GameObject lineGO = new GameObject("line renderer");
            LineRenderer lineR = lineGO.AddComponent<LineRenderer>();
            lineR.startWidth = width;
            lineR.endWidth = width;
            lineR.material = material;
            return lineR;
        }

        //set LineRenderer into path position
        private void UpdateLineRenderer(LineRenderer lineRenderer, Path path, bool doDebug) {
            if (doDebug) {
                if (path == null)
                    return;

                var pathNodes = path.nodes;

                if (pathNodes == null)
                    return;

                if (pathNodes.Count > 0) {
                    lineRenderer.positionCount = pathNodes.Count + 1;
                    Vector3[] pathPos = new Vector3[pathNodes.Count + 1];
                    pathPos[0] = transform.position;

                    for (int i = 0; i < pathNodes.Count; i++) {
                        pathPos[i + 1] = pathNodes[i].positionV3 + Vector3.up;
                    }

                    lineRenderer.SetPositions(pathPos);
                }
                else {
                    lineRenderer.positionCount = 0;
                }                
            }
            else
                lineRenderer.positionCount = 0;
        }
    }
}

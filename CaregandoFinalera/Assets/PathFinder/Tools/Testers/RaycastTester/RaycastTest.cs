#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif
using UnityEngine;

namespace K_PathFinder {
#if UNITY_EDITOR
    [ExecuteInEditMode()]
#endif
    public class RaycastTest : MonoBehaviour {
#if UNITY_EDITOR
        public AgentProperties properties;

        public int tests = 4;

        void Update() {
            if (properties == null)
                return;

            RaycastHit raycastHit;
            if (!Physics.Raycast(transform.position, Vector3.down, out raycastHit, 10))
                return;

            Vector3 p = raycastHit.point;
            Debug.DrawLine(transform.position, p, Color.red);

            //Debuger_K.ClearGeneric();
            
            RaycastHitNavMesh raycastHitNavMesh;
            for (int i = 0; i < tests; i++) {
                float x = Mathf.Cos((i / (float)tests) * 2 * Mathf.PI);
                float z = Mathf.Sin((i / (float)tests) * 2 * Mathf.PI);

                //var q = Quaternion.LookRotation(transform.forward + new Vector3(x, 0, z), Vector3.up);

                PathFinder.Raycast(p, new Vector3(x, 0, z), properties, out raycastHitNavMesh);
                if (raycastHitNavMesh.isOnGraph) {
                    Debuger_K.AddLine(p, raycastHitNavMesh.point, Color.blue);
                    Debuger_K.AddLabel(raycastHitNavMesh.point, "H");
                }
            }

            //PathFinder.Raycast(p, transform.forward * -1, properties, out raycastHitNavMesh);
            //if (raycastHitNavMesh.isOnGraph) {
            //    Debuger_K.AddLine(p, raycastHitNavMesh.point, Color.blue);
            //    Debuger_K.AddLabel(raycastHitNavMesh.point, "B");
            //}
            //PathFinder.Raycast(p, transform.transform.right * -1, properties, out raycastHitNavMesh);
            //if (raycastHitNavMesh.isOnGraph) {
            //    Debuger_K.AddLine(p, raycastHitNavMesh.point, Color.blue);
            //    Debuger_K.AddLabel(raycastHitNavMesh.point, "L");
            //}
            //PathFinder.Raycast(p, transform.right, properties, out raycastHitNavMesh);
            //if (raycastHitNavMesh.isOnGraph) {
            //    Debuger_K.AddLine(p, raycastHitNavMesh.point, Color.blue);
            //    Debuger_K.AddLabel(raycastHitNavMesh.point, "R");
            //}
        }
#endif
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class IntermediaryController : MonoBehaviour {
    public string IntName
    {
        set
        {
            name = value;
            IntermediaryCache[name] = gameObject;
        }
    }

    private static Dictionary<string, GameObject> IntermediaryCache;
    public List<GameObject> Children;
    private List<GameObject> Intermediaries;

	// Use this for initialization
	void Start () {
        //Intermediaries.AddRange(GameObject.FindGameObjectsWithTag("Intermediary"));

	}
	
	// Update is called once per frame
	void Update () {
		
	}
}

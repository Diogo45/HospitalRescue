using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class AuxinController {
	//is auxin taken?
	public bool taken = false;
    //position
    public Vector3 position;
    //name
    public string name;


    private static Dictionary<string, AuxinController> AuxinCache = new Dictionary<string, AuxinController>();

    public static AuxinController GetAuxinName(string idName)
    {
        if (AuxinCache.ContainsKey(idName))
        {
            return AuxinCache[idName];
        }
        else
        {
            //Debug.LogWarning(idName + ": Cell not found!!!");
            return null;
        }
    }

    //min distance from a taken agent
    //when a new agent find it in his personal space, test the distance with this value to see which one is smaller
    private float minDistance;
	//agent who took this auxin
	private GameObject agent;
    //cell who has this auxin
    private GameObject cell;

    void Awake(){
		//above agent radius, for it need to be high, since will store the min distance
		minDistance = 2;
	}

	//Reset auxin to his default state, for each update
	public void ResetAuxin(){
		//GetComponent<MeshRenderer> ().material.color = Color.clear;
		SetMinDistance (2);
		SetAgent (null);
		taken = false;
	}

	//GET-SET
	public float GetMinDistance(){
		return this.minDistance;
	}
	public void SetMinDistance(float minDistance){
		this.minDistance = minDistance;
	}
	public GameObject GetAgent(){
		return this.agent;
	}
	public void SetAgent(GameObject agent){
		this.agent = agent;
	}
    public GameObject GetCell()
    {
        return this.cell;
    }
    public void SetCell(GameObject cell)
    {
        AuxinCache[name] = this;
        this.cell = cell;
    }
}

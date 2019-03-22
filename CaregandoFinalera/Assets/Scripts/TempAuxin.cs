using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TempAuxin : MonoBehaviour {
    //is auxin taken?
    public bool taken = false;
    //min distance from a taken agent
    //when a new agent find it in his personal space, test the distance with this value to see which one is smaller
    private float minDistance;
    //agent who took this auxin
    private GameObject agent;
    // Use this for initialization
    void Awake()
    {
        //above agent radius, for it need to be high, since will store the min distance
        minDistance = 2;
    }

    //Reset auxin to his default state, for each update
    public void ResetAuxin()
    {
        //GetComponent<MeshRenderer> ().material.color = Color.clear;
        SetMinDistance(2);
        SetAgent(null);
        taken = false;
    }

    //GET-SET
    public float GetMinDistance()
    {
        return this.minDistance;
    }
    public void SetMinDistance(float minDistance)
    {
        this.minDistance = minDistance;
    }
    public GameObject GetAgent()
    {
        return this.agent;
    }
    public void SetAgent(GameObject agent)
    {
        this.agent = agent;
    }
}

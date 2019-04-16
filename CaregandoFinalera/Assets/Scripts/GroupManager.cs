using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

public class GroupManager : MonoBehaviour {

    public List<GameObject> agentGroups;
    public GameObject circle;
    public GameObject[] furniturePrefabs;
    public float setCohesion = 2;
    private float prevCohesion;
    public bool update;
    public GameObject[] agentPrefabs;


	// Use this for initialization
	void Start () {
        //SpawnGroup(new Vector3(1, 0, 1), 5,1.5f,15);
        foreach(GameObject group in agentGroups)
        {
            Destroy(group);
        }
        SpawnGroups("groups10_data.txt", "groups10_pos.txt");
        update = false;
        prevCohesion = setCohesion;
	}
	
	// Update is called once per frame
	void Update () {
        if(prevCohesion != setCohesion)
        {
            update = true;
            prevCohesion = setCohesion;
        }
        if (update)
        {
            Start();
        }
		
	}

    public void SpawnGroups(string file1, string file2)
    {
        var data = new List<string[]>();

        using (StreamReader sr = new StreamReader("Assets/CrowdData/" + file1))
        {
            for (int i = 0; i < 7; i++)
            {
                data.Add(sr.ReadLine().Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries)); //data [0] ID, [1] n agentes, [2]HCD, [3]cohesion,[4] radius,[5] disposition, [6] ang variation
            }
        }

        using (StreamReader sr = new StreamReader("Assets/CrowdData/" + file2))
        {
            for (int i = 0; i < 2; i++)
            {
                data.Add(sr.ReadLine().Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries)); //data[7],[8] posicao
            }
        }

        int size = data[0].Length;
        Vector3 max, min;
        max = min =  new Vector3(float.Parse(data[7][0]), 0, float.Parse(data[8][0])); 

        for (int i = 0; i < size; i++)
        {
            
            var spawnPos = new Vector3(float.Parse(data[7][i]), 0, float.Parse(data[8][i]));

            if (spawnPos.x > max.x)
                max.x = spawnPos.x;
            else if (spawnPos.x < min.x)
                min.x = spawnPos.x;

            if (spawnPos.z > max.z)
                max.z = spawnPos.z;
            else if (spawnPos.z < min.z)
                min.z = spawnPos.z;


            SpawnGroup(spawnPos, float.Parse(data[4][i]), float.Parse(data[3][i]), (int)float.Parse(data[1][i]),float.Parse(data[6][i]));
            //Debug.Log(data[0][i] + " " + data[1][i]);
        }

        SpawnObjects(max, min);

    }

    private void SpawnGroup(Vector3 pos, float radius,float cohesion,int nAgents, float angleVar)
    {
        var group = new GameObject();
        group.name = "Group";

        var g = Instantiate(circle, pos,Quaternion.identity,group.transform);
        g.transform.localScale = new Vector3(2 * radius, g.transform.localScale.y, 2* radius);

        float varCoh = (cohesion / 3f);
        float groupRadius = radius;//(0.30f * (1f - varCoh) + 0.45f ) * radius;
        float degIncr = 2f * Mathf.PI / nAgents;
        float currAng = 0;

        

        for(int i = 0; i < nAgents; i++)
        {
            var radiusVariance = radius * 0.10f * (1f - varCoh); //variation on distance from center of the group
            var spawnRadius = groupRadius + (Random.Range(-radiusVariance, 0));

            var posAngVar = degIncr * (1f - varCoh) * Random.Range(-0.5f,0.5f); //spawn position angle variation to distribute agents more when in low cohesion
            var spawnPos = pos + new Vector3(Mathf.Cos(currAng + posAngVar) * spawnRadius, 0, Mathf.Sin(currAng + posAngVar) * spawnRadius);
             
            var centerLookAng = -((currAng * 180f) / Mathf.PI) -90; //angle pointing towards center of group in euler ang
            var angleVariation = angleVar * (Random.value * 200 - 100);
            var lookAng = centerLookAng + angleVariation;

            var a = Instantiate(agentPrefabs[Random.Range(0,agentPrefabs.Length)], spawnPos, Quaternion.identity,group.transform);
            a.transform.Rotate(new Vector3(0, lookAng, 0));

            float moveVariance = radius * 0.2f * (1f - varCoh);
            a.transform.position += a.transform.forward * moveVariance;

            currAng += degIncr;
        }
        agentGroups.Add(group);

    }

    private void SpawnObjects(Vector3 max, Vector3 min)
    {
        int spawned = 0;
        int attempts = 0;

        while(spawned < 5 && attempts <50)
        {
            var pos = new Vector3(Random.Range(min.x, max.x), 0f, Random.Range(min.z, max.z));
            var hit = Physics.OverlapBox(pos, Vector3.one);
            if (hit.Length == 0)
            {
                var chooseObj = furniturePrefabs[Random.Range(0, furniturePrefabs.Length)];
                var obj = Instantiate(chooseObj,pos,chooseObj.transform.rotation);
                obj.transform.Rotate(Vector3.forward,Random.Range(0,4) * 90);
                spawned++;
            }
            attempts++;
        }


    }
}

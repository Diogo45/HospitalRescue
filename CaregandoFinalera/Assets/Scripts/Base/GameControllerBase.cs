using UnityEngine;
using System.Collections.Generic;
using System.IO;
//to build nav mesh at runtime


public class GameControllerBase : MonoBehaviour {
    private int framecount = 0;
    //scenario X
    //LIMITATION: this value need to be even. Odd values may generate error
    public float scenarioSizeX;
	//scenario Z
	//LIMITATION: this value need to be even. Odd values may generate error
	public float scenarioSizeZ;
	//agent prefab
	public GameObject agent;
	//agent radius
	public float agentRadius;
    //cell prefab
    public GameObject cell;
    //qnt of agents in the scene
    public int qntAgents;
    //keep the agents already destroyed
    List<int> agentsDestroyed = new List<int>();    
    //radius for auxin collide
    public float auxinRadius;
    //save config file?
    public bool saveConfigFile;
    //load config file?
    public bool loadConfigFile;
    //config filename
    public string configFilename;
    //obstacles filename
    public string obstaclesFilename;
	//exit filename
	public string exitFilename;
    //simulation scenario
    //0 = normal, all agents bottom
    //1 = half agents top, hald bottom
    public int scenarioType;

	//max agent spawn x
	private int spawnPositionX;
	//max agent spawn z
	private int spawnPositionZ;
    //density
    private float PORC_QTD_Marcacoes = 0.65f;
    //qnt of auxins on the ground
    private int qntAuxins;
	//exit file
	private StreamWriter exitFile;

	//on destroy application, close the file
	void OnDestroy(){
		exitFile.Close ();
	}

    // Use this for initialization
    void Awake () {

		//camera height and center position
		//scene is 3:2 scale, so we divide for 3 or 2, depending the parameter
		//float cameraHeight = scenarioSizeX/3;
		//float cameraPositionX = scenarioSizeX/2;
		//float cameraPositionZ = scenarioSizeZ/2;
		//GameObject camera = GameObject.Find ("Camera");
		//camera.transform.position = new Vector3(cameraPositionX, camera.transform.position.y, cameraPositionZ);
		//camera.GetComponent<Camera>().orthographicSize = cameraHeight;
		
		Terrain terrain = GameObject.Find("Terrain").GetComponent<Terrain>();
		//change terrain size according informed
		terrain.terrainData.size = new Vector3(scenarioSizeX, terrain.terrainData.size.y, scenarioSizeZ);

        //Get map size        
        spawnPositionX = (int)Mathf.Floor(terrain.terrainData.size.x - 1f);
        spawnPositionZ = (int)Mathf.Floor(terrain.terrainData.size.z - (terrain.terrainData.size.z-2f));

        //if loadConfigFile is checked, we do not generate the initial scenario. We load it from the Config.xml (or something like this) file
        if (loadConfigFile)
        {
            LoadConfigFile();

			//build the navmesh at runtime
			UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        }
        else
        {
			//build the navmesh at runtime
			UnityEditor.AI.NavMeshBuilder.BuildNavMesh();

			//draw Obstacles
			//DrawObstacles();

            //first of all, create all cells (with this scene and this agentRadius = 1 : 150 cells)
            //since radius = 1; diameter = 2. So, iterate 2 in 2
			//if the radius varies, this 2 operations adjust the cells
			Vector3 newPosition = new Vector3(cell.transform.position.x*agentRadius,
				cell.transform.position.y*agentRadius, cell.transform.position.z*agentRadius);
			Vector3 newScale = new Vector3(cell.transform.localScale.x*agentRadius,
				cell.transform.localScale.y*agentRadius, cell.transform.localScale.z*agentRadius);
			
			for (float i = 0; i < terrain.terrainData.size.x; i = i + agentRadius*2)
            {
				for (float j = 0; j < terrain.terrainData.size.z; j = j + agentRadius*2)
                {
                    //instantiante a new cell
					GameObject newCell = Instantiate(cell, new Vector3(newPosition.x + i, newPosition.y, newPosition.z + j), Quaternion.identity) as GameObject;
                    //change his name
                    newCell.GetComponent<CellControllerBase>().cellName = newCell.name = "cell" + i + "-" + j;
                    //change scale
                    newCell.transform.localScale = newScale;
                }
            }

            //just to see how many cells were generated
            GameObject[] allCells = GameObject.FindGameObjectsWithTag("Cell");
            //Debug.Log(allCells.Length);

            //lets set the qntAuxins for each cell according the density estimation
            float densityToQnt = PORC_QTD_Marcacoes;

            densityToQnt *= 2f / (2.0f * auxinRadius);
            densityToQnt *= 2f / (2.0f * auxinRadius);

            qntAuxins = (int)Mathf.Floor(densityToQnt);
            //Debug.Log(qntAuxins);

            //for each cell, we generate his auxins
            for (int c = 0; c < allCells.Length; c++) {

                //Dart throwing auxins
                //use this flag to break the loop if it is taking too long (maybe there is no more space)
                int flag = 0;
                for (int i = 0; i < qntAuxins; i++)
                {
                    float x = Random.Range(allCells[c].transform.position.x - 0.99f, allCells[c].transform.position.x + 0.99f);
                    float z = Random.Range(allCells[c].transform.position.z - 0.99f, allCells[c].transform.position.z + 0.99f);

                    //see if there are auxins in this radius. if not, instantiante
                    List<AuxinControllerBase> allAuxinsInCell = allCells[c].GetComponent<CellControllerBase>().GetAuxins();
                    bool canIInstantiante = true;
                    for (int j = 0; j < allAuxinsInCell.Count; j++) {
                        float distanceAA = Vector3.Distance(new Vector3(x, 0f, z), allAuxinsInCell[j].position);
                        
                        //if it is too near, i cant instantiante. found one, so can Break
                        if(distanceAA < auxinRadius)
                        {
                            canIInstantiante = false;
                            break;
                        }
                    }

                    //if i have found no auxin, i still need to check if is there obstacles on the way
                    if (canIInstantiante)
                    {
                        //sphere collider to try to find the obstacles
                        Collider[] hitColliders = Physics.OverlapSphere(new Vector3(x, 0f, z), auxinRadius);
                        {
                            //if found some
                            if(hitColliders.Length > 0)
                            {
                                //for each of them, verify if it is an Obstacle. If it is, i cannot instantiate
                                for(int s = 0; s < hitColliders.Length; s++)
                                {
                                    if(hitColliders[s].gameObject.tag == "Obstacle")
                                    {
                                        canIInstantiante = false;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    //canIInstantiante???
                    if (canIInstantiante)
                    {
                        AuxinControllerBase newAuxin = new AuxinControllerBase();
                        //change his name
                        newAuxin.name = "auxin" + c + "-" + i;
                        //this auxin is from this cell
                        newAuxin.SetCell(allCells[c]);
                        //set position
                        newAuxin.position = new Vector3(x, 0f, z);

                        //add this auxin to this cell
                        allCells[c].GetComponent<CellControllerBase>().AddAuxin(newAuxin);

                        //reset the flag
                        flag = 0;
                    }
                    else
                    {
                        //else, try again
                        flag++;
                        i--;
                    }

                    //if flag is above qntAuxins (*2 to have some more), break;
                    if (flag > qntAuxins*2)
                    {
                        //reset the flag
                        flag = 0;
                        break;
                    }
                }
            }

            //to avoid a freeze
            int doNotFreeze = 0;
            //instantiate qntAgents Agents
            for (int i = 0; i < qntAgents; i++)
            {
                //if we are not finding space to set the agent, lets update the maxZ position to try again
                if (doNotFreeze > qntAgents) {
                    doNotFreeze = 0;
                    spawnPositionZ += 2;
                }

                //default
                //scenarioType 0
                //sort out a cell
                float x = (int)Random.Range(3f, spawnPositionX);
                float z = (int)Random.Range(3f, spawnPositionZ);
                //need to me uneven, for the cells are too
				while (x % 2 == 0 || z % 2 == 0) {
                    x = (int)Random.Range(1f, spawnPositionX);
                    z = (int)Random.Range(1f, spawnPositionZ);
                }
				//Debug.Log (x+"--"+z);
                GameObject[] allGoals = GameObject.FindGameObjectsWithTag("Goal");
                GameObject choosen = new GameObject();
                float menorDist = float.PositiveInfinity;
                foreach (GameObject g in allGoals)
                {
                    float dist = Vector3.Distance(new Vector3(x, 0, z), g.transform.position);
                    if (dist < menorDist)
                    {
                        menorDist = dist;
                        choosen = g;

                    }
                }
                GameObject thisGoal = choosen;

                if (scenarioType == 1)
                {
                    //scenarioType 1
                    //half agents, so
                    if(i%2 != 0)
                    {
                        //z size = 20
                        z = (int)Random.Range(terrain.terrainData.size.z, terrain.terrainData.size.z - spawnPositionZ);
                        //need to me uneven, for the cells are too
                        while (z % 2 == 0)
                        {
                            z = (int)Random.Range(terrain.terrainData.size.z, terrain.terrainData.size.z - spawnPositionZ);
                        }
                        thisGoal = allGoals[1];
                    }
                }

                //find the cell in x - z coords, using his name
                int nameX = (int)x - 1;
                int nameZ = (int)z - 1;
                GameObject foundCell = CellControllerBase.GetCellByName("cell" + nameX + "-" + nameZ);
                //generate the agent position
                x = Random.Range(foundCell.transform.position.x - 1f, foundCell.transform.position.x + 1f);
                z = Random.Range(foundCell.transform.position.z  , foundCell.transform.position.z + 5f);

                //see if there are agents in this radius. if not, instantiante
                Collider[] hitColliders = Physics.OverlapSphere(new Vector3(x, 0, z), 0.5f);
                //here we have all colliders hit, where we can have auxins too. So, lets see which of these are Player
                int pCollider = 0;
                for (int j = 0; j < hitColliders.Length; j++)
                {
                    if (hitColliders[j].gameObject.tag == "Player")
                    {
                        //if we find one, it is enough. Break!
                        pCollider++;
                        break;
                    }
                }

                //if found a player in the radius, do not instantiante. try again
                if (pCollider > 0)
                {
                    //try again
                    i--;
                    doNotFreeze++;
                    continue;
                }
                else
                {
                    GameObject newAgent = Instantiate(agent, new Vector3(x, 0f, z), Quaternion.identity) as GameObject;
                    //change his name
                    newAgent.name = "agent" + i;
                    //random agent color
                    newAgent.GetComponent<AgentControllerBase>().SetColor(new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f)));
                    //agent cell
                    newAgent.GetComponent<AgentControllerBase>().SetCell(foundCell);
					//agent radius
					newAgent.GetComponent<AgentControllerBase>().agentRadius = agentRadius;

                    //if lane scene, black and white
                    if (scenarioType == 1) {
                        //scenarioType 1
                        //half agents, so
                        if (i % 2 != 0)
                        {
                            newAgent.GetComponent<AgentControllerBase>().SetColor(new Color(0f, 0f, 0f));
                        }
                        else
                        {
                            newAgent.GetComponent<AgentControllerBase>().SetColor(new Color(1f, 1f, 1f));
                        }
                    }

                    newAgent.GetComponent<MeshRenderer>().material.color = newAgent.GetComponent<AgentControllerBase>().GetColor();

                    //agent goal
                    newAgent.GetComponent<AgentControllerBase>().go = thisGoal;
                }
            }
        }
        Debug.Log("Termino");
	}

    void Start() {
        //all ready to go. If saveConfigFile is checked, save this auxin/agents config in a xml file
        if (saveConfigFile)
        {
            SaveConfigFile();
        }

		//open exit file to save info each frame
		//exit file
		exitFile = File.CreateText(Application.dataPath + "/" + exitFilename);
        Debug.Log("Acabo");
    }
	
	// Update is called once per frame
	void Update () {
        //reset auxins
        string s = framecount.ToString();
        if (s.Length == 1) ScreenCapture.CaptureScreenshot("I:\\DiogoMuller\\Thriliriel-navmeshy-a511020bb077\\Frames\\frame000" + framecount + ".png");
        if (s.Length == 2) ScreenCapture.CaptureScreenshot("I:\\DiogoMuller\\Thriliriel-navmeshy-a511020bb077\\Frames\\frame00" + framecount + ".png");
        if (s.Length == 3) ScreenCapture.CaptureScreenshot("I:\\DiogoMuller\\Thriliriel-navmeshy-a511020bb077\\Frames\\frame0" + framecount + ".png");
        if (s.Length == 4) ScreenCapture.CaptureScreenshot("I:\\DiogoMuller\\Thriliriel-navmeshy-a511020bb077\\Frames\\frame" + framecount + ".png");
        framecount++;
        //Debug.Log("Update");
		GameObject[] allCells = GameObject.FindGameObjectsWithTag("Cell");
		for (int i = 0; i < allCells.Length; i++) {
            List<AuxinControllerBase> allAuxins = allCells[i].GetComponent<CellControllerBase>().GetAuxins();
            for (int j = 0; j < allAuxins.Count; j++)
            {
                allAuxins[j].ResetAuxin();
            }
		}

		//find nearest auxins for each agent
		for (int i = 0; i < qntAgents; i++) {
            //first, lets see if the agent is still in the scene
            bool destroyed = false;
            for (int j = 0; j < agentsDestroyed.Count; j++)
            {
                if (agentsDestroyed[j] == i) destroyed = true;
            }

            //if he is
            if (!destroyed)
            {
                GameObject agentI = GameObject.Find("agent" + i);
                //find all auxins near him (Voronoi Diagram)
                agentI.GetComponent<AgentControllerBase>().FindNearAuxins();
            }
		}

		/*to find where the agent must move, we need to get the vectors from the agent to each auxin he has, and compare with 
		the vector from agent to goal, generating a angle which must lie between 0 (best case) and 180 (worst case)
        The calculation formula was taken from the Bicho´s mastery tesis and from Paravisi algorithm, all included
        in AgentControllerBase.
        */
        //agents Goal

        /*for each agent, we:
        1 - verify if he is in the scene. If he is...
        2 - find him 
        3 - for each auxin near him, find the distance vector between it and the agent
        4 - calculate the movement vector (CalculaDirecaoM())
        5 - calculate speed vector (CalculaVelocidade())
        6 - walk (Caminhe())
        7 - verify if the agent has reached the goal. If so, destroy it //TODO: try to find a better value/way
        */
        for (int i = 0; i < qntAgents; i++) {

            //verify if agent is not destroyed
            bool destroyed = false;
            for (int j = 0; j < agentsDestroyed.Count; j++)
            {
                if (agentsDestroyed[j] == i) destroyed = true;
            }

            if (!destroyed)
            {
                //find the agent
                GameObject agentI = GameObject.Find("agent" + i);
                GameObject goal = agentI.GetComponent<AgentControllerBase>().go;
                List<AuxinControllerBase> agentAuxins = agentI.GetComponent<AgentControllerBase>().GetAuxins();
                //Debug.Log(agentAuxins.Count);
                //vector for each auxin
                for (int j = 0; j < agentAuxins.Count; j++)
                {
                    //add the distance vector between it and the agent
                    agentI.GetComponent<AgentControllerBase>().vetorDistRelacaoMarcacao.Add(agentAuxins[j].position - agentI.transform.position);
                    
                    //just draw the little spider legs xD
                    Debug.DrawLine(agentAuxins[j].position, agentI.transform.position);
                }

                //calculate the movement vector
                agentI.GetComponent<AgentControllerBase>().CalculaDirecaoM();
                //calculate speed vector
                agentI.GetComponent<AgentControllerBase>().CalculaVelocidade();
                //walk
                agentI.GetComponent<AgentControllerBase>().Caminhe();
                
                //verify agent position, in relation to the goal.
                //if the distance between them is less than 1 (arbitrary, maybe authors have a better solution), he arrived. Destroy it so
                float dist = Vector3.Distance(goal.transform.position, agentI.transform.position);
                if (dist < agentI.GetComponent<AgentControllerBase>().agentRadius)
                {
                    agentsDestroyed.Add(i);
                    Destroy(agentI);
                }
            }
        }

		//write the exit file
		SaveExitFile();
	}

	//draw obstacles on the scene
	protected void DrawObstacles(){
		//draw a rectangle
		GameObject go = new GameObject();
		go.AddComponent<MeshFilter>();
		go.AddComponent<MeshRenderer>();
		MeshFilter mf = go.GetComponent< MeshFilter > ();
		var mesh = new Mesh();
		mf.mesh = mesh;

		Vector3[] vertices = new Vector3[4];

		vertices[0] = new Vector3(5, 0, 10);
		vertices[1] = new Vector3(15, 0, 10);
		vertices[2] = new Vector3(5, 0, 13);
		vertices[3] = new Vector3(15, 0, 13);

		mesh.vertices = vertices;

		int[] tri = new int[6];

		tri[0] = 0;
		tri[1] = 2;
		tri[2] = 1;

		tri[3] = 2;
		tri[4] = 3;
		tri[5] = 1;

		mesh.triangles = tri;

		go.AddComponent<BoxCollider>();
		//go.GetComponent<BoxCollider>().isTrigger = true;
		go.tag = "Obstacle";
		go.name = "Obstacle";
        //nav mesh obstacle
        go.AddComponent<UnityEngine.AI.NavMeshObstacle>();
        go.GetComponent<UnityEngine.AI.NavMeshObstacle>().carving = true;
        go.GetComponent<UnityEngine.AI.NavMeshObstacle>().carveOnlyStationary = false;
        go.GetComponent<UnityEngine.AI.NavMeshObstacle>().size = new Vector3(go.GetComponent<UnityEngine.AI.NavMeshObstacle>().size.x, 1f, go.GetComponent<UnityEngine.AI.NavMeshObstacle>().size.z);

        //draw a pentagon
        go = new GameObject();
		go.AddComponent<MeshFilter>();
		go.AddComponent<MeshRenderer>();
		mf = go.GetComponent< MeshFilter > ();
		mesh = new Mesh();
		mf.mesh = mesh;

		vertices = new Vector3[5];

		vertices[0] = new Vector3(20, 0, 15);
		vertices[1] = new Vector3(18, 0, 18);
		vertices[2] = new Vector3(22, 0, 20);
		vertices[3] = new Vector3(26, 0, 18);
		vertices[4] = new Vector3(22, 0, 15);

		mesh.vertices = vertices;

		tri = new int[9];

		tri[0] = 0;
		tri[1] = 1;
		tri[2] = 2;

		tri[3] = 2;
		tri[4] = 3;
		tri[5] = 4;

		tri[6] = 2;
		tri[7] = 4;
		tri[8] = 0;

		mesh.triangles = tri;

		go.AddComponent<BoxCollider>();
		go.GetComponent<BoxCollider>().isTrigger = true;
		go.tag = "Obstacle";
		go.name = "Obstacle";
        //nav mesh obstacle
        go.AddComponent<UnityEngine.AI.NavMeshObstacle>();
        go.GetComponent<UnityEngine.AI.NavMeshObstacle>().carving = true;
        go.GetComponent<UnityEngine.AI.NavMeshObstacle>().carveOnlyStationary = false;
        go.GetComponent<UnityEngine.AI.NavMeshObstacle>().size = new Vector3(go.GetComponent<UnityEngine.AI.NavMeshObstacle>().size.x, 1f, go.GetComponent<UnityEngine.AI.NavMeshObstacle>().size.z);
    }

    //save a csv config file
    protected void SaveConfigFile() {
        //config file
        var file = File.CreateText(Application.dataPath + "/" + configFilename);
        //obstacles file
        var fileObstacles = File.CreateText(Application.dataPath + "/" + obstaclesFilename);

		//first, we save the terrain dimensions
		Terrain terrain = GameObject.Find("Terrain").GetComponent<Terrain>();
		file.WriteLine("terrainSize:" + terrain.terrainData.size.x + "," + terrain.terrainData.size.z);

		//then, camera position and height
		GameObject camera = GameObject.Find("Camera");
		file.WriteLine("camera:" + camera.transform.position.x + "," + camera.transform.position.y + "," +
			camera.transform.position.z + "," + camera.GetComponent<Camera>().orthographicSize);

        List<AuxinControllerBase> allAuxins = new List<AuxinControllerBase>();

        //get cells info
        GameObject[] allCells = GameObject.FindGameObjectsWithTag("Cell");
        if (allCells.Length > 0)
        {
            //each line: name, positionx, positiony, positionz
            //separated with ;

            file.WriteLine("qntCells:" + allCells.Length);
            //for each auxin
            for (int i = 0; i < allCells.Length; i++)
            {
                file.WriteLine(allCells[i].name + ";" + allCells[i].transform.position.x + ";" + allCells[i].transform.position.y +
                    ";" + allCells[i].transform.position.z);

                //add all cell auxins to write later
                List<AuxinControllerBase> allCellAuxins = allCells[i].GetComponent<CellControllerBase>().GetAuxins();
                for(int j = 0; j < allCellAuxins.Count; j++)
                {
                    //Debug.Log(allCellAuxins[j].name+" -- "+ allCellAuxins[j].position);
                    allAuxins.Add(allCellAuxins[j]);
                }
            }
        }

        //get auxins info
        if (allAuxins.Count > 0) {
            //each line: name, positionx, positiony, positionz, auxinRadius, cell
            //separated with ;

            file.WriteLine("qntAuxins:"+allAuxins.Count);
            //for each auxin
            for (int i = 0; i < allAuxins.Count; i++) {
                file.WriteLine(allAuxins[i].name+";"+allAuxins[i].position.x+";"+ allAuxins[i].position.y+
                    ";"+ allAuxins[i].position.z+";"+auxinRadius+";"+allAuxins[i].GetCell().name);
            }
        }

        //get agents info
        GameObject[] allAgents = GameObject.FindGameObjectsWithTag("Player");
        if (allAgents.Length > 0)
        {
            //each line: name, radius, maxSpeed, color, positionx, positiony, positionz, goal object name, cell name
            //separated with ;

            file.WriteLine("qntAgents:" + allAgents.Length);
            //for each agent
            for (int i = 0; i < allAgents.Length; i++)
            {
                file.WriteLine(allAgents[i].name + ";" + allAgents[i].GetComponent<AgentControllerBase>().agentRadius + ";" + allAgents[i].GetComponent<AgentControllerBase>().maxSpeed + ";" 
                    + allAgents[i].GetComponent<AgentControllerBase>().GetColor() + ";" + 
                    allAgents[i].transform.position.x + ";" + allAgents[i].transform.position.y + ";" + allAgents[i].transform.position.z + ";" + allAgents[i].GetComponent<AgentControllerBase>().go.name
                    + ";" + allAgents[i].GetComponent<AgentControllerBase>().GetCell().name);
            }
        }

        file.Close();

        //get obstacles info
        GameObject[] allObstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        if (allObstacles.Length > 0)
        {
            //separated with ;
            fileObstacles.WriteLine("qntObstacles:" + allObstacles.Length);
            //for each obstacle
            for (int i = 0; i < allObstacles.Length; i++)
            {
                //new line for the obstacle name
                fileObstacles.WriteLine("\n"+allObstacles[i].name);
                //new line for the qnt vertices
                Vector3[] vertices = allObstacles[i].GetComponent<MeshFilter>().mesh.vertices;
                fileObstacles.WriteLine("qntVertices:"+vertices.Length);
                //for each vertice, one new line
                for(int j = 0; j < vertices.Length; j++)
                {
                    fileObstacles.WriteLine(vertices[j].x+";"+ vertices[j].y+";"+ vertices[j].z);
                }

                //new line for the qnt triangles
                int[] triangles = allObstacles[i].GetComponent<MeshFilter>().mesh.triangles;
                fileObstacles.WriteLine("qntTriangles:" + triangles.Length);
                //for each triangle, one new line
                for (int j = 0; j < triangles.Length; j++)
                {
                    fileObstacles.WriteLine(triangles[j]);
                }
            }
        }

        fileObstacles.Close();
    }

    //load a csv config file
    protected void LoadConfigFile() {
        string line;

        // Create a new StreamReader, tell it which file to read and what encoding the file
        StreamReader theReader = new StreamReader(Application.dataPath + "/" + obstaclesFilename, System.Text.Encoding.Default);

        using (theReader)
        {
            int lineCount = 1;
            int qntObstacles = 0;
            int qntVertices = 0;
            int qntTriangles = 0;
			int controlVertice = 0;
			int controlTriangle = 0;
            Vector3[] vertices = new Vector3[qntVertices];
            int[] triangles = new int[qntTriangles];

            do {
                line = theReader.ReadLine();

                if (line != null && line != "")
                {
                    //in the first line, we have the qntObstacles to instantiante
                    if (lineCount == 1)
                    {
                        string[] entries = line.Split(':');
                        qntObstacles = System.Int32.Parse(entries[1]);
                    }
                    //else, if the line has "Obstacle", it is the name. Here starts a new obstacle, so we activate activeObstacle
                    else if (line == "Obstacle")
                    {
						vertices = null;
						triangles = null;
						controlVertice = 0;
						controlTriangle = 0;
                    }
                    //else, if has qntVertices, starts to list the vertices
                    else if (line.Contains("qntVertices"))
                    {
                        string[] entries = line.Split(':');
                        qntVertices = System.Int32.Parse(entries[1]);
						vertices = new Vector3[qntVertices];
                    }
                    //else, if has qntTraingles, starts to list the triangles
                    else if (line.Contains("qntTriangles"))
                    {
                        string[] entries = line.Split(':');
                        qntTriangles = System.Int32.Parse(entries[1]);
						triangles = new int[qntTriangles];
                    }
                    else {
                        //else, we check if the value has ;. If so, it is a vector3. Otherwise, it is the triangle values
						string[] entries = line.Split(';');
						if(entries.Length > 1){
							vertices[controlVertice] = new Vector3(System.Convert.ToSingle(entries[0]), System.Convert.ToSingle(entries[1]), System.Convert.ToSingle(entries[2]));
							controlVertice++;
						}else{
                            triangles[controlTriangle] = System.Int32.Parse(entries[0]);
                            controlTriangle++;

                            //if it is the last one, we create the Object
                            if (controlTriangle >= qntTriangles) {
                                GameObject go = new GameObject();
                                go.AddComponent<MeshFilter>();
                                go.AddComponent<MeshRenderer>();
                                MeshFilter mf = go.GetComponent<MeshFilter>();
                                var mesh = new Mesh();
                                mf.mesh = mesh;
                                mesh.vertices = vertices;
                                mesh.triangles = triangles;

                                go.AddComponent<BoxCollider>();
                                go.GetComponent<BoxCollider>().isTrigger = true;
                                go.tag = "Obstacle";
                                go.name = "Obstacle";
                                //nav mesh obstacle
                                go.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                                go.GetComponent<UnityEngine.AI.NavMeshObstacle>().carving = true;
                                go.GetComponent<UnityEngine.AI.NavMeshObstacle>().carveOnlyStationary = false;
                                go.GetComponent<UnityEngine.AI.NavMeshObstacle>().size = new Vector3(go.GetComponent<UnityEngine.AI.NavMeshObstacle>().size.x, 1f, go.GetComponent<UnityEngine.AI.NavMeshObstacle>().size.z);
                            }
                        }
                    }
                }
                lineCount++;
            } while (line != null);
        }
        // Done reading, close the reader and return true to broadcast success    
        theReader.Close();

        int qntCells = 0;
        // Create a new StreamReader, tell it which file to read and what encoding the file
        theReader = new StreamReader(Application.dataPath + "/" + configFilename, System.Text.Encoding.Default);
		Terrain terrain = GameObject.Find ("Terrain").GetComponent<Terrain> ();
		GameObject camera = GameObject.Find ("Camera");

        using (theReader)
        {
            int lineCount = 1;
            // While there's lines left in the text file, do this:
            do
            {
                line = theReader.ReadLine();

                if (line != null)
                {
					//in first line, we have the terrain size
					if (lineCount == 1)
					{
						string[] entries = line.Split(':');
						entries = entries[1].Split(',');

						scenarioSizeX = System.Convert.ToSingle(entries[0]);
						scenarioSizeZ = System.Convert.ToSingle(entries[1]);

						terrain.terrainData.size = new Vector3(scenarioSizeX, terrain.terrainData.size.y, scenarioSizeZ);
					}
					//in second line, we have the camera position
					else if (lineCount == 2)
					{
						string[] entries = line.Split(':');
						entries = entries[1].Split(',');
						camera.transform.position = new Vector3(System.Convert.ToSingle(entries[0]),
							System.Convert.ToSingle(entries[1]), System.Convert.ToSingle(entries[2]));
						camera.GetComponent<Camera>().orthographicSize = System.Convert.ToSingle(entries[3]);
					}
                    //in the third line, we have the qntCells to instantiante
                    else if (lineCount == 3)
                    {
                        string[] entries = line.Split(':');
                        qntCells = System.Int32.Parse(entries[1]);
                    }
                    //else, if we are in the qntCells+4 line, we have the qntAuxins to instantiante
                    else if (lineCount == qntCells + 4)
                    {
                        string[] entries = line.Split(':');
                        qntAuxins = System.Int32.Parse(entries[1]);
                    }
                    //else, if we are in the qntCells+qntAuxins+5 line, it is the qntAgents to instantiate
                    else if (lineCount == qntCells + qntAuxins + 5)
                    {
                        string[] entries = line.Split(':');
                        qntAgents = System.Int32.Parse(entries[1]);
                    }
                    else
                    {
                        //while we are til qntCells+3 line, we have cells. After that, we have auxins and then, agents
                        if (lineCount <= qntCells + 3)
                        {
                            string[] entries = line.Split(';');
                            
                            if (entries.Length > 0)
                            {
                                GameObject newCell = Instantiate(cell, new Vector3(System.Convert.ToSingle(entries[1]), System.Convert.ToSingle(entries[2]), System.Convert.ToSingle(entries[3])),
                                    Quaternion.identity) as GameObject;
                                //change his name
                                newCell.name = entries[0];
                            }
                        }
                        else if (lineCount <= qntCells + qntAuxins + 4)
                        {
                            string[] entries = line.Split(';');
                            if (entries.Length > 0)
                            {
                                //find his cell
                                GameObject hisCell = GameObject.Find(entries[5]);

                                AuxinControllerBase newAuxin = new AuxinControllerBase();
                                //change his name
                                newAuxin.name = entries[0];
                                //this auxin is from this cell
                                newAuxin.SetCell(hisCell);
                                //set position
                                newAuxin.position = new Vector3(System.Convert.ToSingle(entries[1]), System.Convert.ToSingle(entries[2]), System.Convert.ToSingle(entries[3]));
                                //alter auxinRadius
                                auxinRadius = System.Convert.ToSingle(entries[4]);
                                //add this auxin to this cell
                                hisCell.GetComponent<CellControllerBase>().AddAuxin(newAuxin);
                            }
                        }
                        else
                        {
                            string[] entries = line.Split(';');
                            if (entries.Length > 0)
                            {
                                if (lineCount <= qntAuxins + 5 + qntAgents + qntCells)
                                {
                                    GameObject newAgent = Instantiate(agent, new Vector3(System.Convert.ToSingle(entries[4]),
                                    System.Convert.ToSingle(entries[5]), System.Convert.ToSingle(entries[6])),
                                    Quaternion.identity) as GameObject;
                                    //change his name
                                    newAgent.name = entries[0];
                                    //change his radius
                                    newAgent.GetComponent<AgentControllerBase>().agentRadius = System.Convert.ToSingle(entries[1]);
                                    //change his maxSpeed
                                    newAgent.GetComponent<AgentControllerBase>().maxSpeed = System.Convert.ToSingle(entries[2]);
                                    //change his color
                                    //new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f))
                                    string[] temp = entries[3].Split('(');
                                    temp = temp[1].Split(')');
                                    temp = temp[0].Split(',');
                                    newAgent.GetComponent<AgentControllerBase>().SetColor(
                                        new Color(System.Convert.ToSingle(temp[0]), System.Convert.ToSingle(temp[1]),
                                        System.Convert.ToSingle(temp[2])));
                                    newAgent.GetComponent<MeshRenderer>().material.color = newAgent.GetComponent<AgentControllerBase>().GetColor();
                                    //goal
                                    newAgent.GetComponent<AgentControllerBase>().go = GameObject.Find(entries[7]);
                                    //cell
                                    GameObject theCell = GameObject.Find(entries[8]);
                                    newAgent.GetComponent<AgentControllerBase>().SetCell(theCell);
									//agent radius
									newAgent.GetComponent<AgentControllerBase>().agentRadius = agentRadius;
                                }
                            }
                        }
                    }
                }
                lineCount++;
            }
            while (line != null);
            // Done reading, close the reader and return true to broadcast success    
            theReader.Close();
        }

        //just to see how many auxins were generated
        GameObject[] qnt = GameObject.FindGameObjectsWithTag("Auxin");
        Debug.Log(qnt.Length);
    }

	//save a csv exit file, with positions of all agents in function of time
	protected void SaveExitFile() {
		//get agents info
		GameObject[] allAgents = GameObject.FindGameObjectsWithTag("Player");
		if (allAgents.Length > 0)
		{
			//each line: frame, agents name, positionx, positiony, positionz, goal object name, cell name
			//separated with ;
			//for each agent
			for (int i = 0; i < allAgents.Length; i++)
			{
				exitFile.WriteLine(Time.frameCount + ";" + allAgents[i].name + ";" + allAgents[i].transform.position.x + ";" + 
					allAgents[i].transform.position.y + ";" + allAgents[i].transform.position.z + ";" + 
					allAgents[i].GetComponent<AgentControllerBase>().go.name + ";" + 
					allAgents[i].GetComponent<AgentControllerBase>().GetCell().name);
			}
		}
	}
}

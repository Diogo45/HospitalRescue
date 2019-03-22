using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.AI;
using UnityEngine.Profiling;

//to build nav mesh at runtime
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    private const float TIME_CONST = 1.0f / 30.0f;
    public bool gravarVideo;

    public int QTD_DEPENDENTES;
    public int FAMILY_PERCENTAGE;

    public int QTD_ALTRUISTAS;
    public int QTD_FUNCIONARIOS;
    public int TRAINED_PERCENTAGE;
    public int HOSP_SIZE;
     
    private int qtdAgentesTotal;

    public int QTD_TOTAL_BEDS;
    public GameObject Auxin;
    List<GameObject> dependentes;
    List<GameObject> altruistas;
    List<GameObject> trained;
    //scenario X
    //LIMITATION: this value need to be even. Odd values may generate error
    public float scenarioSizeX;
    //scenario Z
    //LIMITATION: this value need to be even. Odd values may generate error
    public float scenarioSizeZ;
    //agent prefab
    public GameObject agent;
    private List<GameObject> beds;
    //agent radius
    public float agentRadius;
    //cell prefab
    public GameObject cell;
    //qnt of agents in the scene
    
    //keep the agents already destroyed
    //List<GameObject> agentsDestroyed = new List<GameObject>();
    private float dependentesDestruidos = 0f;
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
    //max agent spawn x
    private int spawnPositionX;
    //max agent spawn z
    private int spawnPositionZ;
    //density
    private float PORC_QTD_Marcacoes = 0.65f;
    //qnt of auxins on the ground
    private int qntAuxins;
    //exit file
    public GameObject group;
    GameObject[] allcells;
    private int framecount = 0;
    private float timeCount = 0f;
    private int savedCount = 0;
    private List<float> lstTime;
    private List<int> lstSaved;

    //parte do conrado
    private int frames;
    public static int count_sims = 4;
    //fim da parte do conrado

    List<GameObject> everyone = new List<GameObject>();

    //private GameObject Wait;

    /*
    private StreamWriter exitFile;
   
    private bool _havmesh = true;
    int framecount = 0;
   
    GameObject normalEvent;
    private int inPenAgents = 0;
    private static List<List<GameObject>> Sections;
    */
    //on destroy application, close the file
    void OnDestroy()
    {
        //exitFile.Close();
        //Debug.Log(inPenAgents);

    }
    //CRUSHING


    //CRUSHING
    //public static List<List<GameObject>> GetSections()
    //{
    //return Sections;
    //}

    void InicializacaoCarregados()
    {
        //Wait = GameObject.Find("Wait");
        beds = new List<GameObject>();
        for (int i = 0; i < QTD_TOTAL_BEDS; i++)
        {
            GameObject temp = GameObject.Find("Bed (" + i + ")");
            if (temp) beds.Add(temp);
        }

        qtdAgentesTotal = QTD_DEPENDENTES + QTD_ALTRUISTAS + QTD_FUNCIONARIOS;
    }


    private int AUX_COUNT;
    void AltruistasAtributos(GameObject newAgent, GameObject thisGoal)
    {
        newAgent.GetComponent<AgentController>().ConfiguraAltruismo(1.0f, 0.0f, 0.0f, 1 + Random.Range(0,1.3f), true, 40 + Random.Range(0, 70));
        newAgent.tag = "Altruista";
        newAgent.GetComponent<AgentController>().SetColor(new Color(0f, 0f, 1f));
        newAgent.transform.parent = GameObject.Find("Agents").transform;
        newAgent.GetComponent<MeshRenderer>().material.color = newAgent.GetComponent<AgentController>().GetColor();
        newAgent.GetComponent<AgentController>().go = thisGoal;
        newAgent.GetComponent<AgentController>().bkpGo = GameObject.Find("Goal");
        if (AUX_COUNT > 0)
        {
           
            newAgent.GetComponent<AgentController>().EMPLOYEE = true;
            if (Random.Range(1, 10) <= TRAINED_PERCENTAGE)
            {
                trained.Add(newAgent);
                newAgent.GetComponent<AgentController>().TRAINED = true;
            }
            float rand = Random.Range(0, 99);
            if (rand <= 10) newAgent.GetComponent<AgentController>().FunctionType = 1;
            if (rand > 10 && rand <= 25) newAgent.GetComponent<AgentController>().FunctionType = 2;
            if (rand > 25 && rand <= 50) newAgent.GetComponent<AgentController>().FunctionType = 3;
            if (rand > 50 && rand <= 75) newAgent.GetComponent<AgentController>().FunctionType = 4;
            if (rand > 75) newAgent.GetComponent<AgentController>().FunctionType = 5;
            AUX_COUNT--;
        }
    }

    void InstanciaDependente(GameObject foundCell, int i, float x, float z, GameObject thisGoal)
    {
        Vector3 pos = new Vector3(x, 0.0f, z);
        //Vector3 pos = new Vector3();
        if (beds.Count > 0)
        {
            //Debug.Log("AAAAAAAAAAAA");
            int rand = Random.Range(0, beds.Count - 1);
            GameObject temp2 = beds[rand];
            //Debug.Log(rand);
            pos = temp2.transform.position;
            pos.x = pos.x - 0.2f;
            beds.Remove(temp2);
        }


        GameObject dependente = Instantiate(agent, new Vector3(pos.x, 0f, pos.z), Quaternion.identity) as GameObject;

        dependente.tag = "Dependente";
        dependente.GetComponent<AgentController>().ConfiguraAltruismo(1.0f, 0.8f + Random.Range(0.0f, 0.2f), 0.0f, 1 + Random.Range(0, 1.3f), true, 40 + Random.Range(0, 70));
        //change his name
        dependente.name = "agent" + i;
        dependente.transform.parent = GameObject.Find("Dependents").transform;
        dependente.GetComponent<AgentController>().SetColor(new Color(1f, 0f, 0f));
        //agent cell
        dependente.GetComponent<AgentController>().SetCell(foundCell);
        //agent radius
        dependente.GetComponent<AgentController>().agentRadius = agentRadius;
        dependente.GetComponent<MeshRenderer>().material.color = dependente.GetComponent<AgentController>().GetColor();
        dependente.GetComponent<AgentController>().go = thisGoal;
        dependente.GetComponent<AgentController>().bkpGo = thisGoal;
        GameObject newGroup = Instantiate(group, dependente.transform.position, Quaternion.identity) as GameObject;
        newGroup.name = dependente.name + "'s Group";
        newGroup.transform.parent = GameObject.Find("Groups").transform;
        newGroup.GetComponent<GroupController>().SetDependente(dependente);
        dependente.GetComponent<AgentController>().group = newGroup.GetComponent<GroupController>();
        //Oitenta por cento da populacao esta em cadeiras de rodas
        if (Random.Range(1,10) <= FAMILY_PERCENTAGE)
        {
            dependente.GetComponent<AgentController>().ON_FAMILY = true;
            dependente.GetComponent<AgentController>().group.GroupType = 1;
        }
        else
        {          
            dependente.GetComponent<AgentController>().ON_WHEELCHAIR = true;
            dependente.GetComponent<AgentController>().peso /= 4;
            dependente.GetComponent<AgentController>().group.GroupType = 2;
            dependente.GetComponent<AgentController>().SetColor(new Color(0f, 0f, 0f));
            dependente.GetComponent<MeshRenderer>().material.color = dependente.GetComponent<AgentController>().GetColor();
        }

    }

    void ListaAltruistasDependentes()
    {

        altruistas = GameObject.FindGameObjectsWithTag("Altruista").ToList();
        dependentes = GameObject.FindGameObjectsWithTag("Dependente").ToList();
       
        //agentes = GameObject.FindGameObjectsWithTag("Player").ToList();
    }


    public static List<GameObject> FindClosest(GameObject g, int n, GameObject[] disponiveis)
    {
        GameObject[] nClosest = disponiveis.OrderBy(t => (t.transform.position - g.transform.position).sqrMagnitude)
                            .Take(n)   //or use .FirstOrDefault();  if you need just one
                            .ToArray();
        for (int i = 0; i < nClosest.Length; i++)
        {

            nClosest[i].GetComponent<AgentController>().estado = 2;

        }
        return nClosest.ToList<GameObject>();
    }
    /*
    Vector3[] normals;
    void OnDrawGizmos()
    {
        NavMeshTriangulation navMesh = NavMesh.CalculateTriangulation();
        Debug.Log(navMesh.indices.Length);
        Debug.Log(navMesh.vertices.Length);
        Mesh mesh = new Mesh();
        mesh.vertices = navMesh.vertices;
        mesh.triangles = navMesh.indices;
        if (normals == null || normals.Length != navMesh.vertices.Length)
        {
            normals = new Vector3[navMesh.vertices.Length];
            for (int i = 0; i < navMesh.vertices.Length; i++)
                normals[i] = Vector3.up;
        }
        mesh.normals = normals;
        Gizmos.DrawWireMesh(mesh);
    }
    */

    // Use this for initialization
    void Awake()
    {
        //conrado
        if (count_sims >= 0 && count_sims < 10 )
        {
            QTD_FUNCIONARIOS = 7;
            QTD_DEPENDENTES = 36;
            TRAINED_PERCENTAGE = 100;
        }
        /*
        if (count_sims >= 5 && count_sims < 10 && HOSP_SIZE > 1)
        {
            QTD_FUNCIONARIOS = 144;
            QTD_DEPENDENTES = 56;
            TRAINED_PERCENTAGE = 0;
        }
        if (count_sims >= 10 && count_sims < 15 && HOSP_SIZE > 2)
        {
            QTD_FUNCIONARIOS = 116;
            QTD_DEPENDENTES = 84;
            TRAINED_PERCENTAGE = 0;
        }


        if (count_sims >= 15 && count_sims < 20 )
        {
            QTD_FUNCIONARIOS = 172;
            QTD_DEPENDENTES = 28;
            TRAINED_PERCENTAGE = 2;
        }
        if (count_sims >= 20 && count_sims < 25 && HOSP_SIZE > 1)
        {
            QTD_FUNCIONARIOS = 144;
            QTD_DEPENDENTES = 56;
            TRAINED_PERCENTAGE = 2;
        }
        if (count_sims >= 25 && count_sims < 30 && HOSP_SIZE > 2)
        {
            QTD_FUNCIONARIOS = 116;
            QTD_DEPENDENTES = 84;
            TRAINED_PERCENTAGE = 2;
        }


        if (count_sims >= 30 && count_sims < 35 )
        {
            QTD_FUNCIONARIOS = 172;
            QTD_DEPENDENTES = 28;
            TRAINED_PERCENTAGE = 5;
        }
        if (count_sims >= 35 && count_sims < 40 && HOSP_SIZE > 1)
        {
            QTD_FUNCIONARIOS = 144;
            QTD_DEPENDENTES = 56;
            TRAINED_PERCENTAGE = 5;
        }
        if (count_sims >= 40 && count_sims < 45 && HOSP_SIZE > 2)
        {
            QTD_FUNCIONARIOS = 116;
            QTD_DEPENDENTES = 84;
            TRAINED_PERCENTAGE = 5;
        }


        if (count_sims >= 45 && count_sims < 50)
        {
            QTD_FUNCIONARIOS = 72;
            QTD_DEPENDENTES = 28;
            TRAINED_PERCENTAGE = 0;
        }
        if (count_sims >= 50 && count_sims < 55 && HOSP_SIZE > 1)
        {
            QTD_FUNCIONARIOS = 44;
            QTD_DEPENDENTES = 56;
            TRAINED_PERCENTAGE = 0;
        }

        if (count_sims >= 55 && count_sims < 60 && HOSP_SIZE > 2)
        {
            QTD_FUNCIONARIOS = 16;
            QTD_DEPENDENTES = 84;
            TRAINED_PERCENTAGE = 0;
        }


        if(count_sims >= 60 && count_sims < 65)
        {
            QTD_FUNCIONARIOS = 22;
            QTD_DEPENDENTES = 28;
            TRAINED_PERCENTAGE = 100;
        }
        if (count_sims >= 65 && count_sims < 70 && HOSP_SIZE > 1)
        {
            QTD_FUNCIONARIOS = 44;
            QTD_DEPENDENTES = 56;
            TRAINED_PERCENTAGE = 100;
        }
        if (count_sims >= 70 && count_sims < 75 && HOSP_SIZE > 2)
        {
            QTD_FUNCIONARIOS = 16;
            QTD_DEPENDENTES = 84;
            TRAINED_PERCENTAGE = 100;
        }


        if (count_sims >= 75 && count_sims < 80 )
        {
            QTD_FUNCIONARIOS = 72;
            QTD_DEPENDENTES = 28;
            TRAINED_PERCENTAGE = 5;
        }
        if (count_sims >= 80 && count_sims < 85 && HOSP_SIZE > 1)
        {
            QTD_FUNCIONARIOS = 44;
            QTD_DEPENDENTES = 56;
            TRAINED_PERCENTAGE = 5;
        }
        if (count_sims >= 85 && count_sims < 90 && HOSP_SIZE > 2)
        {
            QTD_FUNCIONARIOS = 16;
            QTD_DEPENDENTES = 84;
            TRAINED_PERCENTAGE = 5;
        }


        if (count_sims >= 90 && count_sims < 95)
        {
            QTD_FUNCIONARIOS = 22;
            QTD_DEPENDENTES = 28;
            TRAINED_PERCENTAGE = 0;
        }
        if (count_sims >= 95 && count_sims < 100 )
        {
            QTD_FUNCIONARIOS = 22;
            QTD_DEPENDENTES = 28;
            TRAINED_PERCENTAGE = 2;
        }
        if (count_sims >= 100 && count_sims < 105)
        {
            QTD_FUNCIONARIOS = 22;
            QTD_DEPENDENTES = 28;
            TRAINED_PERCENTAGE = 5;
        }


        if (count_sims >= 105 && count_sims < 110 )
        {
            QTD_FUNCIONARIOS = 20;
            QTD_DEPENDENTES = 20;
            TRAINED_PERCENTAGE = 0;
        }
        if (count_sims >= 110 && count_sims < 115)
        {
            QTD_FUNCIONARIOS = 20;
            QTD_DEPENDENTES = 20;
            TRAINED_PERCENTAGE = 2;
        }
        if (count_sims >= 115 && count_sims < 120)
        {
            QTD_FUNCIONARIOS = 20;
            QTD_DEPENDENTES = 20;
            TRAINED_PERCENTAGE = 5;
        }


        if (count_sims >= 120 && count_sims < 125 && HOSP_SIZE > 1)
        {
            QTD_FUNCIONARIOS = 40;
            QTD_DEPENDENTES = 40;
            TRAINED_PERCENTAGE = 0;
        }
        if (count_sims >= 125 && count_sims < 130 && HOSP_SIZE > 1)
        {
            QTD_FUNCIONARIOS = 40;
            QTD_DEPENDENTES = 40;
            TRAINED_PERCENTAGE = 2;
        }
        if (count_sims >= 130 && count_sims < 135 && HOSP_SIZE > 1)
        {
            QTD_FUNCIONARIOS = 40;
            QTD_DEPENDENTES = 40;
            TRAINED_PERCENTAGE = 5;
        }


        if (count_sims >= 135 && count_sims < 140 && HOSP_SIZE > 2)
        {
            QTD_FUNCIONARIOS = 80;
            QTD_DEPENDENTES = 80;
            TRAINED_PERCENTAGE = 0;
        }
        if (count_sims >= 140 && count_sims < 145 && HOSP_SIZE > 2)
        {
            QTD_FUNCIONARIOS = 80;
            QTD_DEPENDENTES = 80;
            TRAINED_PERCENTAGE = 2;
        }
        if (count_sims >= 145 && count_sims < 150 && HOSP_SIZE > 2)
        {
            QTD_FUNCIONARIOS = 80;
            QTD_DEPENDENTES = 80;
            TRAINED_PERCENTAGE = 5;
        }
        */
        frames = 0;


        trained = new List<GameObject>();
        AUX_COUNT = QTD_FUNCIONARIOS;
        InicializacaoCarregados();
        Terrain terrain = GameObject.Find("Terrain").GetComponent<Terrain>();
        //change terrain size according informed
        terrain.terrainData.size = new Vector3(scenarioSizeX, terrain.terrainData.size.y, scenarioSizeZ);

        //Get map size        
        spawnPositionX = (int)Mathf.Floor(terrain.terrainData.size.x - 1f);
        spawnPositionZ = (int)Mathf.Floor(terrain.terrainData.size.z - (terrain.terrainData.size.z - 72));



        //if loadConfigFile is checked, we do not generate the initial scenario. We load it from the Config.xml (or something like this) file
        if (loadConfigFile)
        {
            LoadConfigFile();

            //build the navmesh at runtime
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        }
        else
        {

            //BuildNavMesh();

            //build the navmesh at runtime
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();

            //first of all, create all cells (with this scene and this agentRadius = 1 : 150 cells)
            //since radius = 1; diameter = 2. So, iterate 2 in 2
            //if the radius varies, this 2 operations adjust the cells
            Vector3 newPosition = new Vector3(cell.transform.position.x * agentRadius,
                cell.transform.position.y * agentRadius, cell.transform.position.z * agentRadius);
            Vector3 newScale = new Vector3(cell.transform.localScale.x * agentRadius,
                cell.transform.localScale.y * agentRadius, cell.transform.localScale.z * agentRadius);

            for (float i = 0; i < terrain.terrainData.size.x; i = i + agentRadius * 2)
            {
                for (float j = 0; j < terrain.terrainData.size.z; j = j + agentRadius * 2)
                {
                    //instantiante a new cell
                    GameObject newCell = Instantiate(cell, new Vector3(newPosition.x + i, newPosition.y, newPosition.z + j), Quaternion.identity) as GameObject;
                    newCell.transform.parent = GameObject.Find("Cells").transform;


                    //change his name
                    newCell.GetComponent<CellController>().cellName = newCell.name = "cell" + i + "-" + j;
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
            for (int c = 0; c < allCells.Length; c++)
            {

                //Dart throwing auxins
                //use this flag to break the loop if it is taking too long (maybe there is no more space)
                int flag = 0;
                for (int i = 0; i < qntAuxins; i++)
                {
                    float x = Random.Range(allCells[c].transform.position.x - 0.99f, allCells[c].transform.position.x + 0.99f);
                    float z = Random.Range(allCells[c].transform.position.z - 0.99f, allCells[c].transform.position.z + 0.99f);

                    //see if there are auxins in this radius. if not, instantiante
                    List<AuxinController> allAuxinsInCell = allCells[c].GetComponent<CellController>().GetAuxins();
                    bool canIInstantiante = true;
                    for (int j = 0; j < allAuxinsInCell.Count; j++)
                    {
                        float distanceAA = Vector3.Distance(new Vector3(x, 0f, z), allAuxinsInCell[j].position);

                        //if it is too near, i cant instantiante. found one, so can Break
                        if (distanceAA < auxinRadius)
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
                            if (hitColliders.Length > 0)
                            {
                                //for each of them, verify if it is an Obstacle. If it is, i cannot instantiate
                                for (int s = 0; s < hitColliders.Length; s++)
                                {
                                    if (hitColliders[s].gameObject.tag == "Obstacle")
                                    {
                                        canIInstantiante = false;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (canIInstantiante)
                    {
                        NavMeshHit h = new NavMeshHit();
                        
                        bool NavMesh = UnityEngine.AI.NavMesh.SamplePosition(new Vector3(x, 0f, z), out h, 0.1f, 0xFFFF);
                        if (!NavMesh)
                        {
                            canIInstantiante = false;
                        }
                        
                    }

                    //canIInstantiante???
                    if (canIInstantiante)
                    {
                        AuxinController newAuxin = new AuxinController();
                        //change his name
                        newAuxin.name = "auxin" + c + "-" + i;
                        //this auxin is from this cell
                        newAuxin.SetCell(allCells[c]);
                        //set position
                        newAuxin.position = new Vector3(x, 0f, z);

                        //add this auxin to this cell
                        allCells[c].GetComponent<CellController>().AddAuxin(newAuxin);

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
                    if (flag > qntAuxins * 2)
                    {
                        //reset the flag
                        flag = 0;
                        break;
                    }
                }
            }

            //to avoid a freeze
            int doNotFreeze = 0;
            //Para saber quantos altruistas intanciar
            int cont = QTD_ALTRUISTAS + QTD_FUNCIONARIOS;
            //instantiate qtdAgentesTotal Agents
            GameObject[] goals = GameObject.FindGameObjectsWithTag("Goal");
            //normalEvent = new GameObject();
            //normalEvent.name = "Normal";
            //normalEvent.AddComponent<EventController>();
            //normalEvent.GetComponent<EventController>().EventoNormal("Normal", goals);

            for (int i = 0; i < qtdAgentesTotal; i++)
            {

                //if we are not finding space to set the agent, lets update the maxZ position to try again
                if (doNotFreeze > qtdAgentesTotal)
                {
                    doNotFreeze = 0;
                    spawnPositionZ += 2;
                }

                //default
                //scenarioType 0
                //sort out a cell
                float x = (int)Random.Range(3f, spawnPositionX);
                float z = (int)Random.Range(3f, spawnPositionZ);
                //need to me uneven, for the cells are too
                while (x % 2 == 0 || z % 2 == 0)
                {
                    x = (int)Random.Range(1f, spawnPositionX);
                    z = (int)Random.Range(1f, spawnPositionZ);
                }
                //Debug.Log (x+"--"+z);
                GameObject[] allGoals = GameObject.FindGameObjectsWithTag("Goal");
                //Debug.Log(allGoals.Length);
                //GameObject[] allCorridors = GameObject.FindGameObjectsWithTag("Corridor");
                float menorDist = float.PositiveInfinity;
                GameObject choosen = null;
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


                //find the cell in x - z coords, using his name
                int nameX = (int)x - 1;
                int nameZ = (int)z - 1;
                GameObject foundCell = CellController.GetCellByName("cell" + nameX + "-" + nameZ);
                //generate the agent position
                x = Random.Range(foundCell.transform.position.x - 1f, foundCell.transform.position.x + 1f);
                z = Random.Range(foundCell.transform.position.z - 1f, foundCell.transform.position.z + 1f);

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

                    if (cont > 0)
                    {
                        
                        GameObject newAgent = Instantiate(agent, new Vector3(x, 0f, z), Quaternion.identity) as GameObject;
                        //change his name
                        newAgent.name = "agent" + i;
                        //random agent color
                        //newAgent.GetComponent<AgentController>().SetColor(new Color(Random.Range(0.5f, 1f), Random.Range(0.2f, 0.4f), Random.Range(0.0f, 1f)));
                        //agent cell
                        newAgent.GetComponent<AgentController>().SetCell(foundCell);
                        //agent radius
                        newAgent.GetComponent<AgentController>().agentRadius = agentRadius;

                        //1+Random.Range(0,1.3f) considera que todos são homens 
                        AltruistasAtributos(newAgent, thisGoal);
                        cont--;
                        continue;
                    }
                    if (QTD_DEPENDENTES > 0)
                    {
                        InstanciaDependente(foundCell, i, x, z, thisGoal);
                    }
                }

            }

        }
        ListaAltruistasDependentes();
        
        allcells = GameObject.FindGameObjectsWithTag("Cell");
    }



    void Start()
    {
        lstSaved = new List<int>{0};
        lstTime = new List<float>{0};
        NavMesh.pathfindingIterationsPerFrame = 100000000;

        //all ready to go. If saveConfigFile is checked, save this auxin/agents config in a xml file
        if (saveConfigFile)
        {
            SaveConfigFile();
        }
        //open exit file to save info each frame
        //exit file
        //exitFile = File.CreateText(Application.dataPath + "/" + exitFilename);

        //Inicializa as familias levam seu parente e saem da cena
        GameObject[] temp = new GameObject[QTD_DEPENDENTES];
        //Debug.Log(altruistas.Count + " "+ (QTD_ALTRUISTAS + QTD_FUNCIONARIOS));
        dependentes.CopyTo(temp);
        List<GameObject> aux = temp.ToList();
        foreach(GameObject g in altruistas)
        {
            if(!g.GetComponent<AgentController>().TRAINED)
                g.GetComponent<AgentController>().AchaDependente(aux);
            
        }


        /*
        for (int i = 0; i < temp.Length; i++)
        {
            if (temp[i].GetComponent<AgentController>().TRAINED || temp[i].GetComponent<AgentController>().EMPLOYEE) temp[i] = null;
        }
        List<GameObject> aux = temp.ToList();
        aux.RemoveAll(x => x == null);
        foreach (GameObject g in dependentes)
        {
            if (g.GetComponent<AgentController>().ON_WHEELCHAIR) continue;
            GameObject[] nClosest = aux.OrderBy(t => (g.GetComponent<AgentController>().PathDist(t)))
                     .Take(4)   //or use .FirstOrDefault();  if you need just one
                     .ToArray();

            g.GetComponent<AgentController>().group.agentesBuscando.AddRange(nClosest);
            g.GetComponent<AgentController>().group.GroupType = 1;
            foreach (GameObject o in g.GetComponent<AgentController>().group.agentesBuscando)
            {
                o.transform.position = (o.transform.position - g.transform.position).normalized * (agentRadius) + g.transform.position;
                aux.Remove(o);
                o.GetComponent<AgentController>().go = g;
                o.GetComponent<AgentController>().ON_FAMILY = true;
                o.GetComponent<AgentController>().group = g.GetComponent<AgentController>().group;
            }
        }
        GameObject[] auxEmployees = new GameObject[QTD_FUNCIONARIOS];
        trained.CopyTo(auxEmployees);
        List<GameObject> auxEmp = auxEmployees.ToList();
        //Inicializa os grupos com Altruistas nao treinados levam seu dependente e saem da cena
        foreach (GameObject g in dependentes)
        {
            if (g.GetComponent<AgentController>().ON_FAMILY) continue;
            GameObject Closest = auxEmp.OrderBy(t => (g.GetComponent<AgentController>().PathDist(t)))
                     .FirstOrDefault();   //or use .FirstOrDefault();  if you need just one
            if (Closest)
            {

                g.GetComponent<AgentController>().group.agentesBuscando.Add(Closest);
                g.GetComponent<AgentController>().group.GroupType = 2;
                auxEmp.Remove(Closest);
                Closest.GetComponent<AgentController>().go = g;
                Closest.GetComponent<AgentController>().group = g.GetComponent<AgentController>().group;
            }
           
        }
        */
    }

    // Update is called once per frame
    void Update()
    {
        Profiler.BeginSample("List ");
        everyone.AddRange(altruistas.Concat(dependentes));
        //conrado
        frames++;
        Profiler.EndSample();

        bool continua = false;
        //mudei pra &&, diogo -assinado: bruninho TA ERRADO
        if(TRAINED_PERCENTAGE > 0)
        {
            if(dependentes.Count > 0)
            {
                continua = true;
            }
        }
        else
        {
            if(altruistas.Count > 0)
            {
                continua = true;
            }
        }

        //int bla = 0;
        //foreach(GameObject g in everyone)
        //{
        //    if (g.GetComponent<AgentController>().TRAINED) bla++;
        //}
        //Debug.Log(bla);
        if (continua)
        {
            timeCount += TIME_CONST;
            if (gravarVideo)
            {
                string s = framecount.ToString();
                if (s.Length == 1) ScreenCapture.CaptureScreenshot("I:\\DiogoMuller\\Thriliriel-navmeshy-a511020bb077\\Frames\\frame000" + framecount + ".png");
                if (s.Length == 2) ScreenCapture.CaptureScreenshot("I:\\DiogoMuller\\Thriliriel-navmeshy-a511020bb077\\Frames\\frame00" + framecount + ".png");
                if (s.Length == 3) ScreenCapture.CaptureScreenshot("I:\\DiogoMuller\\Thriliriel-navmeshy-a511020bb077\\Frames\\frame0" + framecount + ".png");
                if (s.Length == 4) ScreenCapture.CaptureScreenshot("I:\\DiogoMuller\\Thriliriel-navmeshy-a511020bb077\\Frames\\frame" + framecount + ".png");
            }
            framecount++;

            //reset auxins

            Profiler.BeginSample("AuxinClear");
            for (int i = 0; i < allcells.Length; i++)
            {
                List<AuxinController> allAuxins = allcells[i].GetComponent<CellController>().GetAuxins();
                for (int j = 0; j < allAuxins.Count; j++)
                {
                    allAuxins[j].ResetAuxin();
                }
            }
            Profiler.EndSample();

            //find nearest auxins for each agent

            Profiler.BeginSample("AgentsFindAuxin");
            foreach (GameObject agentI in everyone)
            {
                
                /*//first, lets see if the agent is still in the scene
                if (agentsDestroyed.Contains(agentI))
                {
                    continue;
                }
                */
                //if he is
                //find all auxins near him (Voronoi Diagram)
                agentI.GetComponent<AgentController>().FindNearAuxins();

            }
            Profiler.EndSample();

            /*to find where the agent must move, we need to get the vectors from the agent to each auxin he has, and compare with 
            the vector from agent to goal, generating a angle which must lie between 0 (best case) and 180 (worst case)
            The calculation formula was taken from the Bicho´s mastery tesis and from Paravisi algorithm, all included
            in AgentController.
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
            Profiler.BeginSample("Agent Update");
            foreach (GameObject agentI in everyone)
            {

                //first, lets see if the agent is still in the scene
                /*
                if (agentsDestroyed.Contains(agentI))
                {
                    continue;
                }
                /*
                if (!agentI.GetComponent<AgentController>().inPen)
                {
                    if(agentI.transform.position.z > 66)
                    {
                        inPenAgents++;
                        agentI.GetComponent<AgentController>().inPen = true;
                    }
                    if (Random.Range(0f, 1f) == 0.9 && inPenAgents > 0)
                    {
                        Debug.Log(inPenAgents);
                    }
                }
                */
                //find the agent
                //GameObject agentI = GameObject.Find("agent" + i);
                GameObject goal = agentI.GetComponent<AgentController>().go;
                // float dist = Vector3.Distance(goal.transform.position, agentI.transform.position);
                Profiler.BeginSample("AgentGetAuxins");
                List<AuxinController> agentAuxins = agentI.GetComponent<AgentController>().GetAuxins();
                Profiler.EndSample();
                //vector for each auxin

                Profiler.BeginSample("DistRelMarcacao");
                for (int j = 0; j < agentAuxins.Count; j++)
                {
                    //add the distance vector between it and the agent
                    agentI.GetComponent<AgentController>().vetorDistRelacaoMarcacao.Add(agentAuxins[j].position - agentI.transform.position);

                    //just draw the little spider legs
                    Debug.DrawLine(agentAuxins[j].position, agentI.transform.position);
                }
                Profiler.EndSample();

                Profiler.BeginSample("AgentCalculations");
                //calculate the movement vector
                agentI.GetComponent<AgentController>().CalculaDirecaoM();
                //calculate speed vector
                agentI.GetComponent<AgentController>().CalculaVelocidade();
                agentI.GetComponent<AgentController>().Caminhe();

                //verify agent position, in relation to the goal.
                //if the distance between them is less than 1 (arbitrary, maybe authors have a better solution), he arrived. Destroy it so

                //if (agentI == null) Debug.Log("SSS");
                float dist = Vector3.Distance(goal.transform.position, agentI.transform.position);
                //if (agentI.tag == "Dependente") Debug.Log(dist);
                Profiler.EndSample();

                Profiler.BeginSample("DependenteDestroy");
                if (dist < GetAgentRadius(agentI) * 2 && goal.tag == "Goal" && agentI.tag == "Dependente")
                {
                    //agentsDestroyed.Add(agentI);
                    ResetGroup(agentI);
                    dependentesDestruidos++;
                    if (agentI.GetComponent<AgentController>().ON_FAMILY)
                        Debug.Log("Familia");
                    continue;


                    //Destroy(agentI);
                }
                Profiler.EndSample();

                Profiler.BeginSample("TrainedDestroy");

                //Debug.Log(dependentes.Count);

                if (agentI.GetComponent<AgentController>().TRAINED && !agentI.GetComponent<AgentController>().group)
                {


                    if (agentI.GetComponent<AgentController>().agentesNaoConsegueAjudar.Count == dependentes.Count)
                    {
                        //Debug.Log(agentI.name + " " + agentI.GetComponent<AgentController>().agentesNaoConsegueAjudar.Count + " " + dependentes.Count);

                        if (dist <= agentRadius)
                        {
                            trained.Remove(agentI);
                            //agentsDestroyed.Add(agentI);
                            Destroy(agentI);
                            altruistas.Remove(agentI);
                            continue;
                        }

                    }

                }
                Profiler.EndSample();
                Profiler.BeginSample("FuncionarioDestroy");

                if (!agentI.GetComponent<AgentController>().group && !agentI.GetComponent<AgentController>().TRAINED && goal.tag == "Goal")
                {
                    //Debug.Log("AAAA"); 
                    if (dist <= agentRadius)
                    {
                        //agentsDestroyed.Add(agentI);
                        Destroy(agentI);
                        altruistas.Remove(agentI);
                    }


                }
                Profiler.EndSample();

            }
            Profiler.EndSample();
            everyone.Clear();
            
            if (framecount - lstTime[lstTime.Count - 1] > 30)
            {
                lstTime.Add(framecount);
                lstSaved.Add(savedCount);
                /*
                string temp = "lstSaved ";
                foreach (int i in lstSaved)
                {
                    temp += i + ", "; //maybe also + '\n' to put them on their own line.
                }
                
                temp += '\n';
                Debug.Log(temp);
                temp = "lstTime ";
               
                foreach (int i in lstTime)
                {
                    temp += i + ", "; //maybe also + '\n' to put them on their own line.
                }
                
                temp += '\n';
                Debug.Log(temp);
                */
            }

            //write the exit file
            //SaveExitFile();
        }
        //conrado
        else
        {
            if(lstTime[lstTime.Count-1] != framecount)
            {
                lstTime.Add(framecount);
                lstSaved.Add(savedCount);
            }
            PrintdoDiego(lstTime, lstSaved);
            count_sims++;
            if (count_sims < 10)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            else
            {
                UnityEditor.EditorApplication.isPlaying = false;
            }
                

        }
    }

    private void PrintdoDiego(List<float> l1,List<int> l2)
    {
        string temp = "";
        var file = File.CreateText(Application.dataPath + "/TESTE_HOSP="+ HOSP_SIZE +"_DEPENDENTES="+ QTD_DEPENDENTES + "_FUNCIONARIOS=" + QTD_FUNCIONARIOS +"_TREINADOS=" + TRAINED_PERCENTAGE + "_"+ count_sims + ".txt");
        //Debug.Log(Application.dataPath);
        for (int i = 0; i < l1.Count;i++) {
             temp = temp + l1[i] + " " + l2[i] + "\n";
        }
        file.Write(temp);
        file.Close();
    }
    

    private static float GetAgentRadius(GameObject agentI)
    {
        return agentI.GetComponent<AgentController>().agentRadius;
    }

    private void ResetGroup(GameObject g)
    {
        //Debug.Log("AAAA");
        GameObject agenteI = g.GetComponent<AgentController>().group.agenteCarregando;
        if (agenteI)
        {
            if (agenteI.GetComponent<AgentController>().TRAINED)
            {
                agenteI.GetComponent<AgentController>().maxSpeed = agenteI.GetComponent<AgentController>().mSpeed;
                agenteI.GetComponent<AgentController>().initSpeed = agenteI.GetComponent<AgentController>().mSpeed;
                agenteI.GetComponent<AgentController>().group = null;
                agenteI.GetComponent<AgentController>().AchaDependente();
            }
            else
            {
                //agentsDestroyed.Add(agenteI);
                Destroy(agenteI);
                altruistas.Remove(agenteI);
            }
        }
        foreach (GameObject o in g.GetComponent<AgentController>().group.agentesCarregando)
        {
            /*
            o.GetComponent<AgentController>().GetNaoConsegueAjudar().Clear();
            o.GetComponent<AgentController>().maxSpeed = 1.3f;
           
            o.GetComponent<AgentController>().go = o.GetComponent<AgentController>().bkpGo;
            */
            o.GetComponent<AgentController>().group = null;
            //agentsDestroyed.Add(o);
            Destroy(o);
            altruistas.Remove(o);
        }
        foreach (GameObject j in altruistas)
        {
            j.GetComponent<AgentController>().atualizaDepentes(g);

        }
        dependentes.Remove(g);
        g.GetComponent<AgentController>().group.agentesCarregando.Clear();
        Destroy(g.GetComponent<AgentController>().group);
        Destroy(g.GetComponent<AgentController>().group.gameObject);
        savedCount++;
        Destroy(g);

    }

    //draw obstacles on the scene

    //save a csv config file
    protected void SaveConfigFile()
    {
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

        List<AuxinController> allAuxins = new List<AuxinController>();

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
                List<AuxinController> allCellAuxins = allCells[i].GetComponent<CellController>().GetAuxins();
                for (int j = 0; j < allCellAuxins.Count; j++)
                {
                    //Debug.Log(allCellAuxins[j].name+" -- "+ allCellAuxins[j].position);
                    allAuxins.Add(allCellAuxins[j]);
                }
            }
        }

        //get auxins info
        if (allAuxins.Count > 0)
        {
            //each line: name, positionx, positiony, positionz, auxinRadius, cell
            //separated with ;

            file.WriteLine("qntAuxins:" + allAuxins.Count);
            //for each auxin
            for (int i = 0; i < allAuxins.Count; i++)
            {
                file.WriteLine(allAuxins[i].name + ";" + allAuxins[i].position.x + ";" + allAuxins[i].position.y +
                    ";" + allAuxins[i].position.z + ";" + auxinRadius + ";" + allAuxins[i].GetCell().name);
            }
        }

        //get agents info
        GameObject[] allAgents = GameObject.FindGameObjectsWithTag("Player");
        if (allAgents.Length > 0)
        {
            //each line: name, radius, maxSpeed, color, positionx, positiony, positionz, goal object name, cell name
            //separated with ;

            file.WriteLine("qtdAgentesTotal:" + allAgents.Length);
            //for each agent
            for (int i = 0; i < allAgents.Length; i++)
            {
                file.WriteLine(allAgents[i].name + ";" + allAgents[i].GetComponent<AgentController>().agentRadius + ";" + allAgents[i].GetComponent<AgentController>().maxSpeed + ";"
                    + allAgents[i].GetComponent<AgentController>().GetColor() + ";" +
                    allAgents[i].transform.position.x + ";" + allAgents[i].transform.position.y + ";" + allAgents[i].transform.position.z + ";" + allAgents[i].GetComponent<AgentController>().go.name
                    + ";" + allAgents[i].GetComponent<AgentController>().GetCell().name);
            }
        }

        file.Close();

        //get obstacles info
        /*
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
        */

        fileObstacles.Close();
    }


    protected void SaveSimulationData()
    {
        //config file
        var file = File.CreateText(Application.dataPath + "/");
        //obstacles file
        var fileObstacles = File.CreateText(Application.dataPath + "/" + obstaclesFilename);

        //first, we save the terrain dimensions
        Terrain terrain = GameObject.Find("Terrain").GetComponent<Terrain>();
        file.WriteLine("terrainSize:" + terrain.terrainData.size.x + "," + terrain.terrainData.size.z);

        //get agents info
        GameObject[] allAgents = GameObject.FindGameObjectsWithTag("Player");
        if (allAgents.Length > 0)
        {
            //each line: name, radius, maxSpeed, color, positionx, positiony, positionz, goal object name, cell name
            //separated with ;

            file.WriteLine("qtdAgentesTotal:" + allAgents.Length);
            //for each agent
            for (int i = 0; i < allAgents.Length; i++)
            {
                file.WriteLine(allAgents[i].name + ";" + allAgents[i].GetComponent<AgentController>().agentRadius + ";" + allAgents[i].GetComponent<AgentController>().maxSpeed + ";"
                    + allAgents[i].GetComponent<AgentController>().GetColor() + ";" +
                    allAgents[i].transform.position.x + ";" + allAgents[i].transform.position.y + ";" + allAgents[i].transform.position.z + ";" + allAgents[i].GetComponent<AgentController>().go.name
                    + ";" + allAgents[i].GetComponent<AgentController>().GetCell().name);
            }
        }

        file.Close();

        //get obstacles info
        /*
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
        */

        fileObstacles.Close();
    }

    //load a csv config file
    protected void LoadConfigFile()
    {
        string line;

        // Create a new StreamReader, tell it which file to read and what encoding the file
        StreamReader theReader = new StreamReader(Application.dataPath + "/" + obstaclesFilename, System.Text.Encoding.Default);

        using (theReader)
        {
            int lineCount = 1;
            //int qntObstacles = 0;
            int qntVertices = 0;
            int qntTriangles = 0;
            int controlVertice = 0;
            int controlTriangle = 0;
            Vector3[] vertices = new Vector3[qntVertices];
            int[] triangles = new int[qntTriangles];

            do
            {
                line = theReader.ReadLine();

                if (line != null && line != "")
                {
                    //in the first line, we have the qntObstacles to instantiante
                    if (lineCount == 1)
                    {
                        string[] entries = line.Split(':');
                        //qntObstacles = System.Int32.Parse(entries[1]);
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
                    else
                    {
                        //else, we check if the value has ;. If so, it is a vector3. Otherwise, it is the triangle values
                        string[] entries = line.Split(';');
                        if (entries.Length > 1)
                        {
                            vertices[controlVertice] = new Vector3(System.Convert.ToSingle(entries[0]), System.Convert.ToSingle(entries[1]), System.Convert.ToSingle(entries[2]));
                            controlVertice++;
                        }
                        else
                        {
                            triangles[controlTriangle] = System.Int32.Parse(entries[0]);
                            controlTriangle++;

                            //if it is the last one, we create the Object
                            if (controlTriangle >= qntTriangles)
                            {
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
        Terrain terrain = GameObject.Find("Terrain").GetComponent<Terrain>();
        GameObject camera = GameObject.Find("Camera");

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
                    //else, if we are in the qntCells+qntAuxins+5 line, it is the qtdAgentesTotal to instantiate
                    else if (lineCount == qntCells + qntAuxins + 5)
                    {
                        string[] entries = line.Split(':');
                        qtdAgentesTotal = System.Int32.Parse(entries[1]);
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

                                AuxinController newAuxin = new AuxinController();
                                //change his name
                                newAuxin.name = entries[0];
                                //this auxin is from this cell
                                newAuxin.SetCell(hisCell);
                                //set position
                                newAuxin.position = new Vector3(System.Convert.ToSingle(entries[1]), System.Convert.ToSingle(entries[2]), System.Convert.ToSingle(entries[3]));
                                //alter auxinRadius
                                auxinRadius = System.Convert.ToSingle(entries[4]);
                                //add this auxin to this cell
                                hisCell.GetComponent<CellController>().AddAuxin(newAuxin);
                            }
                        }
                        else
                        {
                            string[] entries = line.Split(';');
                            if (entries.Length > 0)
                            {
                                if (lineCount <= qntAuxins + 5 + qtdAgentesTotal + qntCells)
                                {
                                    GameObject newAgent = Instantiate(agent, new Vector3(System.Convert.ToSingle(entries[4]),
                                    System.Convert.ToSingle(entries[5]), System.Convert.ToSingle(entries[6])),
                                    Quaternion.identity) as GameObject;
                                    //change his name
                                    newAgent.name = entries[0];
                                    //change his radius
                                    newAgent.GetComponent<AgentController>().agentRadius = System.Convert.ToSingle(entries[1]);
                                    //change his maxSpeed
                                    newAgent.GetComponent<AgentController>().maxSpeed = System.Convert.ToSingle(entries[2]);
                                    //change his color
                                    //new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f))
                                    string[] temp = entries[3].Split('(');
                                    temp = temp[1].Split(')');
                                    temp = temp[0].Split(',');
                                    newAgent.GetComponent<AgentController>().SetColor(
                                        new Color(System.Convert.ToSingle(temp[0]), System.Convert.ToSingle(temp[1]),
                                        System.Convert.ToSingle(temp[2])));
                                    newAgent.GetComponent<MeshRenderer>().material.color = newAgent.GetComponent<AgentController>().GetColor();
                                    //goal
                                    newAgent.GetComponent<AgentController>().go = GameObject.Find(entries[7]);
                                    //cell
                                    GameObject theCell = GameObject.Find(entries[8]);
                                    newAgent.GetComponent<AgentController>().SetCell(theCell);
                                    //agent radius
                                    newAgent.GetComponent<AgentController>().agentRadius = agentRadius;
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

    }


    public List<GameObject> GetAgentList()
    {
        return altruistas;
    }

    //save a csv exit file, with positions of all agents in function of time
    protected void SaveExitFile()
    {
        //get agents info
        GameObject[] allAgents = GameObject.FindGameObjectsWithTag("Player");
        if (allAgents.Length > 0)
        {
            //each line: frame, agents name, positionx, positiony, positionz, goal object name, cell name
            //separated with ;
            //for each agent
            for (int i = 0; i < allAgents.Length; i++)
            {
                //exitFile.WriteLine(Time.frameCount + ";" + allAgents[i].name + ";" + allAgents[i].transform.position.x + ";" +
                //allAgents[i].transform.position.y + ";" + allAgents[i].transform.position.z + ";" +
                //allAgents[i].GetComponent<AgentController>().go.name + ";" + 
                //allAgents[i].GetComponent<AgentController>().GetCell().name);
            }
        }
    }
}


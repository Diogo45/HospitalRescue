using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AgentControllerBase : MonoBehaviour {
	//agent radius
	public float agentRadius;
	//agent speed
	public Vector3 speed;
    //max speed
    public float maxSpeed;
    //goal
    public GameObject go;

    //list with all auxins in his personal space
    private List<AuxinControllerBase> myAuxins;
    //agent color
    private Color color;
    //agent cell
    private GameObject cell;
    //path
    private UnityEngine.AI.NavMeshPath path;
    //time elapsed (to calculate path just between an interval of time)
    private float elapsed;
	//auxins distance vector from agent
	public List<Vector3> vetorDistRelacaoMarcacao;

    /*
    START: Copied from Paravisi´s model 
    */
    private bool denominadorW  = false; // variavel para calculo da variavel m (impede recalculo)
    private float valorDenominadorW;	// variavel para calculo da variavel m (impede recalculo)
    private Vector3 m; //orientation vector (movement)
    public Vector3 goal; //goal position
    private Vector3 diff; //diff between goal and agent
    private  float diffMod; //diff module
    private Vector3 g; //goal vector (diff / diffMod)
    /*
    FINISH: Copied from Paravisi´s model 
    */

    void Awake(){
        //set inicial values
		myAuxins = new List<AuxinControllerBase>();        
        valorDenominadorW = 0;
        denominadorW = false;
        elapsed = 0f;
        
    }

    void Start() {
        goal = go.transform.position;
        diff = goal - transform.position;
        diffMod = Vector3.Distance(diff, new Vector3(0f, 0f, 0f));
        g = diff / diffMod;
        path = new UnityEngine.AI.NavMeshPath();
    }

    void Update() {
        //clear agent´s informations
        Debug.Log("AgenteUpdate");
        ClearAgent();
        // Update the way to the goal every second.
        elapsed += Time.deltaTime;

        if (true)
        {
            elapsed -= 1f;
            //calculate agent path
            UnityEngine.AI.NavMesh.CalculatePath(transform.position, go.transform.position, UnityEngine.AI.NavMesh.AllAreas, path);

            //update his goal
            if(path.corners.Length > 1)
            {
                goal = new Vector3(path.corners[1].x, 0f, path.corners[1].z);
            }
            diff = goal - transform.position;
            diffMod = Vector3.Distance(diff, new Vector3(0f, 0f, 0f));
            g = diff / diffMod;
        }

        //Debug.Log(path.corners.Length);
        //just to draw the path
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.red);
        }

        //if the distance between the agent and his actual goal is lower than 0.1, place him on the goal to update the navmesh
        //Sim, é gambiarra. Evita que os agentes "travem" próximos do next goal, o que faz com que o o CalculatePath não atualize os corners
        if (Vector3.Distance(transform.position, goal) < 0.1f) {
            //Debug.Log(gameObject.name+"--Position: "+transform.position+"--Goal: "+goal);
            //transform.Translate(goal * Time.deltaTime, Space.World);
            //transform.position = goal;
        }
    }

    //clear agent´s informations
    void ClearAgent()
    {
        //re-set inicial values
        valorDenominadorW = 0;
        vetorDistRelacaoMarcacao.Clear();
        denominadorW = false;
        m = new Vector3(0f, 0f, 0f);
        diff = goal - transform.position;
        diffMod = Vector3.Distance(diff, new Vector3(0, 0, 0));
        //diffMod = Vector3.Distance(goal, transform.position); pode ser assim tb, testar
        g = diff / diffMod;
    }

    //walk
    public void Caminhe()
    {
        //transform.position = Vector3.MoveTowards(transform.position, transform.position+speed, Time.deltaTime);
        //this one seems better (thanks Amyr =D)
        transform.Translate(speed * Time.deltaTime, Space.World);
        //Debug.Log(speed);
    }

    //The calculation formula starts here
    //the ideia is to find m=SUM[k=1 to n](Wk*Dk)
    //where k iterates between 1 and n (number of auxins), Dk is the vector to the k auxin and Wk is the weight of k auxin
    //the weight (Wk) is based on the degree resulting between the goal vector and the auxin vector (Dk), and the
    //distance of the auxin from the agent
    public void CalculaDirecaoM()
    {
        //for each agent´s auxin
        for (int k = 0; k < vetorDistRelacaoMarcacao.Count; k++)
        {
            //calculate W
            float valorW = CalculaW(k);
            if (valorDenominadorW < 0.0001)
            //if (valorDenominadorW == 0)
                valorW = 0.0f;
            
            //sum the resulting vector * weight (Wk*Dk)
            m += valorW * vetorDistRelacaoMarcacao[k]*maxSpeed;
        }
    }

    //calculate W
    float CalculaW(int indiceRelacao)
    {
        //calculate F (F is part of weight formula)
        float valorF = CalculaF(indiceRelacao);
        
        if (!denominadorW)
        {
            valorDenominadorW = 0f;

            //for each agent´s auxin
            for (int k = 0; k < vetorDistRelacaoMarcacao.Count; k++)
            {
                //calculate F for this k index, and sum up
                valorDenominadorW += CalculaF(k);
            }
            denominadorW = true;
        }

        float retorno = valorF / valorDenominadorW;
        return retorno;
    }

    //calculate F (F is part of weight formula)
    float CalculaF(int indiceRelacao)
    { 
        //distance between auxin´s distance and origin (dont know why origin...)
        float moduloY = Vector3.Distance(vetorDistRelacaoMarcacao[indiceRelacao], new Vector3(0, 0, 0));
        //distance between goal vector and origin (dont know why origin...)
        float moduloX = Vector3.Distance(g, new Vector3(0, 0, 0));
        //vector * vector
        float produtoEscalar = vetorDistRelacaoMarcacao[indiceRelacao].x * g.x + vetorDistRelacaoMarcacao[indiceRelacao].y * g.y + vetorDistRelacaoMarcacao[indiceRelacao].z * g.z;
        
        if (moduloY < 0.00001)
        {
            return 0.0f;
        }

        //return the formula, defined in tesis/paper
        float retorno = (float)((1.0 / (1.0 + moduloY)) * (1.0 + ((produtoEscalar) / (moduloX * moduloY))));
        return retorno;
    }

    //calculate speed vector    
    public void CalculaVelocidade()
    {
        //distance between movement vector and origin
        float moduloM = Vector3.Distance(m, new Vector3(0, 0, 0));

        //multiply for PI
        float s = moduloM * 3.14f;
        
        //if it is bigger than maxSpeed, use maxSpeed instead
        if (s > maxSpeed)
            s = maxSpeed;
        //Debug.Log("vetor M: " + m + " -- modulo M: " + s);
        if (moduloM > 0.0001 || true)
        {
            //calculate speed vector
            speed = s * (m / moduloM);
        }
        else
        {
            //else, he is idle
            speed = new Vector3(0, 0, 0);
        }
    }

    //find all auxins near him (Voronoi Diagram)
    //call this method from game controller, to make it sequential for each agent
    public void FindNearAuxins(){
		//clear them all, for obvious reasons
		myAuxins.Clear ();

        //get all auxins on my cell
        List<AuxinControllerBase> cellAuxins = cell.GetComponent<CellControllerBase>().GetAuxins();

        //iterate all cell auxins to check distance between auxins and agent
        for (int i = 0; i < cellAuxins.Count; i++) {
            //see if the distance between this agent and this auxin is smaller than the actual value, and inside agent radius
            float distance = Vector3.Distance(transform.position, cellAuxins[i].position);
            if (distance < cellAuxins[i].GetMinDistance() && distance <= agentRadius)
            {
                //take the auxin!!
                //if this auxin already was taken, need to remove it from the agent who had it
                if (cellAuxins[i].taken == true)
                {
                    GameObject otherAgent = cellAuxins[i].GetAgent();
                    otherAgent.GetComponent<AgentControllerBase>().myAuxins.Remove(cellAuxins[i]);
                }
                //auxin is taken
                cellAuxins[i].taken = true;
                //change the color (visual purpose)
                //cellAuxins[i].material.color = color;
                //auxin has agent
                cellAuxins[i].SetAgent(this.gameObject);
                //update min distance
                cellAuxins[i].SetMinDistance(distance);
                //update my auxins
                myAuxins.Add(cellAuxins[i]);
            }
        }

        //find all neighbours cells
        //default
        Terrain terrain = GameObject.Find("Terrain").GetComponent<Terrain>();
        int startX = (int)cell.transform.position.x - 2;
        int startZ = (int)cell.transform.position.z - 2;
        int endX = (int)cell.transform.position.x + 2;
        int endZ = (int)cell.transform.position.z + 2;
        //distance from agent to cell, to define agent new cell
        float distanceToCell = Vector3.Distance(transform.position, cell.transform.position);

        //see if it is in some border
        if((int)cell.transform.position.x == 1)
        {
            startX = (int)cell.transform.position.x;
        }
        if ((int)cell.transform.position.z == 1)
        {
            startZ = (int)cell.transform.position.z;
        }
        if ((int)cell.transform.position.x == (int)terrain.terrainData.size.x-1)
        {
            endX = (int)cell.transform.position.x;
        }
        if ((int)cell.transform.position.z == (int)terrain.terrainData.size.z - 1)
        {
            endZ = (int)cell.transform.position.z;
        }

        //iterate to find the cells
        //2 in 2, since the radius of each cell is 1 = diameter 2
        for(int i = startX; i <= endX; i = i + 2)
        {
            for (int j = startZ; j <= endZ; j = j + 2)
            {
                int nameX = i - 1;
                int nameZ = j - 1;
                //find the cell
                GameObject neighbourCell = CellControllerBase.GetCellByName("cell" + nameX + "-" + nameZ);

                //get all auxins on neighbourcell
                cellAuxins = neighbourCell.GetComponent<CellControllerBase>().GetAuxins();

                //iterate all cell auxins to check distance between auxins and agent
                for (int c = 0; c < cellAuxins.Count; c++)
                {
                    //see if the distance between this agent and this auxin is smaller than the actual value, and smaller than agent radius
                    float distance = Vector3.Distance(transform.position, cellAuxins[c].position);
                    if (distance < cellAuxins[c].GetMinDistance() && distance <= agentRadius)
                    {
                        //take the auxin!!
                        //if this auxin already was taken, need to remove it from the agent who had it
                        if (cellAuxins[c].taken == true)
                        {
                            GameObject otherAgent = cellAuxins[c].GetAgent();
                            otherAgent.GetComponent<AgentControllerBase>().myAuxins.Remove(cellAuxins[c]);
                        }
                        //auxin is taken
                        cellAuxins[c].taken = true;
                        //change the color (visual purpose)
                        //cellAuxins[i].material.color = color;
                        //auxin has agent
                        cellAuxins[c].SetAgent(this.gameObject);
                        //update min distance
                        cellAuxins[c].SetMinDistance(distance);
                        //update my auxins
                        myAuxins.Add(cellAuxins[c]);
                    }
                }

                //see distance to this cell
                float distanceToNeighbourCell = Vector3.Distance(transform.position, neighbourCell.transform.position);
                if(distanceToNeighbourCell < distanceToCell)
                {
                    distanceToCell = distanceToNeighbourCell;
                    SetCell(neighbourCell);
                }
            }
        }
    }

	//GET-SET
	public Color GetColor(){
		return this.color;
	}
	public void SetColor(Color color){
		this.color = color;
	}
    public GameObject GetCell()
    {
        return this.cell;
    }
    public void SetCell(GameObject cell)
    {
        this.cell = cell;
    }

    //add a new auxin on myAuxins
    public void AddAuxin(AuxinControllerBase auxin)
    {
        myAuxins.Add(auxin);
    }
    //return all auxins in this cell
    public List<AuxinControllerBase> GetAuxins()
    {
        return myAuxins;
    }
}

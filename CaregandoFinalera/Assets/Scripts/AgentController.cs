using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;




public class AgentController : MonoBehaviour
{
    private Vector3 lastPos;
    private Animator anim;
    // A funcao que vai determinar a carga que o agente consegue empurrar pela distancia
    public int FunctionType;
    private bool reset = false;
    //Rescue
    //private const double PORC_VARIACAO = 0.4f;
    private const float LIMIAR_DEPENDENCIA = 0.8f;
    //Se o altruista vai ou nao ser treinado para resgate e assim voltar para resgatar mais pessoas
    public bool TRAINED = false;
    public bool EMPLOYEE = false;
    //Dependente se ON_WHEELCHAIR ou ON_FAMILY for true
    public bool ON_WHEELCHAIR = false;
    public bool ON_FAMILY = false;
    // Calculado por 1 dividido pelo número de frames em um segundo
    private const float TIME_CONST = 1.0f/30.0f;
    
    public GameObject bkpGo;//guarda o goal principal
    protected float sMax;//maior modulo da velocidade do agente
    protected bool flagAltruismo;//altruismo liga/desliga
    public int estado;//buscando,carregando,objetivo
    protected float altruismo;//indice de altruismo(0 a 1)
    public float dependencia;//indice de dependencia(0 a 1);
    protected float indiceNIOSH_Carga;
    public float indiceNIOSH_Max;//indice maximo do niosh
    protected bool homem;
    public float peso;
    public float inicialForce;//Força de movimento maxima para calculo posterior da velocidade
    public List<GameObject> dependentes;
    protected float cargaRecomendada;//Peso recomendado baseado no niosh
    protected GameObject agenteDependente;//O dependente sendo resgatado por este altruista
    public List<GameObject> agentesNaoConsegueAjudar;//Os agentes que este altruista nao pode mais ajudar, seja isso por serem muito pesados ou ja estarem sendo carregados
    public float initSpeed = 1.3f;
    public float mSpeed = 0f;
    public List<GameObject> familia;//Se este for um dependente esta lista representa todos agentes altruistas que o estão ajudando
    public GroupController group;//Uma referencia para o grupo contendo o agente depente e os altruistas ajudando
    //Rescue
    //private bool podeAjudar;
    //agent radius
    public float agentRadius;
    //agent speed
    public Vector3 speed;
    //max speed
    public float maxSpeed;
    //goal
    public GameObject go;
    private int heuristic;
    public bool changePosition;
    //list with all auxins in his personal space
    private List<AuxinController> myAuxins;
    //agent color
    private Color color;
    //agent cell
    private GameObject cell;
    //path
    private UnityEngine.AI.NavMeshPath path;
    //time elapsed 
    public float elapsed;
    //auxins distance vector from agent
    public List<Vector3> vetorDistRelacaoMarcacao;
    /*
    START: Copied from Paravisi´s model 
    */
    private bool denominadorW = false; // variavel para calculo da variavel m (impede recalculo)
    private float valorDenominadorW;    // variavel para calculo da variavel m (impede recalculo)
    private Vector3 m; //orientation vector (movement)
    public Vector3 goal; //goal position
    private Vector3 diff; //diff between goal and agent
    private float diffMod; //diff module
    private Vector3 g; //goal vector (diff / diffMod)
    /*
    FINISH: Copied from Paravisi´s model 
    */

    private static Terrain terrain;
    public bool inPen = false;
    private static GameObject Auxin;

    void Awake()
    {

        
        Auxin = GameObject.Find("GameController").GetComponent<GameController>().Auxin;
        agentesNaoConsegueAjudar = new List<GameObject>();
        group = null;

        myAuxins = new List<AuxinController>();
        valorDenominadorW = 0;
        denominadorW = false;
        elapsed = 0f;
        path = new UnityEngine.AI.NavMeshPath();
    }

    void Start()
    {
        anim = GetComponent<Animator>();
        terrain = GameObject.Find("Terrain").GetComponent<Terrain>();
        dependentes = GameObject.FindGameObjectsWithTag("Dependente").ToList<GameObject>();
        inicialForce = peso * maxSpeed;
        goal = go.transform.position;
        diff = goal - transform.position;
        diffMod = Vector3.Distance(diff, new Vector3(0f, 0f, 0f));
        g = diff / diffMod;
        if (this.tag == "Altruista")
        {
            changePosition = true;
        }
        else
        {
            changePosition = false;
        }
    }

    public void AchaDependente()
    {
        float menorDist = float.PositiveInfinity;
        GameObject choosen = null;
        
        foreach (GameObject g in dependentes)
        {
            /*
            if (g.GetComponent<AgentController>().ON_FAMILY && !agentesNaoConsegueAjudar.Contains(g)) {
                agentesNaoConsegueAjudar.Add(g);
                continue;
            }
            */

            if (agentesNaoConsegueAjudar.Contains(g)) continue;
            float dist = PathDist(g);
            if (dist < menorDist )
            {
                menorDist = dist;
                choosen = g;
            }
        }
        if (choosen != null)
        {
            //Debug.Log(choosen.name);
            List<GameObject> aux = choosen.GetComponent<AgentController>().group.agentesBuscando;
            List<GameObject> aux2 = choosen.GetComponent<AgentController>().group.agentesCarregando;
            if(aux.Count < 1 && aux2.Count < 1)
            {
                choosen.GetComponent<AgentController>().group.agentesBuscando.Add(this.gameObject);
                this.group = choosen.GetComponent<AgentController>().group;
                this.BuscarAgente(this.group.GetDependente());
            }
            else
            {
                if(!agentesNaoConsegueAjudar.Contains(choosen))
                    agentesNaoConsegueAjudar.Add(choosen);
            }
            
        }
    }

    public void AchaDependente(List<GameObject> lst)
    {
        float menorDist = float.PositiveInfinity;
        GameObject choosen = null;

        foreach (GameObject g in lst)
        {
            if (g.GetComponent<AgentController>().ON_FAMILY && !agentesNaoConsegueAjudar.Contains(g))
            {
                agentesNaoConsegueAjudar.Add(g);
                continue;
            }

            if (agentesNaoConsegueAjudar.Contains(g)) continue;
            float dist = PathDist(g);
            if (dist < menorDist)
            {
                menorDist = dist;
                choosen = g;
            }
        }
        if (choosen != null)
        {
            
            //Debug.Log(choosen.name);
            List<GameObject> aux = choosen.GetComponent<AgentController>().group.agentesBuscando;
            List<GameObject> aux2 = choosen.GetComponent<AgentController>().group.agentesCarregando;
            if (aux.Count < 1 && aux2.Count < 1)
            {
                lst.Remove(choosen);
                choosen.GetComponent<AgentController>().group.agentesBuscando.Add(this.gameObject);
                this.group = choosen.GetComponent<AgentController>().group;
                this.BuscarAgente(this.group.GetDependente());
            }
            else
            {
                if (!agentesNaoConsegueAjudar.Contains(choosen))
                    agentesNaoConsegueAjudar.Add(choosen);
            }

        }
    }


    public float PathDist(GameObject g)
    {
        if (g.GetComponent<AgentController>().TRAINED) return float.PositiveInfinity;
        UnityEngine.AI.NavMesh.CalculatePath(transform.position, g.transform.position, UnityEngine.AI.NavMesh.AllAreas, path);
        float dist = 0f;
        for(int i = 0; i <path.corners.Length - 1; i++)
        {
            dist += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }
        return dist;
    }



    public void RestoreNIOSH()
    {
        //this.indiceNIOSH_Carga = carga / this.cargaRecomendada;
        Vector3 init = new Vector3(0, 0, 0);
        float distanciaCaminhada = Vector3.Distance(this.speed * TIME_CONST, init);
        indiceNIOSH_Max += distanciaCaminhada * 0.02f;

    }

    void Update()
    {
        //clear agent´s informations
        ClearAgent();
        // Update the way to the goal every second.

        elapsed += Time.deltaTime;

        if (this.tag == "Altruista" && !group && TRAINED)
        {
            AchaDependente();

        }
        if (group)
        {
            mSpeed = (initSpeed + maxSpeed) / 2;
        }
        /*
        else
        {
            if(this.group.conseguindoCarregar && !group.agentesCarregando.Contains(this.gameObject))
            {
                group.agentesBuscando.Remove(this.gameObject);
                this.agenteDependente = null;
                this.group = null;
                this.IrObjetivo();

            }
        }
        */
        
        if(elapsed >= 1f)
        {
            elapsed -= 1f;
            if(this.tag == "Altruista" && changePosition == false)
            {
                changePosition = true;
            }
        }
        
        //calculate agent path
        /*
        Vector3[] res;
        int[] pos;
        UnityEngine.AI.NavMesh.Triangulate(out res,out pos);
            
        for(int i = 0; i < pos.Length - 1; i++)
        {
            Debug.DrawLine(res[i], res[i + 1],Color.blue);
        }
        */
        UnityEngine.AI.NavMesh.CalculatePath(transform.position, go.transform.position, UnityEngine.AI.NavMesh.AllAreas, path);




        if (path.corners.Length > 1)
        {
            //update his goal
            Vector3 newGoal = new Vector3(path.corners[1].x, 0f, path.corners[1].z);
            
            goal = newGoal;
            diff = goal - transform.position;
            diffMod = Vector3.Distance(diff, new Vector3(0f, 0f, 0f));
            g = diff / diffMod;
        }

        //now, we check if agent is stuck with another agent
        //if so, change places
        
        GameObject agentI = this.gameObject;

        if (this.speed.Equals(Vector3.zero))
        {
            float dist = Vector3.Distance(transform.position, go.transform.position);
            if (dist > agentRadius)
            {
                Collider[] lockHit = Physics.OverlapSphere(agentI.transform.position, agentRadius);
                foreach (Collider loki in lockHit)
                {
                    //if it is the Player tag (agent) and it is not the agent itself and he can change position (to avoid forever changing)
                    if (loki.gameObject.tag == "Altruista" &&  loki.gameObject.name != agentI.gameObject.name && this.changePosition)
                    {
                        //if is on the same group ignore
                        if (this.group != null && this.group.pertenceGrupo(loki.gameObject)) continue;
                        Vector3 positionA = Vector3.zero;

                        
                        
                        if ((this.group && ON_FAMILY && this.group.conseguindoCarregar) && (loki.gameObject.GetComponent<AgentController>().group && !loki.gameObject.GetComponent<AgentController>().group.conseguindoCarregar && loki.gameObject.GetComponent<AgentController>().ON_FAMILY))
                        {
                            loki.GetComponent<AgentController>().changePosition = false;
                            this.changePosition = false;
                            //Debug.Log(agentI.gameObject.name + " -- " + loki.gameObject.name);
                            positionA = agentI.GetComponent<AgentController>().group.GetDependente().transform.position;
                            agentI.GetComponent<AgentController>().group.GetDependente().transform.position = loki.gameObject.GetComponent<AgentController>().group.GetDependente().transform.position ;
                            loki.gameObject.GetComponent<AgentController>().group.GetDependente().transform.position = positionA;
                            continue;
                        }
                        //the other agent will not change position in this frame
                        /*
                        if (this.group) {
                            foreach (GameObject g in this.group.agentesCarregando)
                            {
                                g.GetComponent<AgentController>().changePosition = false;
                            }
                        }
                        */
                        loki.GetComponent<AgentController>().changePosition = false;
                        this.changePosition = false;

                        //Debug.Log(agentI.gameObject.name + " -- " + loki.gameObject.name);
                        //exchange!!!
                        positionA = agentI.transform.position;
                        agentI.transform.position = loki.gameObject.transform.position;
                        loki.gameObject.transform.position = positionA;
                    }
                    if(loki.gameObject.tag == "Dependente" && loki.gameObject.name != agentI.gameObject.name && this.changePosition)
                    {
                        Vector3 positionA = Vector3.zero;
                        if (this.group && this.group.agenteCarregando && this.group.agenteCarregando.Equals(this.gameObject))
                        {
                            continue;
                        }
                        else
                        {
                            positionA = agentI.transform.position;
                            agentI.transform.position = loki.gameObject.transform.position;
                            loki.gameObject.transform.position = positionA;
                        }
                    }
                   
                }
            }
            
        }
        
        //just to draw the path
        
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.red);
        }

        /*
        if (this.go.tag == "Dependente" && Vector3.Distance(this.transform.position, go.transform.position) < agentRadius / 1.5)
        {
            this.goal = (this.transform.position - go.transform.position).normalized * (agentRadius) + go.transform.position;
        }
        */
        //CRUSHING
        /*
            RaioAgent();
            //Densidade();
            CalculaPro();
            conforto = myAuxins.Count;
            CalculaC();

            tempo += timeConst;

            if (tempo > 1f)
            {
                tempo = 0f;

                if (machucado > 1) machucado -= kMachucado / 2f * timeConst;
            }

            //Contagio();
            //Decaimento();
            //UpdateInterest();
        //SetEventParam();


        //varia a cor do agente de acordo com a densidade
        //this.gameObject.GetComponent<Renderer>().material.color = new Color(densidadeAgente / 6.5f, 0, (6.5f - densidadeAgente) / 6.5f);
        
 
            //if (eventoAtual.gameObject.name == "EventController")
            //{
                //this.gameObject.GetComponent<Renderer>().material.color = new Color(1, 0, 0);
            //}


            TesteInjuria();
            */
        //CRUSHING


        var currMoveVect = transform.position - lastPos;

        float totalAngleDiff = Vector3.SignedAngle(transform.forward, currMoveVect, Vector3.up);

        transform.Rotate(new Vector3(0, totalAngleDiff * 0.05f, 0), Space.World);

        anim.SetFloat("Speed", (speed.magnitude /1.3f ) * 0.5f );


        lastPos = transform.position;
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
        //changePosition = true;
    }

    //walk
    public void Caminhe()
    {
        //transform.position = Vector3.MoveTowards(transform.position, transform.position+speed, TIME_CONST);
        //this one seems better (thanks Amyr =D)
        if (dependencia <= LIMIAR_DEPENDENCIA) transform.Translate(speed * TIME_CONST, Space.World);
        else speed = Vector3.zero;
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


            //calculo de pesos tem duas versões, uma com o afoitar diminuindo o primeiro termo e na outra o segundo termo
            //o valor kDir é uma constante que recebe um valor quando a função afoitar é acionada e faz com que os agentes deem mais peso para o goal
            //valorW = Mathf.Min(aux * (float)conforto /(35f - kDir), aux) + Mathf.Max(20f / (Mathf.Pow((float)conforto, 2) + 40) - 0.1f, 0f);                       
            //valorW = Mathf.Min(aux * (float)conforto / (35f - 10 * kDir), aux) + Mathf.Max(20f / (Mathf.Pow((float)conforto, 2) + 40) - (0.1f + kDir), 0f);



            //if (valorW < 0.0001)
            if (valorDenominadorW == 0)
                valorW = 0.0f;

            //sum the resulting vector * weight (Wk*Dk)
            m += valorW * vetorDistRelacaoMarcacao[k] * maxSpeed;
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
        if (moduloM > 0.0001)
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

    public void FindNearAuxinsAlt()
    {
        List<AuxinController> aux = new List<AuxinController>();
        List<GameObject> tmep = new List<GameObject>();
        myAuxins.Clear();
        Collider[] hitCollider = Physics.OverlapSphere(transform.position, agentRadius);
        foreach (Collider c in hitCollider)
        {
            if (c.gameObject.tag == "Cell")
            {
                aux.AddRange(c.gameObject.GetComponent<CellController>().GetAuxins());
            }
        }
        foreach (AuxinController a in aux)
        {
            GameObject newAuxin = Instantiate(Auxin, a.position, Quaternion.identity) as GameObject;
            newAuxin.name = a.name;
            newAuxin.GetComponent<TempAuxin>().taken = a.taken;
            newAuxin.GetComponent<TempAuxin>().SetAgent(a.GetAgent());
            newAuxin.GetComponent<TempAuxin>().SetMinDistance(a.GetMinDistance());
            tmep.Add(newAuxin);
        }
        Collider[] outer = Physics.OverlapSphere(transform.position, agentRadius);
        foreach (Collider c in outer)
        {
            if (c.gameObject.tag == "Auxin")
            {

                if (c.gameObject.GetComponent<TempAuxin>().taken)
                {
                    GameObject otherAgent = c.gameObject.GetComponent<TempAuxin>().GetAgent();
                    otherAgent.GetComponent<AgentController>().myAuxins.Remove(AuxinController.GetAuxinName(c.gameObject.name));
                }
                AuxinController.GetAuxinName(c.gameObject.name).taken = true;
                AuxinController.GetAuxinName(c.gameObject.name).SetAgent(this.gameObject);
                myAuxins.Add(AuxinController.GetAuxinName(c.gameObject.name));

            }
        }
        foreach (GameObject g in tmep)
        {
            Destroy(g);
        }
    }


    public void FindNearAuxins()
    {
        //clear them all, for obvious reasons
        myAuxins.Clear();

        //get all auxins on my cell
        List<AuxinController> cellAuxins = cell.GetComponent<CellController>().GetAuxins();
        UISystemProfilerApi.BeginSample(UISystemProfilerApi.SampleType.Layout);
        //iterate all cell auxins to check distance between auxins and agent
        for (int i = 0; i < cellAuxins.Count; i++)
        {
            //see if the distance between this agent and this auxin is smaller than the actual value, and inside agent radius
            float distance = Vector3.Distance(transform.position, cellAuxins[i].position);


            //compara o produto distancia x calmo do agente dono da auxina com o que quer pegar
            //caso não tenha agente dono da auxina A2 fica como 1
            if (cellAuxins[i].taken == true)
            {
                GameObject otherAgent = cellAuxins[i].GetAgent();
            }

            //if (distance < cellAuxins[i].GetMinDistance() && distance <= agentRadius && !cellAuxins[i].takenByRuler)
            if (distance < cellAuxins[i].GetMinDistance() && distance <= agentRadius)
            {
                //take the auxin!!
                //if this auxin already was taken, need to remove it from the agent who had it
                if (cellAuxins[i].taken == true)
                {
                    GameObject otherAgent = cellAuxins[i].GetAgent();
                    otherAgent.GetComponent<AgentController>().myAuxins.Remove(cellAuxins[i]);
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
        UISystemProfilerApi.EndSample(UISystemProfilerApi.SampleType.Layout);
        //find all neighbours cells
        //default
        int startX = (int)cell.transform.position.x - 2;
        int startZ = (int)cell.transform.position.z - 2;
        int endX = (int)cell.transform.position.x + 2;
        int endZ = (int)cell.transform.position.z + 2;
        //distance from agent to cell, to define agent new cell
        float distanceToCell = Vector3.Distance(transform.position, cell.transform.position);

        //see if it is in some border
        if ((int)cell.transform.position.x == 1)
        {
            startX = (int)cell.transform.position.x;
        }
        if ((int)cell.transform.position.z == 1)
        {
            startZ = (int)cell.transform.position.z;
        }
        if ((int)cell.transform.position.x == (int)terrain.terrainData.size.x - 1)
        {
            endX = (int)cell.transform.position.x;
        }
        if ((int)cell.transform.position.z == (int)terrain.terrainData.size.z - 1)
        {
            endZ = (int)cell.transform.position.z;
        }

        //iterate to find the cells
        //2 in 2, since the radius of each cell is 1 = diameter 2
        UISystemProfilerApi.BeginSample(UISystemProfilerApi.SampleType.Layout);
        for (int i = startX; i <= endX; i = i + 2)
        {
            for (int j = startZ; j <= endZ; j = j + 2)
            {
                int nameX = i - 1;
                int nameZ = j - 1;
                //find the cell
                //Debug.LogWarning(gameObject.name);
                GameObject neighbourCell = CellController.GetCellByName("cell" + nameX + "-" + nameZ);

                //get all auxins on neighbourcell
                cellAuxins = neighbourCell.GetComponent<CellController>().GetAuxins();

                //iterate all cell auxins to check distance between auxins and agent
                for (int c = 0; c < cellAuxins.Count; c++)
                {
                    //see if the distance between this agent and this auxin is smaller than the actual value, and smaller than agent radius
                    float distance = Vector3.Distance(transform.position, cellAuxins[c].position);


                    // caso ele seja afoito a sua distnacia diminui o que leva a uma prioridade maior sobre os outros 
                    if (cellAuxins[c].taken == true)
                    {
                        GameObject otherAgent = cellAuxins[c].GetAgent();

                    }



                    if (distance < cellAuxins[c].GetMinDistance() && distance <= agentRadius)
                    {
                        //take the auxin!!
                        //if this auxin already was taken, need to remove it from the agent who had it
                        if (cellAuxins[c].taken == true)
                        {
                            GameObject otherAgent = cellAuxins[c].GetAgent();
                            otherAgent.GetComponent<AgentController>().myAuxins.Remove(cellAuxins[c]);
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
                if (distanceToNeighbourCell < distanceToCell)
                {
                    distanceToCell = distanceToNeighbourCell;
                    SetCell(neighbourCell);
                }
            }

        }
        UISystemProfilerApi.EndSample(UISystemProfilerApi.SampleType.Layout);
    }
    //parametro vai depender da estrategia utilizada
    public void BuscarAgente(GameObject Agente)
    {
        this.go = Agente;
        this.agenteDependente = Agente;
        //Agente.GetComponent<AgentController>().agentesAjudando.Add(this);
        //Debug.Log(this.name + " buscando " + agenteDependente.name);
    }


    public void IrObjetivo()
    {
        //this.group = null;
        this.go = bkpGo;
        //Debug.Log(this.name + " indo para o objetivo ");
    }

    public float CargaMaxima()
    {
        return cargaRecomendada * indiceNIOSH_Max;
    }

    public void ConfiguraAltruismo(float altruismo, float dependencia, float NIOSHcarga, float NIOSHmax, bool homem, float peso)
    {
        this.altruismo = altruismo;
        this.dependencia = dependencia;
        this.indiceNIOSH_Carga = NIOSHcarga;
        this.indiceNIOSH_Max = NIOSHmax;
        this.homem = homem;
        this.peso = peso;
        this.sMax = maxSpeed / 30.0f;

        float LC = peso / 3.0f;
        float HM = 1f;
        float VM = 1;
        float DM = 0.88f;
        float AM = 1;//angulo --> 1-0,0032*anguloEmGraus
        float FM = 0.91f;
        float CM = 1;
        this.cargaRecomendada = LC * HM * VM * DM * AM * FM * CM;
        //Debug.Log(this.cargaRecomendada*NIOSHmax);
    }


    public void AtualizaNIOSH(float carga)
    {
        this.indiceNIOSH_Carga = carga / this.cargaRecomendada;
        Vector3 init = new Vector3(0, 0, 0);
        float distanciaCaminhada = Vector3.Distance(this.speed * TIME_CONST, init);
        indiceNIOSH_Max -= distanciaCaminhada * 0.02f;
    }

    public void AtualizaDestinoDependente(GameObject ag)
    {
        
        this.go = ag;
    }

    public bool EstaExausto()
    {
        return this.indiceNIOSH_Max < 1;
    }
    private bool PrecisaAjuda()
    {
        return this.dependencia >= LIMIAR_DEPENDENCIA;
    }

    public void desisteCarregar(GameObject g)
    {
        agentesNaoConsegueAjudar.Add(g);
        this.group = null;
        this.IrObjetivo();
    }

    //GET-SET
    public Color GetColor()
    {
        return this.color;
    }
    public void SetColor(Color color)
    {
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
    public float obterNIOSH_MAX()
    {
        return this.indiceNIOSH_Max;
    }
    //add a new auxin on myAuxins
    public void AddAuxin(AuxinController auxin)
    {
        myAuxins.Add(auxin);
    }
    //return all auxins in this cell
    public List<AuxinController> GetAuxins()
    {
        return myAuxins;
    }
    /*
    public void setAjudar(bool b)
    {
        this.podeAjudar = b;
    }
    public bool getAjudar()
    {
        return this.podeAjudar;
    }
    */

    public void atualizaDepentes(GameObject g)
    {
        dependentes.Remove(g);
        if(agentesNaoConsegueAjudar.Contains(g))
            agentesNaoConsegueAjudar.Remove(g);
    }

    public List<GameObject> GetNaoConsegueAjudar()
    {
        return agentesNaoConsegueAjudar;
    }
    public void SetReset(bool n)
    {
        this.reset = n;
    }
    public bool GetReset()
    {
        elapsed += TIME_CONST;
        return this.reset;
    }
    public bool GetElapsed()
    {
        if (elapsed >= 5f)
        {
            elapsed = 0;
            return true;
        }
        return false;
    }

}


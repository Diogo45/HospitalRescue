    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AI;
    using System.Linq;

public class GroupController: MonoBehaviour {
    private const int FAMILY = 1;
    private const int WHEELCHAIR = 2;
    public float MaxWeightPerDist;
    public float Walked;
    //O tipo de Grupo entre ser uma familia ou um cadeirante e acompanhante
    public int GroupType;
    private const float TIME_CONST = 1.0f / 30.0f;
    private const float minWalkSpeed = 0.5f;

    //o Agente sendo Carregado pelo grupo
    private GameObject agenteDependente;
    //Agentes buscando o dependente
    public List<GameObject> agentesBuscando;
    //Agentes que ja chegaram ao seu dependente objetivo e estão no processo de carrega-lo
    public  List<GameObject> agentesCarregando;
    //a velocidade média do grupo
    private Vector3 groupSpeed;
    //O centro do grupo onde o dependente esta
    private Vector3 center;
    //soma das cargas maximas de cada agente carregando que sera utilizado no calculo do percentual por Agente
    private float somaMaxPeso;
    //flag para determinar se o grupo esta atualmente conseguindo carregar o dependente
    public bool conseguindoCarregar;
    //velocidade maxima do grupo que e carregado
    private float maxGroupSpeed;

    //private float elapsed;
    private float superMaxSpeed;
    private GameObject agenteBuscando;
    public GameObject agenteCarregando;
    void Awake()
    {
        Walked = 0f;
        MaxWeightPerDist = 0f;
        agenteBuscando = null;
        agenteCarregando = null;
        agentesCarregando = new List<GameObject>();
        agentesBuscando = new List<GameObject>();
        center = new Vector3(0, 0, 0);
        this.tag = "Grupo";
        this.conseguindoCarregar = false;
        //elapsed = 0.0f;
    }

    void Start()
    {
       
        somaMaxPeso = 0f;
    }
    void Update()
    {
        if (GroupType == WHEELCHAIR)
        {
            if (!agenteCarregando && agentesBuscando.Count > 0) { 
                agenteBuscando = agentesBuscando[0];
                float dist = Vector3.Distance(agenteDependente.transform.position, agenteBuscando.transform.position);

                if (dist <= agenteDependente.GetComponent<AgentController>().agentRadius * 1.5f)
                {
                    agenteCarregando = agenteBuscando;
                    agenteBuscando = null;
                    agentesBuscando.Remove(agenteBuscando);
                    superMaxSpeed = agenteCarregando.GetComponent<AgentController>().maxSpeed;
                    /*
                    GameObject[] allGoals = GameObject.FindGameObjectsWithTag("Goal");
                    float menorDist = float.PositiveInfinity;
                    GameObject choosen = null;
                    foreach (GameObject g in allGoals)
                    {
                        float dist_ = Vector3.Distance(agenteDependente.transform.position, g.transform.position);
                        if (dist_ < menorDist)
                        {
                            menorDist = dist_;
                            choosen = g;

                        }
                    }
                    */
                    agenteDependente.GetComponent<AgentController>().go = agenteCarregando.GetComponent<AgentController>().bkpGo;
                    agenteCarregando.GetComponent<AgentController>().IrObjetivo();
                }
            }
            else 
            {
                if (agenteCarregando)
                {
                    //Posicao do agente dependente
                    agenteDependente.transform.position = (agenteCarregando.transform.position - agenteCarregando.GetComponent<AgentController>().goal).normalized * agenteDependente.GetComponent<AgentController>().agentRadius + agenteCarregando.transform.position;
                    
                    //Testar a distancia caminhada
                    Vector3 init = Vector3.zero;
                    float distanciaCaminhada = Vector3.Distance(agenteCarregando.GetComponent<AgentController>().speed * Time.deltaTime, init);
                    Walked += distanciaCaminhada;
                    int funcType = agenteCarregando.GetComponent<AgentController>().FunctionType;
                    switch (funcType)
                    {
                        case 1:
                            MaxWeightPerDist = 67.1163f * Mathf.Pow(Walked, -0.1950f);
                            break;
                        case 2:
                            MaxWeightPerDist = 57.774f * Mathf.Pow(Walked, -0.1961f);
                            break;
                        case 3:
                            MaxWeightPerDist = 47.1301f * Mathf.Pow(Walked, -0.1938f);
                            break;
                        case 4:
                            MaxWeightPerDist = 36.1163f * Mathf.Pow(Walked, -0.1898f);
                            break;
                        case 5:
                            MaxWeightPerDist = 26.1163f * Mathf.Pow(Walked, -0.19046f);
                            break;
                         default:
                            Debug.Log(funcType + " N.A");
                            break;
                    }
                    //if(MaxWeightPerDist > 40)Debug.Log(MaxWeightPerDist);
                    if (agenteDependente.GetComponent<AgentController>().peso > MaxWeightPerDist)
                    {
                       
                            //reduzir velocidade do grupo caso cansado          
                            float k = Mathf.Min(MaxWeightPerDist / agenteDependente.GetComponent<AgentController>().peso, 1f);
                            agenteCarregando.GetComponent<AgentController>().maxSpeed = superMaxSpeed * k;
                        
                        //Debug.Log(agenteCarregando.GetComponent<AgentController>().maxSpeed);
                    }
                }

            }

        }
        else
        {
            if (agentesBuscando.Count > 0)
            {
                somaMaxPeso = 0f;
                for (int i = 0; i < agentesBuscando.Count; i++)
                {
                    float dist = Vector3.Distance(agenteDependente.transform.position, agentesBuscando[i].transform.position);
                    
                    if (dist <= agenteDependente.GetComponent<AgentController>().agentRadius * 1.5f)
                    {
                        agentesCarregando.Add(agentesBuscando[i]);
                        agentesBuscando.Remove(agentesBuscando[i]);
                    }


                }
                for (int i = 0; i < agentesCarregando.Count; i++)
                {
                    somaMaxPeso += agentesCarregando[i].GetComponent<AgentController>().CargaMaxima();

                }
                float percentualPorAgente = this.agenteDependente.GetComponent<AgentController>().peso / somaMaxPeso;
                //Debug.Log(percentualPorAgente);
                if (percentualPorAgente > 1)
                {
                    consegueCarregar(false);
                }
                else
                {

                    consegueCarregar(true);
                }
                //Debug.Log(conseguindoCarregar + " para " + agenteDependente.name);

            }
            if (conseguindoCarregar)
            {

                //elapsed += Time.deltaTime;
                somaMaxPeso = 0f;
                for (int i = 0; i < agentesCarregando.Count; i++)
                {
                    somaMaxPeso += agentesCarregando[i].GetComponent<AgentController>().CargaMaxima();
                    float dist = Vector3.Distance(agenteDependente.transform.position, agentesCarregando[i].transform.position);
                    if (dist > agenteDependente.GetComponent<AgentController>().agentRadius)
                    {
                        agentesCarregando[i].transform.position = (agentesCarregando[i].transform.position - agenteDependente.transform.position).normalized * (agenteDependente.GetComponent<AgentController>().agentRadius) + agenteDependente.transform.position;
                    }
                }
                float percentualPorAgente = this.agenteDependente.GetComponent<AgentController>().peso / somaMaxPeso;
                if (percentualPorAgente >= 1)
                {
                    //Debug.Log(agenteDependente.name + " " + percentualPorAgente);
                }
                if (percentualPorAgente > 1)
                {
                    consegueCarregar(false);
                    //this.removeExaustos();

                }
                else
                {
                    CalculaNovaVelocidadeMaxima();
                    foreach (GameObject o in agentesCarregando)
                    {
                        groupSpeed += o.GetComponent<AgentController>().speed;
                    }
                    groupSpeed = groupSpeed / agentesCarregando.Count;
                    for (int i = 0; i < agentesCarregando.Count; i++)
                    {
                        agentesCarregando[i].GetComponent<AgentController>().AtualizaNIOSH(this.agenteDependente.GetComponent<AgentController>().peso * percentualPorAgente);
                        //agentesCarregando[i].GetComponent<AgentController>().speed = groupSpeed;
                    }

                    for (int i = 0; i < agentesCarregando.Count; i++)
                    {
                        agentesCarregando[i].GetComponent<AgentController>().go = GameObject.FindGameObjectWithTag("Goal");
                        center += agentesCarregando[i].GetComponent<AgentController>().transform.position;
                    }

                    Vector3 aux = center / agentesCarregando.Count;
                    agenteDependente.transform.position = aux;
                    center = new Vector3(0, 0, 0);
                }

            }
            else
            {
                //this.removeExaustos();

                foreach (GameObject o in agentesCarregando)
                {
                    o.GetComponent<AgentController>().go = agenteDependente;
                    /*
                    if (!o.GetComponent<NavMeshObstacle>())
                    {
                        var obs = o.AddComponent<NavMeshObstacle>();
                        obs.shape = NavMeshObstacleShape.Capsule;
                        obs.carving = true;
                        //obs = agenteDependente.AddComponent<NavMeshObstacle>();
                        //obs.shape = NavMeshObstacleShape.Capsule;
                        //obs.carving = true;
                    }
                    */
                    //o.GetComponent<AgentController>().RestoreNIOSH();
                }
            }
        }
       

    }
    private int Calls = 0;

    private void CalculaNovaVelocidadeMaxima()
    {
        float somaDosPesos = 0;
        foreach(GameObject g in agentesCarregando)
        {
            somaDosPesos += g.GetComponent<AgentController>().peso;
        }
        float p;
        float aux = 0;
        foreach(GameObject o in agentesCarregando)
        {
            float pesoInit = o.GetComponent<AgentController>().peso;
            p = (pesoInit * 100) / somaDosPesos;
            //Debug.Log("p" + p);
            pesoInit = pesoInit + p;
            //Debug.Log("force" + o.GetComponent<AgentController>().inicialForce);
            float newMaxSpeed = o.GetComponent<AgentController>().inicialForce / pesoInit;
            //o.GetComponent<AgentController>().maxSpeed = newMaxSpeed;

            aux += newMaxSpeed ;
            //Debug.Log("newMaxSpeed " + newMaxSpeed);
        }
        Calls++;
        if (aux / agentesCarregando.Count >= 0.7f)
            this.maxGroupSpeed = aux / agentesCarregando.Count;
        //Debug.Log(maxGroupSpeed);
    }

    public bool pertenceGrupo(GameObject g)
    {

        if (g.name == agenteDependente.name) return true;
        foreach (GameObject o in this.agentesCarregando)
        {
            if (g.name == o.name)
            {
                return true;
            }
        }
        return false;
    }
    
    private void consegueCarregar(bool b)
    {
        this.conseguindoCarregar = b;
    }
    
    private void removeExaustos()
    {
        float menorNIOSH = float.PositiveInfinity;
        GameObject agenteMaisFraco = null;
        foreach(GameObject g in agentesCarregando)
        {
            float aux = g.GetComponent<AgentController>().obterNIOSH_MAX();
            if (aux < menorNIOSH)
            {
                menorNIOSH = aux;
                agenteMaisFraco = g;
            }
            if (g.GetComponent<AgentController>().EstaExausto())
            {
                g.GetComponent<AgentController>().desisteCarregar(this.agenteDependente);
                //Debug.Log("Antes " + " " + g.name + " " + g.GetComponent<AgentController>().group);
                //g.GetComponent<AgentController>().group = null;
                //Debug.Log("Depois" + g.GetComponent<AgentController>().group);
                agentesCarregando.Remove(g);
                break;
            }
        }
        if (agentesCarregando.Count >= 4 && agenteMaisFraco!=null )
        {
            agenteMaisFraco.GetComponent<AgentController>().desisteCarregar(this.agenteDependente);
            //Debug.Log("Antes2" + agenteMaisFraco.GetComponent<AgentController>().group);
            //agenteMaisFraco.GetComponent<AgentController>().group = null;
            //Debug.Log("Depois2" + agenteMaisFraco.GetComponent<AgentController>().group);
            agentesCarregando.Remove(agenteMaisFraco);
        }
    }
        public void SetDependente(GameObject g)
        {
            this.agenteDependente = g;
        }
        public GameObject GetDependente()
        {
            return this.agenteDependente;
        }

        public List<GameObject> GetCarregando()
        {
            return this.agentesCarregando;
        }

        public List<GameObject> GetBuscando()
        {
            return this.agentesBuscando;
        }
    }

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CellControllerBase : MonoBehaviour {

    public string cellName
    {
        set
        {
            name = value;
            cellCache[name] = gameObject;
        }
    }
    //static structure to easier find cells by name
    private static Dictionary<string, GameObject> cellCache = new Dictionary<string, GameObject>();

    public static GameObject GetCellByName(string idName)
    {
        if (cellCache.ContainsKey(idName))
        {
            return cellCache[idName];
        }
        else
        {
            //Debug.LogWarning(idName + ": Cell not found!!!");
            return null;
        }
    }
    private List<AuxinControllerBase> myAuxins;

    void Awake()
    {
        myAuxins = new List<AuxinControllerBase>();
    }
    //add a new auxin on myAuxins
    public void AddAuxin(AuxinControllerBase auxin)
    {
        myAuxins.Add(auxin);
    }

    //return all auxins in this cell
    public List<AuxinControllerBase> GetAuxins() {
        return myAuxins;     
    }
}

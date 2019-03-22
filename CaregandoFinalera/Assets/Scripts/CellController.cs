using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CellController : MonoBehaviour {
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
            Debug.LogWarning(idName + ": Cell not found!!!");
            return null;
        }
    }

    private List<AuxinController> myAuxins;

    void Awake() {
        myAuxins = new List<AuxinController>();
    }

    public void ClearAuxins()
    {
        myAuxins.Clear();
    }

    //add a new auxin on myAuxins
    public void AddAuxin(AuxinController auxin)
    {
        myAuxins.Add(auxin);
    }

    //return all auxins in this cell
    public List<AuxinController> GetAuxins() {
        return myAuxins;     
    }


}

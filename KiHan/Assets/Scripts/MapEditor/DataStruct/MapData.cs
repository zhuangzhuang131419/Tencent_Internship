using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

public class MapData : MonoBehaviour
{
    [SerializeField]private int id;
    [SerializeField]private string desc;
    // private List<MapGenerator> mapGenerators = new List<MapGenerator>();

    public int ID
    {
        get { return id; }
        set { id = value; }
    }

    public string Desc
    {
        get { return desc; }
        set { desc = value; }
    }
}
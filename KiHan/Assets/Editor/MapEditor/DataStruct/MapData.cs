using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEditor;

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

    //public List<MapGenerator> MapGenerators
    //{
    //    get { return mapGenerators; }
    //    set { mapGenerators = value; }
    //}
}
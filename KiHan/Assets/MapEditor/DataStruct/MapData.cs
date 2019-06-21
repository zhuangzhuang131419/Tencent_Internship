using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEditor;

public class MapData : MonoBehaviour
{
    private MapDataStruct mapDataStruct = new MapDataStruct();

    public MapDataStruct DataStruct
    {
        get { return mapDataStruct; }
        set { mapDataStruct = value; }
    }
}

[Serializable] 
public class MapDataStruct
{
    private int id;
    private string desc;
    private List<MapGeneratorDataStruct> mapGenerators = new List<MapGeneratorDataStruct>();

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

    public List<MapGeneratorDataStruct> MapGenerators
    {
        get { return mapGenerators; }
        set { mapGenerators = value; }
    }
}
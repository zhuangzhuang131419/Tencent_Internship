using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

[Serializable]
public class MapData
{
    public readonly int ID;
    private string Desc;

    private List<MapGenerator> mapGenerators = new List<MapGenerator>();

    public MapData(string mapID)
    {
        ID = int.Parse(mapID);
    }

    public List<MapGenerator> MapGenerators
    {
        get { return mapGenerators; }
        set { mapGenerators = value; }
    }
}

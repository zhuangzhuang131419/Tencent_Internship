using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map
{
    private GameObject map;
    private MapData mapData;

    public GameObject MapObject
    {
        get { return map; }
        set { map = value; }
    }

    public MapData MapData
    {
        get { return mapData; }
        set { mapData = value; }
    }

    public Map(GameObject mapObject, MapData mapData)
    {
        this.map = mapObject;
        this.MapData = mapData;
    }
}

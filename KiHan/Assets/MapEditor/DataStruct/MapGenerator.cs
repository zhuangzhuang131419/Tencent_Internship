using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

public class MapGenerator : MonoBehaviour
{
    private MapGeneratorDataStruct mapGeneratorStruct = new MapGeneratorDataStruct();

    public MapGeneratorDataStruct DataStruct
    {
        get { return mapGeneratorStruct; }
        set { mapGeneratorStruct = value; }
    }
}

[Serializable]
public class MapGeneratorDataStruct
{
    private int id;
    private string desc;
    private string generatorName;
    private int index = 0;
    private TransformPosition transformPosition;

    // 0/无，暂时无功能，占位用
    // 1/怪物，默认为怪物
    private int type;

    private List<UnitStruct> units = new List<UnitStruct>();

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

    public string Name
    {
        get { return generatorName; }
        set { generatorName = value; }
    }

    public int Index
    {
        get { return index; }
        set { index = value; }
    }

    public int Type
    {
        get { return type; }
        set { type = value; }
    }

    public List<UnitStruct> Units
    {
        get { return units; }
        set { units = value; }
    }

    public TransformPosition Position
    {
        get { return transformPosition; }
        set { transformPosition = value; }
    }
}

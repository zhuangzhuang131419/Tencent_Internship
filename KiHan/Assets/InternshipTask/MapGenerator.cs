using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

[Serializable]
public class MapGenerator
{
    private readonly int ID;
    private string Desc;
    private string name;
    private int index;

    // 0/无，暂时无功能，占位用
    // 1/怪物，默认为怪物
    private int Type;

    private List<Unit> units = new List<Unit>();

    public MapGenerator() { }
    public MapGenerator(string name, int index)
    {
        this.name = name;
        this.index = index;
    }

    public string Name
    {
        get { return name; }
    }

    public int Index
    {
        get { return index; }
    }

    public List<Unit> Units
    {
        get { return units; }
        set { units = value; }
    }

    

}

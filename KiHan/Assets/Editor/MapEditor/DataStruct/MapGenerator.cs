using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

public class MapGenerator : MonoBehaviour
{
    [SerializeField]private int id;
    [SerializeField]private string desc;
    [SerializeField]private string generatorName;
    [SerializeField]private int index = 0;

    // 0/无，暂时无功能，占位用
    // 1/怪物，默认为怪物
    [SerializeField]private int type;

    // private List<Unit> units = new List<Unit>();

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

    //public List<Unit> Units
    //{
    //    get { return units; }
    //    set { units = value; }
    //}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Unit
{ 
    private int index;
    private string unitName;
    private string Desc;
    private int unitID;
    private int CreateAction;
    private int CreateFrame;
    private int direction;
    private int DelayCreateTime;
    private int centerToPlayer;

    public Unit() { }
    public Unit(int index, string unitName)
    {
        this.index = index;
        this.unitName = unitName;
    }

    public int Index
    {
        get { return index; }
    }

    public string UnitName
    {
        get { return unitName; }
    }

}

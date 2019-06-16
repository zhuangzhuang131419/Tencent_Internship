using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Unit : MonoBehaviour
{
    private UnitStruct unitStruct = new UnitStruct();

    public UnitStruct DataStruct
    {
        get { return unitStruct; }
        set { unitStruct = value; }
    }
}

[Serializable]
public class UnitStruct
{
    private int index;
    private string unitName;
    private string desc;
    private int unitID;
    private int createAction;
    private int createFrame;
    private int direction;
    private int delayCreateTime;
    private int centerToPlayer;

    public int Index
    {
        get { return index; }
        set { index = value; }
    }

    public string Name
    {
        get { return unitName; }
        set { unitName = value; }
    }

    public string Desc
    {
        get { return desc; }
        set { desc = value; }
    }

    public int ID
    {
        get { return unitID; }
        set { unitID = value; }
    }

    public int CreateAction
    {
        get { return createAction; }
        set { createAction = value; }
    }

    public int CreateFrame
    {
        get { return createFrame; }
        set { createFrame = value; }
    }

    public int Direction
    {
        get { return direction; }
        set { direction = value; }
    }

    public int DelayCreateTime
    {
        get { return delayCreateTime; }
        set { delayCreateTime = value; }
    }

    public int CenterToPlay
    {
        get { return centerToPlayer; }
        set { centerToPlayer = value; }
    }
}

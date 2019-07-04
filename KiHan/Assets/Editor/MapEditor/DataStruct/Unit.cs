using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Unit : MonoBehaviour
{
    [SerializeField] private int index;
    [SerializeField] private string unitName;
    [SerializeField] private string desc;
    [SerializeField] private int unitID;
    [SerializeField] private int createActionVKey;
    [SerializeField] private int createFrame;
    [SerializeField] private float createHeight;
    [SerializeField] private int direction;
    [SerializeField] private int delayCreateTime;
    [SerializeField] private int centerToPlayer;

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
        get { return createActionVKey; }
        set { createActionVKey = value; }
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

    public int CenterToPlayer
    {
        get { return centerToPlayer; }
        set { centerToPlayer = value; }
    }

    public float CreateHeight
    {
        get { return createHeight; }
        set { createHeight = value; }
    }
}

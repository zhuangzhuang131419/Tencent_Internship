using UnityEngine;
using System.Collections;
using System;
using UnityEditor;
using KH;
using System.Threading;

[Serializable]
public enum MouseType
{
    Left,
    Right,
}

/// <summary>
/// 记录一个鼠标的事件
/// </summary>
[Serializable]
public class MouseAction : Message, ICommand
{

    private MouseEvent startEvent;
    private MouseEvent endEvent;


    public MouseAction(MouseEvent start, MouseEvent end)
    {
        startEvent = start;
        endEvent = end;
        TimeStamp = startEvent.TimeStamp;
    }

    public MouseAction()
    {

    }

    /// <summary>
    /// 重新模拟执行该事件
    /// </summary>
    public void execute()
    {
        
        switch (startEvent.Type)
        {
            case MouseType.Left:
                if (startEvent.PosX == endEvent.PosX && startEvent.PosY == endEvent.PosY)
                {
                    Debuger.Log("点击事件" + startEvent.TimeStamp);
                    MouseSimulator.LeftClick(startEvent.PosX, startEvent.PosY);
                }
                break;
            case MouseType.Right:
                MouseSimulator.RightDown(startEvent.PosX, startEvent.PosY);
                MouseSimulator.RightUp(endEvent.PosX, endEvent.PosY);
                break;
            default:
                break;
        }
    }
}

[Serializable]
public class MouseEvent : Message
{
    private float viewportPosX;
    private float viewportPosY;
    private MouseType mouseType;

    public MouseEvent(float x, float y, MouseType type, ulong timeStamp)
    {
        viewportPosX = x;
        viewportPosY = y;
        mouseType = type;
        TimeStamp = timeStamp;
    }

    public float PosX
    {
        get { return viewportPosX; }
    }

    public float PosY
    {
        get { return viewportPosY; }
    }

    public MouseType Type
    {
        get { return mouseType; }
    }
}

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
public class MouseAction : Message
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
                    Debug.Log("点击事件" + startEvent.TimeStamp);
                    MouseSimulator.LeftClick(startEvent.PosX, startEvent.PosY);
                }
                else
                {
                    Debug.Log("拖拽事件");
                    Debug.Log("开始事件" + startEvent.PosX + ", " + startEvent.PosY);
                    MouseSimulator.LeftDown(startEvent.PosX, startEvent.PosY);
                    Thread.Sleep((int)(endEvent.TimeStamp - startEvent.TimeStamp) * 1000);
                    Debug.Log("结束事件" + endEvent.PosX + ", " + endEvent.PosY);
                    MouseSimulator.LeftUp(endEvent.PosX, endEvent.PosY);
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

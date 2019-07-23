using UnityEngine;
using System;
using KH;

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
                Debuger.Log("点击事件" + startEvent.TimeStamp);
                MouseSimulator.LeftClick(endEvent.ViewportPos.x, endEvent.ViewportPos.y);
                break;
            case MouseType.Right:
                MouseSimulator.RightDown(startEvent.ViewportPos.x, startEvent.ViewportPos.y);
                MouseSimulator.RightUp(endEvent.ViewportPos.x, endEvent.ViewportPos.y);
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

    public MouseEvent(Vector2 v, MouseType type, ulong timeStamp)
    {
        ViewportPos = v;
        mouseType = type;
        TimeStamp = timeStamp;
    }

    public Vector2 ViewportPos
    {
        get { return new Vector2(viewportPosX, viewportPosY); }
        set
        {
            viewportPosX = value.x;
            viewportPosY = value.y;
        }
    }

    public MouseType Type
    {
        get { return mouseType; }
    }
}

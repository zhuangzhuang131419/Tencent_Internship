using UnityEngine;
using System.Collections;
using System;
using UnityEditor;
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
public class MouseAction {


    private MouseEvent startEvent;
    private MouseEvent endEvent;


    public MouseAction(MouseEvent start, MouseEvent end)
    {
        startEvent = start;
        endEvent = end;
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
                Debug.Log("开始事件" + startEvent.PosX + ", " + startEvent.PosY);
                MouseSimulator.LeftDown(startEvent.PosX, startEvent.PosY);
                Debug.Log("结束事件" + endEvent.PosX + ", " + endEvent.PosY);

                // MouseSimulator.MoveTo(endEvent.PosX, endEvent.PosY);
                MouseSimulator.LeftUp(endEvent.PosX, endEvent.PosY);
                // MouseSimulator.MoveTo(endEvent.PosX, endEvent.PosY);
                // MouseSimulator.SetCursorPos(1100, 200);
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
public class MouseEvent
{
    private float viewportPosX;
    private float viewportPosY;
    private ulong timeStamp;
    private MouseType mouseType;

    public MouseEvent(float x, float y, MouseType type)
    {
        viewportPosX = x;
        viewportPosY = y;
        mouseType = type;
    }

    public MouseEvent(float x, float y, MouseType type, ulong timeStamp)
    {
        viewportPosX = x;
        viewportPosY = y;
        mouseType = type;
        this.timeStamp = timeStamp;
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

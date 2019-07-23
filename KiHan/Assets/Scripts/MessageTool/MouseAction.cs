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

    private string clickedButton = null;
    private string clickedListener = null;
    private float PosX;
    private float PosY;
    private float PosZ;

    public Vector3 Pos
    {
        get
        {
            return new Vector3(PosX, PosY, PosZ);
        }
        set
        {
            PosX = value.x;
            PosY = value.y;
            PosZ = value.z;
        }
    }


    public MouseAction(UIButton clickedButton, ulong timeStamp)
    {
        this.clickedButton = clickedButton.name;
        Pos = clickedButton.gameObject.transform.position;
        TimeStamp = timeStamp;
    }

    public MouseAction(UIEventListener clickedUIListener, ulong timeStamp)
    {
        this.clickedListener = clickedUIListener.name;
        Pos = clickedUIListener.gameObject.transform.position;
        TimeStamp = timeStamp;
    }

    public MouseAction()
    {

    }

    /// <summary>
    /// 重新模拟执行该事件
    /// </summary>
    public void execute()
    {
        if (clickedButton != null)
        {
            foreach (var item in UnityEngine.Object.FindObjectsOfType<GameObject>())
            {
                if (item.name == clickedButton && item.transform.position.Equals(Pos))
                {
                    item.GetComponent<UIButton>().OnClick();
                }
            }
        }
        else if (clickedListener != null)
        {
            foreach (var item in UnityEngine.Object.FindObjectsOfType<GameObject>())
            {
                if (item.name == clickedListener && item.transform.position.Equals(Pos))
                {
                    item.GetComponent<UIEventListener>().OnClick();
                }
            }
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

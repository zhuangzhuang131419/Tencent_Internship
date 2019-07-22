using UnityEngine;
using System.Collections;
using KH;
using System;

[Serializable]
public class DragAction : Message, ICommand {

    // private UIPanel targetPanel;
    private string UIPanelName;

    private float clipOffsetX;
    private float clipOffsetY;
    private float clipOffsetZ;

    private float localPositionX;
    private float localPositionY;
    private float localPositionZ;


    private static float precision = 10000;

    public UIPanel Panel
    {
        get
        {
            return GameObject.Find(UIPanelName).GetComponent<UIPanel>();
        }
        set
        {
            UIPanelName = value.gameObject.name;
        }
    }

    public Vector3 ClipOffset
    {
        get
        {
            return new Vector3(clipOffsetX, clipOffsetY, clipOffsetZ);
        }
        set
        {
            clipOffsetX = value.x;
            clipOffsetY = value.y;
            clipOffsetZ = value.z;
        }
    }

    public Vector3 LocalPosition
    {
        get
        {
            return new Vector3(localPositionX, localPositionY, localPositionZ);
        }
        set
        {
            localPositionX = value.x;
            localPositionY = value.y;
            localPositionZ = value.z;
        }
    }

    public DragAction(UIPanel uiPanel, Vector3 clipOffset, Vector3 localPosition, ulong timeStamp)
    {
        TimeStamp = timeStamp;
        UIPanelName = uiPanel.gameObject.name;
        ClipOffset = clipOffset;
        LocalPosition = localPosition;
    }


    public void execute()
    {
        UIPanel targetPanel = GameObject.Find(UIPanelName).GetComponent<UIPanel>();
        UIScrollView targetScroll = GameObject.Find(UIPanelName).GetComponent<UIScrollView>();
        targetScroll.transform.localPosition = new Vector3(localPositionX, localPositionY, localPositionZ);
        targetPanel.clipOffset = new Vector3(clipOffsetX, clipOffsetY, clipOffsetZ);
        targetScroll.UpdateScrollbars(false);
    }

    public static DragAction operator+ (DragAction d1, DragAction d2)
    {
        if (d1.TimeStamp == d2.TimeStamp && d1.Panel.gameObject.name == d2.Panel.gameObject.name)
        {
            return new DragAction(d1.Panel, d1.ClipOffset + d2.ClipOffset, d1.LocalPosition + d2.LocalPosition, d1.TimeStamp);
        }
        return null;
    }
}

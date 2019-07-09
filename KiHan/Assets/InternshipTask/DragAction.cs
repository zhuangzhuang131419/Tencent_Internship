using UnityEngine;
using System.Collections;
using KH;
using System;

[Serializable]
public class DragAction : Message, ICommand {

    // private UIPanel targetPanel;
    private string UIPanelName;
    private float absoluteX;
    private float absoluteY;
    private float absoluteZ;

    private float momentumX;
    private float momentumY;
    private float momentumZ;

    public Vector3 Absolute
    {
        get
        {
            return new Vector3(absoluteX, absoluteY, absoluteZ);
        }
        set
        {
            absoluteX = value.x;
            absoluteY = value.y;
            absoluteZ = value.z;
        }
    }

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

    public DragAction(UIPanel uiPanel, Vector3 absolute, Vector3 momentum, ulong timeStamp)
    {
        TimeStamp = timeStamp;
        UIPanelName = uiPanel.gameObject.name;
        absoluteX = absolute.x;
        absoluteY = absolute.y;
        absoluteZ = absolute.z;

        momentumX = momentum.x;
        momentumY = momentum.y;
        momentumZ = momentum.z;
    }

    public void execute()
    {
        Debuger.Log("execute drag event");
        UIPanel targetPanel = GameObject.Find(UIPanelName).GetComponent<UIPanel>();
        UIScrollView targetScroll = GameObject.Find(UIPanelName).GetComponent<UIScrollView>();
        targetScroll.mMomentum = new Vector3(momentumX, momentumY, momentumZ);
        targetScroll.MoveAbsolute(new Vector3(absoluteX, absoluteY, absoluteZ));
        targetPanel.onClipMove(targetPanel);
    }
}

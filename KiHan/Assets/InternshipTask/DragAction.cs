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

    public DragAction(UIPanel uiPanel, Vector3 absolute, ulong timeStamp)
    {
        TimeStamp = timeStamp;
        UIPanelName = uiPanel.gameObject.name;
        absoluteX = absolute.x;
        absoluteY = absolute.y;
        absoluteZ = absolute.z;
    }

    public void execute()
    {
        Debug.Log("execute drag event");
        UIPanel targetPanel = GameObject.Find(UIPanelName).GetComponent<UIPanel>();
        GameObject.Find(UIPanelName).GetComponent<UIScrollView>().MoveAbsolute(new Vector3(absoluteX, absoluteY, absoluteZ));
        targetPanel.onClipMove(targetPanel);
    }
}

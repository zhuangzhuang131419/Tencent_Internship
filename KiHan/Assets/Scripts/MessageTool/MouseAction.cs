using UnityEngine;
using System;
using System.Collections.Generic;
using KH;

[Serializable]
public enum MouseType
{
    UIButton,
    UIEventListener,
    UILevelSelectChapterItem,
    UIMainPage,
    UIPlayerInteractMenuItem
}

/// <summary>
/// 记录一个鼠标的事件
/// </summary>
[Serializable]
public class MouseAction : Message, ICommand
{

    public string targetComponentInfo = null;
    private float PosX;
    private float PosY;
    private float PosZ;
    private MouseType mouseType;

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


    public MouseAction(UIButton component, ulong timeStamp)
    {
        this.targetComponentInfo = component.name;
        // Pos = GameObject.Find("UI Root").transform.TransformPoint(component.transform.localPosition);
        Pos = component.transform.position;
        TimeStamp = timeStamp;
        mouseType = MouseType.UIButton;
    }

    public MouseAction(UIEventListener component, ulong timeStamp)
    {
        this.targetComponentInfo = component.name;
        // Pos = GameObject.Find("UI Root").transform.TransformPoint(component.transform.localPosition);
        Pos = component.transform.position;
        Debug.Log("Construct Listen" + Pos);
        TimeStamp = timeStamp;
        mouseType = MouseType.UIEventListener;
    }

    public MouseAction(UILevelSelectChapterItem component, ulong timeStamp)
    {
        this.targetComponentInfo = component.name;
        // Pos = GameObject.Find("UI Root").transform.TransformPoint(component.transform.localPosition);
        Pos = component.transform.position;
        TimeStamp = timeStamp;
        mouseType = MouseType.UILevelSelectChapterItem;
    }

    public MouseAction(UIMainPage componnet, ulong timeStamp, UIPlayerBar.BtnDestination targetDes)
    {
        targetComponentInfo = targetDes.ToString();
        TimeStamp = timeStamp;
        mouseType = MouseType.UIMainPage;
    }

    public MouseAction(UIPlayerInteractMenuItem componnet, ulong timeStamp)
    {
        targetComponentInfo = componnet.name;
        TimeStamp = timeStamp;
        mouseType = MouseType.UIPlayerInteractMenuItem;
    }

    public MouseAction()
    {

    }

    /// <summary>
    /// 重新模拟执行该事件
    /// </summary>
    public void execute()
    {
        switch (mouseType)
        {
            case MouseType.UIButton:
                UIButton button = getTargetUIComponnet<UIButton>();
                EventDelegate.Execute(button.onClick);
                break;
            case MouseType.UIEventListener:
                UIEventListener listener = getTargetUIComponnet<UIEventListener>();
                listener.onClick(listener.gameObject);
                break;
            case MouseType.UILevelSelectChapterItem:
                getTargetUIComponnet<UILevelSelectChapterItem>().OnClick();
                break;
            case MouseType.UIMainPage:
                UnityEngine.Object.FindObjectOfType<UIMainPage>().OnClickButton((UIPlayerBar.BtnDestination)Enum.Parse(typeof(UIPlayerBar.BtnDestination), targetComponentInfo));
                break;
            case MouseType.UIPlayerInteractMenuItem:
                getTargetUIComponnet<UIPlayerInteractMenuItem>().OnClick();
                break;
        }
    }

    private T getTargetUIComponnet<T>() where T : MonoBehaviour
    {
        GameObject UIRoot = GameObject.Find("UI Root");
        List<T> components = new List<T>();
        foreach (var item in UnityEngine.Object.FindObjectsOfType<GameObject>())
        {
            //if (item.name == targetComponentInfo
            //    && (int)UIRoot.transform.TransformPoint(item.transform.localPosition).x == Pos.x
            //    && (int)UIRoot.transform.TransformPoint(item.transform.localPosition).y == Pos.y
            //    && (int)UIRoot.transform.TransformPoint(item.transform.localPosition).z == Pos.z
            //    )
            //{
            //    return item.GetComponent<T>();
            //}

            if (item.name == targetComponentInfo) 
            {
                components.Add(item.GetComponent<T>());
            }
        }

        if (components.Count > 1)
        {
            T targetComponent = components[0];
            float magnitude = int.MaxValue;
            foreach (var item in components)
            {
                if ((item.transform.position - Pos).magnitude < magnitude)
                {
                    magnitude = (item.transform.position - Pos).magnitude;
                    targetComponent = item;
                }
            }

            return targetComponent;
        }

        return components[0];
    }
}

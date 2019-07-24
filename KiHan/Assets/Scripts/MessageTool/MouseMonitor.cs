using UnityEngine;
using System.Collections;
using KH;

public class MouseMonitor : MonoBehaviour
{
    MessageManager msgManager;

    // Use this for initialization
    void Start()
    {
        msgManager = MessageManager.Instance;
    }

    // Update is called once per frame
    void Update()
    {

        //if (Input.GetMouseButtonDown(0))
        //{
        //    Vector2 v = Camera.main.ScreenToViewportPoint(Input.mousePosition);
        //    ulong timeStamp = RemoteModel.Instance.CurrentTime;
        //    leftMouseDown = new MouseEvent(v, MouseType.Left, timeStamp);

        //    // Debug.Log("序列化鼠标左键按下操作成功" + v.x + ", " + v.y + "RemoteModel" + timeStamp);
        //}

        //if (Input.GetMouseButtonDown(1))
        //{
        //    Vector2 v = Camera.main.ScreenToViewportPoint(Input.mousePosition);
        //    ulong timeStamp = RemoteModel.Instance.CurrentTime;
        //    rightMouseDown = new MouseEvent(v, MouseType.Right, timeStamp);

        //    // Debug.Log("序列化鼠标右键按下操作成功" + v.x + ", " + v.y + "RemoteModel" + timeStamp);
        //}

        //if (Input.GetMouseButtonUp(0))
        //{

        //    if (leftMouseDown != null)
        //    {
        //        Vector2 v = Camera.main.ScreenToViewportPoint(Input.mousePosition);
        //        if ((v - leftMouseDown.ViewportPos).sqrMagnitude < 0.0001f)
        //        {
        //            Debug.Log("序列化鼠标左键抬起操作成功" + v.x + ", " + v.y);
        //            leftMouseUp = new MouseEvent(v, MouseType.Left, RemoteModel.Instance.CurrentTime);
        //            MouseAction mouseAction = new MouseAction(leftMouseDown, leftMouseUp);
        //            msgManager.serializeToLocal(mouseAction, MessageManager.DEST_PATH_MOUSE_EVENT);
        //        }
        //    }
        //    else
        //    {
        //        Debuger.Log("有问题");
        //    }

        //    // 清空
        //    leftMouseDown = null;
        //    leftMouseUp = null;
        //}

        //if (Input.GetMouseButtonUp(1))
        //{
        //    Vector2 v = Camera.main.ScreenToViewportPoint(Input.mousePosition);
        //    rightMouseUp = new MouseEvent(v, MouseType.Right, RemoteModel.Instance.CurrentTime);
        //    // Debug.Log("序列化鼠标左键抬起操作成功" + Camera.main.ScreenToViewportPoint(Input.mousePosition));
        //    if (rightMouseDown != null)
        //    {
        //        MouseAction mouseAction = new MouseAction(rightMouseDown, rightMouseUp);
        //        msgManager.serializeToLocal(mouseAction, MessageManager.DEST_PATH_MOUSE_EVENT);
        //    }
        //    else
        //    {
        //        Debug.Log("有问题");
        //    }

        //    // 清空
        //    rightMouseDown = null;
        //    rightMouseUp = null;
        //}


        if (MessageManager.Instance.dragActionsCache.Count > 0)
        {
            if (MessageManager.Instance.dragActionsCache[0].TimeStamp < RemoteModel.Instance.CurrentTime)
            {
                MessageManager.Instance.serializeToLocal(MessageManager.Instance.dragActionsCache[0], MessageManager.DEST_PATH_DRAG_EVENT);
                MessageManager.Instance.dragActionsCache.Clear();
            }
        }
    }

    private Vector2 Vector3ToVector2(Vector3 v)
    {
        return new Vector2(v.x, v.y);
    }
}

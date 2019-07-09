using UnityEngine;
using System.Collections.Generic;
using KH;
using System.Threading;

public class MouseActionManager : MonoBehaviour {

	// Use this for initialization
	void Start () {

        //MouseAction mouseAct1 = MessageManager.Instance.deserializeFromLocalByTimeStamp<MouseAction>(MessageManager.DEST_PATH_MOUSE_EVENT, 1562480407);
        //if (mouseAct1 != null)
        //{
        //    mouseAct1.execute();
        //    Debug.Log("点击事件1结束");
        //}
        //else
        //{
        //    Debug.Log("13");
        //}






        //MouseAction mouseAct3 = (MouseAction)MessageManager.Instance.deserializeFromLocalByTimeStamp(MessageManager.DEST_PATH_MOUSE_EVENT, 1562416617);
        //if (mouseAct3 != null)
        //{
        //    mouseAct3.execute();
        //}
        //else
        //{
        //    Debug.Log("17");
        //}

        //MouseAction mouseAct4 = (MouseAction)MessageManager.Instance.deserializeFromLocalByTimeStamp(MessageManager.DEST_PATH_MOUSE_EVENT, 1562416622);
        //if (mouseAct4 != null)
        //{
        //    mouseAct4.execute();
        //}
        //else
        //{
        //    Debug.Log("22");
        //}

    }

    // Update is called once per frame
    void Update () {
        ulong timeStamp = RemoteModel.Instance.CurrentTime;
        List<MouseAction> mouseActs = MessageManager.Instance.deserializeFromLocalByTimeStamp<MouseAction>(MessageManager.DEST_PATH_MOUSE_EVENT, RemoteModel.Instance.CurrentTime);
        List<DragEvent> dragActs = MessageManager.Instance.deserializeFromLocalByTimeStamp<DragEvent>(MessageManager.DEST_PATH_DRAG_EVENT, RemoteModel.Instance.CurrentTime);
        if (mouseActs != null)
        {
            foreach (var action in mouseActs)
            {
                action.execute();
            }
            
        }
        
        if (dragActs != null)
        {
            foreach (var action in dragActs)
            {
                action.execute();
            }
        }
    }
}

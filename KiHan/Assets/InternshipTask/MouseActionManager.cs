using UnityEngine;
using System.Collections.Generic;
using KH;
using System.Threading;

public class MouseActionManager : Singleton<MouseActionManager>
{

	// Use this for initialization
	void Start () {

    }

    // Update is called once per frame
    void Update () {
        ulong timeStamp = RemoteModel.Instance.CurrentTime;
        List<MouseAction> mouseActs = MessageManager.Instance.deserializeFromLocalByTimeStamp<MouseAction>(MessageManager.DEST_PATH_MOUSE_EVENT, RemoteModel.Instance.CurrentTime);
        List<DragAction> dragActs = MessageManager.Instance.deserializeFromLocalByTimeStamp<DragAction>(MessageManager.DEST_PATH_DRAG_EVENT, RemoteModel.Instance.CurrentTime);
        List<SwipeAction> swipeActs = MessageManager.Instance.deserializeFromLocalByTimeStamp<SwipeAction>(MessageManager.DEST_PATH_DRAG_EVENT, RemoteModel.Instance.CurrentTime);
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

        if (swipeActs != null)
        {
            foreach (var action in swipeActs)
            {
                action.execute();
            }
        }
    }
}

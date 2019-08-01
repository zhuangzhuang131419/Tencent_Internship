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

using UnityEngine;
using System.Collections;
using KH;
using System;

[Serializable]
public class SwipeAction : Message, ICommand {

    private float destX;
    private float deltaSpeedX;

    public SwipeAction(float destX, float deltaSpeedX, ulong timeStamp)
    {
        this.destX = destX;
        TimeStamp = timeStamp;
        this.deltaSpeedX = deltaSpeedX;
    }

    public void execute()
    {
        MainUICamera.getInstance().DeltaSpeedX = deltaSpeedX;
        MainUICamera.getInstance().moveTo(destX, 0);
        MainUICamera.getInstance().IsmNewMoveEvt = true;
    }
}

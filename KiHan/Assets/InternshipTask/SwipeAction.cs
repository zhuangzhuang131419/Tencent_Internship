using UnityEngine;
using System.Collections;
using KH;
using System;

[Serializable]
public class SwipeAction : Message, ICommand {

    private float destX = 0;
    private float deltaSpeedX;

    private float distanceX = 0;

    private float destPosX;
    private float destPosY;
    private float destPosZ;

    public Vector3 DestPos
    {
        get
        {
            return new Vector3(destPosX, destPosY, destPosZ);
        }
        set
        {
            destPosX = value.x;
            destPosY = value.y;
            destPosZ = value.z;
        }
    }

    public SwipeAction(float destX, float distanceX, float deltaSpeedX, ulong timeStamp)
    {
        this.destX = destX;
        TimeStamp = timeStamp;
        this.deltaSpeedX = deltaSpeedX;
        this.distanceX = distanceX;
    }

    public SwipeAction(Vector3 destPos, ulong timeStamp)
    {
        DestPos = destPos;
        TimeStamp = timeStamp;
    }

    public void execute()
    {

        //if (destX != 0)
        //{
        //    MainUICamera.getInstance().DeltaSpeedX = deltaSpeedX;
        //    MainUICamera.getInstance().moveTo(destX, 0);
        //    MainUICamera.getInstance().IsmNewMoveEvt = true;
        //}

        //if (distanceX != 0)
        //{
        //    MainUICamera.getInstance().DeltaSpeedX = deltaSpeedX;
        //    MainUICamera.getInstance().move(distanceX, 0);
        //}
        MainUICamera.getInstance().DestPos = DestPos;
        MainUICamera.getInstance().calculatePositionPercent();


    }
}

using System;
using UnityEditor;
using UnityEngine;


[Serializable]
public struct TransformPosition
{
    float x;
    float y;
    float z;

    public TransformPosition(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public TransformPosition(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }

    public float X
    {
        get { return x; }
        set { x = value; }
    }

    public float Y
    {
        get { return y; }
        set { y = value; }
    }

    public float Z
    {
        get { return z; }
        set { z = value; }
    }
}

using UnityEngine;
using System.Collections;
using System;
using UnityEditor;
using System.Runtime.InteropServices;

public class MouseSimulator {

    #region DLLs
    [DllImport("user32.dll")]
    public static extern int SetCursorPos(int x, int y);
    [DllImport("user32.dll")]
    public static extern int GetCursorPos(out int x, out int y);
    [DllImport("user32.dll")]
    private static extern void mouse_event(MouseEventFlag dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

    private static Vector2 offset = new Vector2(-1, -1);

    // 方法参数说明
    // VOID mouse_event(
    //     DWORD dwFlags,         // motion and click options
    //     DWORD dx,              // horizontal position or change
    //     DWORD dy,              // vertical position or change
    //     DWORD dwData,          // wheel movement
    //     ULONG_PTR dwExtraInfo  // application-defined information
    // );

    [Flags]
    enum MouseEventFlag : uint
    {
        Move = 0x0001,
        LeftDown = 0x0002,
        LeftUp = 0x0004,
        RightDown = 0x0008,
        RightUp = 0x0010,
        MiddleDown = 0x0020,
        MiddleUp = 0x0040,
        XDown = 0x0080,
        XUp = 0x0100,
        Wheel = 0x0800,
        VirtualDesk = 0x4000,
        Absolute = 0x8000
    }
    #endregion

    // Unity屏幕坐标从左下角开始，向右为X轴，向上为Y轴
    // Windows屏幕坐标从左上角开始，向右为X轴，向下为Y轴

    /// <summary>
    /// 移动鼠标到指定位置（使用视口坐标）
    /// </summary>
    public static bool MoveTo(double x, double y)
    {


        //if (!UnityEngine.Screen.fullScreen)
        //{
        //    UnityEngine.Debug.LogError("只能在全屏状态下使用！");
        //    return false;
        //}
        int curX;
        int curY;
        if (offset.x == -1 && offset.y == -1)
        {
            GetCursorPos(out curX, out curY);
            offset = new Vector2(curX - Input.mousePosition.x, curY + Input.mousePosition.y);
        }
        Debug.Log("offset" + offset);
        Debug.Log("Input" + Input.mousePosition.x + ", " + Input.mousePosition.y);
        Vector3 screePos = Camera.main.ViewportToScreenPoint(new Vector3((float)x, (float)y, 0));
        SetCursorPos((int)(screePos.x + offset.x), (int)(- screePos.y + offset.y));
        Debug.Log("moveTo" + (int)(screePos.x + offset.x) + ", " + (int)(-screePos.y + offset.y));
        return true;
    }

    // 左键单击
    public static void LeftClick(double x = -1, double y = -1)
    {
        if (MoveTo(x, y))
        {
            mouse_event(MouseEventFlag.LeftDown, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MouseEventFlag.LeftUp, 0, 0, 0, UIntPtr.Zero);
        }
    }

    // 右键单击
    public static void RightClick(float x = -1, float y = -1)
    {
        if (MoveTo(x, y))
        {
            mouse_event(MouseEventFlag.RightDown, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MouseEventFlag.RightUp, 0, 0, 0, UIntPtr.Zero);
        }
    }

    // 中键单击
    public static void MiddleClick(float x = -1, float y = -1)
    {
        if (MoveTo(x, y))
        {
            mouse_event(MouseEventFlag.MiddleDown, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MouseEventFlag.MiddleUp, 0, 0, 0, UIntPtr.Zero);
        }
    }

    // 左键按下
    public static void LeftDown(float x = -1, float y = -1)
    {
        if (MoveTo(x, y))
        {
            mouse_event(MouseEventFlag.LeftDown, 0, 0, 0, UIntPtr.Zero);
        }
    }

    // 左键抬起
    public static void LeftUp(float x = -1, float y = -1)
    {
        if (MoveTo(x, y))
        {
            Debug.Log(x + ", " + y + "左键抬起");
            mouse_event(MouseEventFlag.LeftUp, 0, 0, 0, UIntPtr.Zero);
        }
    }

    // 右键按下
    public static void RightDown(float x = -1, float y = -1)
    {
        if (MoveTo(x, y))
        {
            mouse_event(MouseEventFlag.RightDown, 0, 0, 0, UIntPtr.Zero);
        }
    }

    // 右键抬起
    public static void RightUp(float x = -1, float y = -1)
    {
        if (MoveTo(x, y))
        {
            mouse_event(MouseEventFlag.RightUp, 0, 0, 0, UIntPtr.Zero);
        }
    }

    // 中键按下
    public static void MiddleDown(float x = -1, float y = -1)
    {
        if (MoveTo(x, y))
        {
            mouse_event(MouseEventFlag.MiddleDown, 0, 0, 0, UIntPtr.Zero);
        }
    }

    // 中键抬起
    public static void MiddleUp(float x = -1, float y = -1)
    {
        if (MoveTo(x, y))
        {
            mouse_event(MouseEventFlag.MiddleUp, 0, 0, 0, UIntPtr.Zero);
        }
    }

    // 滚轮滚动
    public static void ScrollWheel(float value)
    {
        mouse_event(MouseEventFlag.Wheel, 0, 0, (uint)value, UIntPtr.Zero);
    }
}

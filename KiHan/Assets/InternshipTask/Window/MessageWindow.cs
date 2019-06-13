using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System;

public class MessageWindow : EditorWindow {

    string message = "Map prefab 不存在";

    void OnGUI()
    {
        // 显示通知信息
        GUILayout.Label(message);

        //打开按钮
        if (GUI.Button(new Rect(60, 180, 100, 30), "确定"))
        {
            Close();
        }
    }
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System;

public class CreateWindow : EditorWindow {

    string MapPrefabID = "";
    string MapID = "";
    

    void OnGUI()
    {
        //在弹出窗口中控制变量
        MapPrefabID = EditorGUILayout.TextField("MapPrefab ID:", MapPrefabID);
        MapID = EditorGUILayout.TextField("地图ID", MapID);

        //打开按钮
        if (GUI.Button(new Rect(60, 180, 100, 30), "创建"))
        {
            OnCreatePress();
        }
    }

    // 添加监听事件
    void OnCreatePress()
    {
        Debug.Log("Create On Pressed");
    }
}

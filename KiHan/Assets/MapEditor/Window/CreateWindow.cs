using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System;

public class CreateWindow : EditorWindow {

    private string mapPrefabID = "";
    private string mapID = "";
    
    public string MapPrefabID
    {
        set { mapPrefabID = value; }
    }

    public string MapID
    {
        set { mapID = value; }
    }

    void OnGUI()
    {
        //在弹出窗口中控制变量
        MapPrefabID = EditorGUILayout.TextField("MapPrefab ID:", mapPrefabID);
        mapID = EditorGUILayout.TextField("地图ID", mapID);

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
        bool isMapPrefabExist = false;
        bool isMapIDExist = false;

        // 检索Resources\Scene
        string targetMapPath = "";
        foreach (string path in Directory.GetFiles(MapEditor.MAP_PREFAB_ID_PATH))
        {
            //获取所有文件夹中包含后缀为 .prefab 的路径
            if (Path.GetExtension(path) == ".prefab" && (Path.GetFileNameWithoutExtension(path) == mapPrefabID))
            {
                isMapPrefabExist = true;
                targetMapPath = "Scene/" + mapPrefabID;
            }
        }

        // 检索Resources\Config\Map
        DirectoryInfo mapDir = new DirectoryInfo(MapEditor.MAP_ID_PATH);
        if (mapDir.Exists)
        {
            foreach (string path in Directory.GetFiles(MapEditor.MAP_ID_PATH))
            {
                if (Path.GetFileNameWithoutExtension(path) == mapID)
                {
                    isMapIDExist = true;
                }
            }
        }

        if (!isMapPrefabExist)
        {
            // 弹出提示信息
            MessageWindow.CreateMessageBox(
                "Map prefab 不存在",
                delegate (EditorWindow window) { window.Close(); },
                delegate (EditorWindow window) { window.Close(); }
            );
        }
        else if (isMapIDExist)
        {
            // 是否打开地图
            MessageWindow.CreateMessageBox(
                "地图ID已存在，是否打开地图",
                delegate (EditorWindow window)
                {
                    window.Close();
                    Close();
                    OpenWindow openWindow = CreateInstance<OpenWindow>();
                    openWindow.MapPrefabID = mapPrefabID;
                    openWindow.MapID = mapID;
                    openWindow.Show();
                },
                delegate (EditorWindow window) { window.Close(); }
            );
        }
        else
        {
            MapEditor.loadMap(targetMapPath, mapID);
        }
        Close();
    }
}

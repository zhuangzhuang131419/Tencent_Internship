using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System;

public class OpenWindow : EditorWindow {

    private string mapPrefabID = "10102";
    private string mapID = "10102";

    public string MapPrefabID
    {
        set { mapPrefabID = value; }
    }

    public string MapID
    {
        set { mapID = value; }
    }

    // Use this for initialization
    void Start () {

	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnGUI()
    {
        mapPrefabID = EditorGUILayout.TextField("MapPrefab ID:", mapPrefabID);
        mapID = EditorGUILayout.TextField("地图ID", mapID);

        //打开按钮
        if (GUI.Button(new Rect(60, 180, 100, 30), "打开"))
        {
            OnOpenPress();
        }
    }

    // 添加监听事件
    void OnOpenPress()
    {
        Debug.Log("Comfirm On Pressed");
        SearchRelatedPrefab();
    }


    private void SearchRelatedPrefab()
    {
        bool isMapPrefabExist = false;
        bool isMapIDExist = false;
        try
        {

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

            if (isMapIDExist && isMapPrefabExist)
            {
                MapEditor.loadMap(targetMapPath, mapID);
                Close();
            }
            else if (!isMapPrefabExist)
            {
                // 弹出提示信息
                MessageWindow messageWindow = CreateInstance<MessageWindow>();
                messageWindow.TitleText = "Map prefab 不存在";
                messageWindow.Comfirm = delegate(EditorWindow window) { window.Close(); };
                messageWindow.Cancel = delegate(EditorWindow window) { window.Close(); };
                messageWindow.Show();
            }
            else
            {
                // 提示是否新建地图
                MessageWindow messageWindow = CreateInstance<MessageWindow>();
                messageWindow.TitleText = "是否新建地图";
                messageWindow.Comfirm = delegate (EditorWindow window) 
                {
                    Directory.CreateDirectory(MapEditor.MAP_ID_PATH);
                    Directory.CreateDirectory(MapEditor.MAP_ID_PATH + "\\" + mapID);
                    window.Close();


                    CreateWindow createWindow = CreateInstance<CreateWindow>();
                    createWindow.MapPrefabID = mapPrefabID;
                    createWindow.MapID = mapID;
                    createWindow.Show();
                    
                };
                messageWindow.Cancel = delegate (EditorWindow window) { window.Close(); };
                messageWindow.Show();
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    void onWindow(int windowID)
    {
        Debug.Log("OnWindow");
    }
}

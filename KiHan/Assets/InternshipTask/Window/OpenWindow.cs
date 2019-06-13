using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System;

public class OpenWindow : EditorWindow {

    string MapPrefabID = "";
    string MapID = "";


    //bool groupEnabled = false;
    //bool myBool1 = true;
    //bool myBool2 = false;
    //float myFloat1 = 1.0f;
    //float myFloat2 = .5f;

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnGUI()
    {
        //在弹出窗口中控制变量
        MapPrefabID = EditorGUILayout.TextField("MapPrefab ID:", MapPrefabID);
        MapID = EditorGUILayout.TextField("地图ID", MapID);


        // myBool1 = EditorGUILayout.Toggle("Open Optional Settings", myBool1);
        // myFloat1 = EditorGUILayout.Slider("myFloat1", myFloat1, -3, 3);


        //创建一个GUILayout 通过groupEnabled 来控制当前GUILayout是否在Editor里面可以编辑
        //groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", groupEnabled);
        //myBool2 = EditorGUILayout.Toggle("myBool2", myBool2);
        //myFloat2 = EditorGUILayout.Slider("myFloat2", myFloat2, -3, 3);
        //EditorGUILayout.EndToggleGroup();

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
        GetDirs();
    }


    private void GetDirs()
    {
        bool isMapPrefabExist = false;
        try
        {
            string targetMapPath = "";
            foreach (string path in Directory.GetFiles(MapEditor.MAP_PREFAB_ID_PATH))
            {
                //获取所有文件夹中包含后缀为 .prefab 的路径
                if (Path.GetExtension(path) == ".prefab" && (Path.GetFileNameWithoutExtension(path) == MapPrefabID))
                {
                    isMapPrefabExist = true;
                    // targetMapPath = path.Substring(path.IndexOf("Scene//"));
                }
            }

            if (!isMapPrefabExist)
            {
                // 弹出提示信息
                GetWindow(typeof(MessageWindow));
            }
            else
            {
                // 加载Prefab资源
                Debug.Log("加载Prefab资源");
                // GameObject targetMap = (GameObject)Resources.Load(targetMapPath);
                GameObject targetMap = (GameObject)Resources.Load("Scene//10102.prefab");
                if (targetMap != null)
                {
                    Instantiate(targetMap);
                }
                else
                {
                    Debug.Log("Load失败");
                }
                
            }


            DirectoryInfo mapDir = new DirectoryInfo(MapEditor.MAP_ID_PATH);
            if (!mapDir.Exists)
            {
                Directory.CreateDirectory(MapEditor.MAP_ID_PATH);
                Debug.Log("是否新建地图");
            }
            else
            {
                foreach (string path in Directory.GetFiles(MapEditor.MAP_ID_PATH))
                {
                    //获取所有文件夹中包含后缀为 .prefab 的路径
                    if (Path.GetFileNameWithoutExtension(path) == MapID)
                    {
                        Debug.Log(isMapPrefabExist);
                    }
                }
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

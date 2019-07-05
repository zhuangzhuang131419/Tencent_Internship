using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using System.Xml;
using System;
public class UnitWindow : EditorWindow
{
    string unitID = "505013021";

    void OnGUI()
    {
        //在弹出窗口中控制变量
        unitID = EditorGUILayout.TextField("Unit ID:", unitID);

        //打开按钮
        if (GUI.Button(new Rect(60, 180, 100, 30), "创建Unit"))
        {
            OnCreatePress();
        }
    }

    void OnCreatePress()
    {
        // 检索UnitID
        XmlDocument xmlFile = new XmlDocument();
        // 暂时不用检索存在
        bool isXMLExist = true;

        //Debug.Log(unitID.Substring(0, unitID.Length - 3) + "_randbehemothsstatic.xml");
        //foreach (var xmlFilePath in Directory.GetFiles(MapEditor.NINJA_XML_PATH))
        //{
            
        //    if (xmlFilePath.Contains(unitID.Substring(0, unitID.Length - 3) + "_randbehemothsstatic.xml") && !xmlFilePath.Contains(".meta"))
        //    {
        //        try
        //        {
        //            xmlFile.Load(MapEditor.NINJA_XML_PATH + "/" + unitID.Substring(0, unitID.Length - 3) + "_randbehemothsstatic.xml");
        //            isXMLExist = true;
        //        }
        //        catch
        //        {
        //            Debug.LogWarning("路径无效");
        //        }
        //    }
        //}

        if (!isXMLExist)
        {
            MessageWindow.CreateMessageBox(
                "请输入正确的UnitID",
                delegate (EditorWindow window) { window.Close(); },
                delegate (EditorWindow window) { window.Close(); }
                );
        }
        else
        {
            Close();

   //         string staticID = null;

			//XmlNode root = xmlFile.SelectSingleNode("RandBehemothsStaticMap");
			//XmlNode all_infos = root.SelectSingleNode("all_infos");
   //         foreach (XmlNode item in all_infos.SelectNodes("item"))
   //         {
   //             if (item.SelectSingleNode("id").InnerText == unitID)
   //             {
   //                 Debug.Log("找到对应的id" + item.SelectSingleNode("staic_idx").InnerText);
   //                 staticID = item.SelectSingleNode("staic_idx").InnerText;
   //             }
   //         }

   //         if (staticID != null)
   //         {
   //             // 从Ninja表检索资源
                
   //         }

            // 自动生成index
            int maxIndex = -1;
            foreach (var unit in Selection.gameObjects[0].GetComponentsInChildren<Unit>())
            {
                if (unit.Index > maxIndex)
                {
                    maxIndex = unit.Index;
                }
            }

            GameObject newUnit = new GameObject();
            Unit unitComponent = newUnit.AddComponent<Unit>();

            // 默认数据
            unitComponent.Index = maxIndex + 1;
            unitComponent.Name = "MonsterGenerator" + unitComponent.Index;
            unitComponent.CreateAction = -1;
            unitComponent.Direction = -1;
            unitComponent.ID = int.Parse(unitID);
            

            newUnit.transform.parent = Selection.gameObjects[0].transform;

            // 添加数据
            // Selection.gameObjects[0].GetComponent<MapGenerator>().Units.Add(unitComponent);

            // 暂时写死加载40001
            // Debug.Log(MapEditor.ACTOR_PREFAB_PATH.Substring(MapEditor.ACTOR_PREFAB_PATH.IndexOf("Config")) + "/40001");
            GameObject actor = Instantiate(Resources.Load("Actor/40001")) as GameObject;
            actor.transform.parent = newUnit.transform;
            

            // 选中
            EditorGUIUtility.PingObject(newUnit);
            Selection.activeGameObject = newUnit;
            
        }
    }
}
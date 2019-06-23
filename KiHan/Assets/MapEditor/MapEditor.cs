using UnityEngine;
using UnityEditor;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Xml;

public class MapEditor : MonoBehaviour
{

    public static readonly string MAP_PREFAB_ID_PATH = "Assets/Resources/Scene";
    public static readonly string MAP_ID_PATH = "Assets/Resources/Config/Map";
    public static readonly string NINJA_XML_PATH = "Assets/EditorConfig/random_dungeon";
    public static readonly string ACTOR_PREFAB_PATH = "Assets/Resources/Actor";
    public static object mapDataPrefab = null;

    // public static Map currentMap;

    [MenuItem("MapEditor/打开")]
    public static void open()
    {
        if (FindObjectsOfType<MapData>().Length > 0)
        {
            MessageWindow.CreateMessageBox(
                "Mapdata已打开",
                delegate (EditorWindow window) { window.Close(); },
                delegate (EditorWindow window) { window.Close(); }
            );
        }
        else
        {
            EditorWindow.GetWindow(typeof(OpenWindow), true, "打开");
        }

    }

    [MenuItem("MapEditor/创建")]
    public static void create()
    {
        if (FindObjectsOfType<MapData>().Length > 0)
        {
            MessageWindow.CreateMessageBox(
                "Mapdata已打开",
                delegate (EditorWindow window) { window.Close(); },
                delegate (EditorWindow window) { window.Close(); }
            );
        }
        else
        {
            EditorWindow.GetWindow(typeof(CreateWindow), true, "创建");

        }
    }

    /// <summary>
    /// 保存对应的mapdata
    /// </summary>
    [MenuItem("MapEditor/保存")]
    public static void save()
    {
        // 保存新的mapData
        if (FindObjectsOfType<MapData>().Length != 1)
        {
            MessageWindow.CreateMessageBox(
                "mapdata数量异常，无法保存",
                delegate (EditorWindow window) { window.Close(); },
                delegate (EditorWindow window) { window.Close(); }
                );
            return;
        }
        MapData mapData = FindObjectOfType<MapData>();
        Debug.LogWarning(mapData.ID);

        // 更新DataStruct
        //mapData.MapGenerators.Clear();
        //foreach (MapGenerator generator in FindObjectsOfType<MapGenerator>())
        //{
        //    generator.Units.Clear();
        //    foreach (Unit unit in generator.GetComponentsInChildren<Unit>())
        //    {
        //        generator.Units.Add(unit);
        //    }
        //    Debug.Log("当前Generator有" + generator.Units.Count + "个Unit");
        //    mapData.MapGenerators.Add(generator);
        //}
        //Debug.Log("当前有" + mapData.MapGenerators.Count + "个Generator");



        // 检查数据
        if (!checkValidity()) { return; }

        // 清空原来的文件
        if (Directory.Exists(MAP_ID_PATH + "/" + mapData.ID + "/MapGenerator"))
        {
            foreach (var path in Directory.GetFiles(MAP_ID_PATH + "/" + mapData.ID + "/MapGenerator"))
            {
                File.Delete(path);
            }
        }
        else
        {
            Directory.CreateDirectory(MAP_ID_PATH + "/" + mapData.ID + "/MapGenerator");
        }

        
        // 依次保存每一个Generator

        foreach (MapGenerator generator in mapData.GetComponentsInChildren<MapGenerator>())
        {
            // saveWithSerialize(mapData, generator);
            saveWithXML(mapData, generator);
        }

        // 旧版本
        PrefabUtility.ReplacePrefab(mapData.gameObject, PrefabUtility.CreatePrefab(MAP_ID_PATH + "/" + mapData.ID + "/" + mapData.ID + ".prefab", mapData.gameObject), ReplacePrefabOptions.ConnectToPrefab);


        // 新版本使用
        // PrefabUtility.SaveAsPrefabAssetAndConnect(mapData.gameObject, MAP_ID_PATH + "/" + mapData.ID + "/" + mapData.ID + ".prefab", InteractionMode.AutomatedAction);
    }

    private static void saveWithXML(MapData mapData, MapGenerator generator)
    {
        // FileStream fileStream = new FileStream(MAP_ID_PATH + "/" + mapData.DataStruct.ID + "/MapGenerator/" + generatorDataStruct.ID + ".bytes", FileMode.Create, FileAccess.ReadWrite);
        XmlDocument xmlFile = new XmlDocument();
        XmlNode root = xmlFile.CreateElement("MonsterPackConfig");
        XmlNode packID = xmlFile.CreateElement("packID");
        XmlNode sceneResID = xmlFile.CreateElement("sceneResID");
        XmlNode jumpInNumFrame = xmlFile.CreateElement("jumpInNumFrame");
        XmlNode ary = xmlFile.CreateElement("ary");

        packID.InnerText = Convert.ToString(generator.ID);
        sceneResID.InnerText = Convert.ToString(mapData.ID);
        //  jumpInNumFrame.InnerText = "jumpInNumFrame?";

        xmlFile.AppendChild(root);
        root.AppendChild(packID);
        root.AppendChild(sceneResID);
        root.AppendChild(jumpInNumFrame);
        root.AppendChild(ary);

        foreach (var unit in generator.GetComponentsInChildren<Unit>())
        {
            XmlNode item = xmlFile.CreateElement("item");
            ary.AppendChild(item);

            jumpInNumFrame.InnerText = Convert.ToString(unit.CreateFrame);

            XmlNode actorID = xmlFile.CreateElement("actorID");
            XmlNode map_pos_x = xmlFile.CreateElement("map_pos_x");
            XmlNode map_pos_y = xmlFile.CreateElement("map_pos_y");
            XmlNode map_pos_z = xmlFile.CreateElement("map_pos_z");
            XmlNode defaultVKey = xmlFile.CreateElement("defaultVKey");
            XmlNode direction = xmlFile.CreateElement("direction");
            XmlNode delayCreateTime = xmlFile.CreateElement("delayCreateTime");
            XmlNode centerToPlayer = xmlFile.CreateElement("centerToPlayer");

            actorID.InnerText = Convert.ToString(unit.ID);
            map_pos_x.InnerText = ((long)(unit.transform.position.x * 10000)).ToString();
            map_pos_y.InnerText = ((long)(unit.CreateHeight * 10000)).ToString();
            map_pos_z.InnerText = ((long)(unit.transform.position.y * 10000)).ToString();
            defaultVKey.InnerText = Convert.ToString(unit.CreateAction);
            direction.InnerText = Convert.ToString(unit.Direction);
            delayCreateTime.InnerText = Convert.ToString(unit.DelayCreateTime);
            centerToPlayer.InnerText = Convert.ToString(unit.CenterToPlayer);


            item.AppendChild(actorID);
            item.AppendChild(map_pos_x);
            item.AppendChild(map_pos_y);
            item.AppendChild(map_pos_z);
            item.AppendChild(defaultVKey);
            item.AppendChild(direction);
            item.AppendChild(delayCreateTime);
            item.AppendChild(centerToPlayer);
        }

        xmlFile.Save(MAP_ID_PATH + "/" + mapData.ID + "/MapGenerator/" + generator.ID + ".bytes");
    }

    private static void saveWithSerialize(MapData mapData, MapGenerator generator)
    {
        FileStream fileStream = new FileStream(MAP_ID_PATH + "/" + mapData.ID + "/MapGenerator/" + generator.ID + ".dat", FileMode.Create, FileAccess.ReadWrite);
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(fileStream, generator);
        fileStream.Close();
    }

    /// <summary>
    /// 进行MapGenerator, Unit的index检查
    /// </summary>
    private static bool checkValidity()
    {
        MapData mapData = FindObjectOfType<MapData>();
        if (mapData == null) { return true; }
        HashSet<int> tempHashSet = new HashSet<int>();
        foreach (var generator in FindObjectsOfType<MapGenerator>())
        {
            if (!tempHashSet.Add(generator.Index))
            {
                // index already exist
                MessageWindow.CreateMessageBox(
                    generator.Name + "_" + generator.ID + "命名非法",
                    delegate (EditorWindow window) { window.Close(); },
                    delegate (EditorWindow window) { window.Close(); }
                );
                return false;
            }

            tempHashSet.Clear();
            foreach (var unit in generator.GetComponentsInChildren<Unit>())
            {
                if (!tempHashSet.Add(unit.Index))
                {
                    // index already exist
                    MessageWindow.CreateMessageBox(
                        unit.Name + "_" + unit.ID + "命名非法",
                        delegate (EditorWindow window) { window.Close(); },
                        delegate (EditorWindow window) { window.Close(); }
                    );
                    return false;
                }

            }
            tempHashSet.Clear();
        }
        return true;

    }

    [MenuItem("MapEditor/创建生成器")]
    public static void createGenerator()
    {
        Debug.Log("创建生成器");

        GameObject generatorRoot = GameObject.Find("MonsterGenerator");
        MapData mapData = FindObjectOfType<MapData>();
        if (generatorRoot != null)
        {
            // 自动生成index
            int maxIndex = -1;
            int maxID = int.Parse(Convert.ToSingle(FindObjectOfType<MapData>().ID) + "000");
            foreach (var generator in generatorRoot.GetComponentsInChildren<MapGenerator>())
            {
                if (generator.Index > maxIndex)
                {
                    maxIndex = generator.Index;
                }

                if (generator.ID > maxID)
                {
                    maxID = generator.ID;
                }
            }

            GameObject newGeneratorObject = new GameObject();
            newGeneratorObject.transform.parent = generatorRoot.transform;
            MapGenerator mapGenerator = newGeneratorObject.AddComponent<MapGenerator>();

            // 初始化新建的Generator
            mapGenerator.Index = maxIndex + 1;
            mapGenerator.ID = maxID + 1;
            mapGenerator.Name = "MapGenerator" + mapGenerator.ID;
            mapGenerator.Type = 1;

            // 添加数据
            // mapData.MapGenerators.Add(mapGenerator);

            // 选中
            EditorGUIUtility.PingObject(newGeneratorObject);
            Selection.activeGameObject = newGeneratorObject;
        }
        else
        {
            MessageWindow.CreateMessageBox(
                "请打开Map",
                delegate (EditorWindow window) { window.Close(); },
                delegate (EditorWindow window) { window.Close(); }
            );
        }
    }

    [MenuItem("MapEditor/创建单位")]
    public static void createUnit()
    {
        Debug.Log("创建单位");
        if (Selection.gameObjects.Length > 0
            && Selection.gameObjects[0].GetComponents<MapGenerator>() != null
            && Selection.gameObjects[0].GetComponents<MapGenerator>().Length > 0)
        {
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
            unitComponent.ID = 0;


            newUnit.transform.parent = Selection.gameObjects[0].transform;

            // 添加数据
            // Selection.gameObjects[0].GetComponent<MapGenerator>().Units.Add(unitComponent);

            // 暂时写死加载40001
            // Debug.Log(MapEditor.ACTOR_PREFAB_PATH.Substring(MapEditor.ACTOR_PREFAB_PATH.IndexOf("Config")) + "/40001");
            try
            {
                GameObject actor = Instantiate(Resources.Load("Actor/40001")) as GameObject;
                actor.transform.parent = newUnit.transform;
            }
            catch (ArgumentException)
            {
                Debug.Log("不存在要加载的对象");
            }
            


            // 选中
            EditorGUIUtility.PingObject(newUnit);
            Selection.activeGameObject = newUnit;
        }
        else
        {
            MessageWindow.CreateMessageBox(
                "请选择Generator",
                delegate (EditorWindow window) { window.Close(); },
                delegate (EditorWindow window) { window.Close(); }
            );
        }
    }



    /// <summary>
    /// 加载mapData
    /// </summary>
    /// <param name="mapID"></param>
    public static void loadMap(string targetMapPath, string mapID)
    {
        GameObject mapPrefab = (GameObject)Resources.Load(targetMapPath);
        Instantiate(mapPrefab);
        if (Directory.Exists(MAP_ID_PATH + "/" + mapID + "/MapGenerator"))
        {
            // 要根据文件内容来加
            // 加载保存的prefab
            UnityEngine.Object mapDataPrefab = AssetDatabase.LoadAssetAtPath("Assets/Resources/Config/Map/" + mapID + "/" + mapID + ".prefab", typeof(GameObject));
            GameObject mapDataObject = (GameObject)mapDataPrefab;
            // GameObject mapDataObject = (GameObject)Resources.Load("Config/Map/" + mapID + "/" + mapID);

            // PrefabUtility.InstantiatePrefab(mapDataObject);

            // 新版本
            // GameObject mapDataObject = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Config/Map/" + mapID + "/" + mapID + ".prefab");
            mapDataObject.name = "MapData_" + mapID;
            PrefabUtility.ReplacePrefab((GameObject)Instantiate(mapDataObject), mapDataPrefab, ReplacePrefabOptions.ConnectToPrefab);
            //Debug.LogWarning(FindObjectsOfType<MapGenerator>().Length);
            //Debug.LogWarning("Unit" + FindObjectsOfType<MapGenerator>()[0].GetComponentsInChildren<Unit>().Length);
            //Debug.LogWarning("MapGenerator0,unit0,x:" + FindObjectsOfType<MapGenerator>()[0].GetComponentsInChildren<Unit>()[0].transform.position.x);




            // Debug.LogWarning("MapGenerator0,unit0,x:" + FindObjectsOfType<MapGenerator>()[0].GetComponentsInChildren<Unit>()[0].transform.position.x);
            // FindObjectsOfType<MapGenerator>()[0].GetComponentsInChildren<Unit>()[0].transform.position = new Vector3(9, 0, 0);
            // setHieraychy(mapData);
        }
        else
        {
            InitializeHieraychy(mapID);
            InitializeMap(targetMapPath);
            Debug.Log("首次打开");
        }
    }

    /// <summary>
    /// // 初始化层级结构
    /// </summary>
    /// <param name="mapID"></param>
    /// <param name="mapData"></param>
    private static void InitializeHieraychy(string mapID)
    {
        GameObject mapDataObject = new GameObject("MapData_" + mapID);
        mapDataObject.AddComponent<MapData>().ID = int.Parse(mapID);
        GameObject mapInfo = new GameObject("MapInfo");
        mapInfo.transform.parent = mapDataObject.transform;
        GameObject mapGenerator = new GameObject("MapGenerator");
        mapGenerator.transform.parent = mapDataObject.transform;
        GameObject monsterGenerator = new GameObject("MonsterGenerator");
        monsterGenerator.transform.parent = mapGenerator.transform;

        // 绑定Prefab
        Directory.CreateDirectory(MAP_ID_PATH + "/" + mapID);

        // 旧版本
        PrefabUtility.ReplacePrefab(mapDataObject, PrefabUtility.CreatePrefab(MAP_ID_PATH + "/" + mapID + "/" + mapID + ".prefab", mapDataObject), ReplacePrefabOptions.ConnectToPrefab);

        // 新版本
        // PrefabUtility.SaveAsPrefabAssetAndConnect(mapDataObject, MAP_ID_PATH + "/" + mapID + "/" + mapID + ".prefab", InteractionMode.AutomatedAction);
    }

    public static void InitializeMap(string targetMapPath)
    {
        // 加载Prefab资源
        Debug.Log("targetPath: " + targetMapPath);
        loadActor();
    }

    private static void loadActor()
    {
        foreach (var generator in FindObjectsOfType<MapGenerator>())
        {
            foreach (var unit in generator.GetComponentsInChildren<Unit>())
            {
                // 暂时写死加载40001
                try
                {
                    GameObject actor = Instantiate(Resources.Load("Actor/40001")) as GameObject;
                    actor.transform.parent = unit.transform;
                    actor.transform.position = unit.transform.position;
                }
                catch (ArgumentException)
                {
                    Debug.Log("不存在要加载的对象");
                }
                
            }
        }
    }

    public static void refreshFromXML()
    {
        // 读取xml读取用户更新的数据
        string mapID = Convert.ToString(FindObjectOfType<MapData>().ID);
        foreach (MapGenerator generator in FindObjectsOfType<MapGenerator>())
        {
            // 依次加载每一个generator
            Debug.Log(generator.Name);
            XmlDocument xmlFile = new XmlDocument();
            FileStream fileStream = new FileStream(MAP_ID_PATH + "/" + mapID + "/MapGenerator/" + generator.ID + ".bytes", FileMode.Open, FileAccess.Read);
            try
            {
                xmlFile.Load(fileStream);
            }
            catch (XmlException e)
            {
                Debug.LogWarning("加载出错" + e.Message);
            }
            fileStream.Close();


            int index = 0;
            foreach (XmlNode item in xmlFile.SelectSingleNode("MonsterPackConfig").SelectSingleNode("ary").SelectNodes("item"))
            {

                // 更新数据
                if (generator.GetComponentsInChildren<Unit>().Length <= index)
                {
                    return;
                }
                Unit unit = generator.GetComponentsInChildren<Unit>()[index];
                Debug.Log(unit.Name);
                unit.ID = int.Parse(item.SelectSingleNode("actorID").InnerText);
                unit.transform.position = new Vector3(
                    float.Parse(item.SelectSingleNode("map_pos_x").InnerText) / 10000,
                    unit.CreateHeight / 10000,
                    float.Parse(item.SelectSingleNode("map_pos_y").InnerText) / 10000
                    );
                unit.CreateAction = int.Parse(item.SelectSingleNode("defaultVKey").InnerText);
                unit.Direction = int.Parse(item.SelectSingleNode("direction").InnerText);
                unit.DelayCreateTime = int.Parse(item.SelectSingleNode("delayCreateTime").InnerText);
                unit.CenterToPlayer = int.Parse(item.SelectSingleNode("centerToPlayer").InnerText);
                index++;
            }

            // PrefabUtility.ReplacePrefab(mapDataObject, PrefabUtility.CreatePrefab(MAP_ID_PATH + "/" + mapID + "/" + mapID + ".prefab", mapDataObject), ReplacePrefabOptions.ConnectToPrefab);
            // mapDataObject = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Resources/Config/Map/" + mapID + "/" + mapID + ".prefab", typeof(GameObject));

            // mapData.MapGenerators.Add(generator);
        }
    }

  //  [MenuItem("MapEditor/test")]
  //  public static void test()
  //  {
  //      Debug.LogWarning("MapGenerator0,unit0,x:" + FindObjectsOfType<MapGenerator>()[0].GetComponentsInChildren<Unit>()[0].transform.position.x);

  //      /*
  //      GameObject mapData = new GameObject("map");
  //      mapData.AddComponent<MapData>().Desc = "测试";
  //      mapData.AddComponent<MapGenerator>().transform.position = new Vector3(7, 0, 0);
  //      PrefabUtility.ReplacePrefab(mapData, PrefabUtility.CreatePrefab("Assets/test.prefab", mapData), ReplacePrefabOptions.ConnectToPrefab);
  //      // PrefabUtility.SaveAsPrefabAssetAndConnect(mapData, "Assets/test.prefab", InteractionMode.AutomatedAction);
  //      */
  //  }

  //  [MenuItem("MapEditor/test1")]
  //  public static void test1()
  //  {
		//Debug.LogWarning("MapGenerator0,unit0,x:" + FindObjectsOfType<MapGenerator>()[0].GetComponentsInChildren<Unit>()[0].transform.position.x);
  //      Debug.Log(((GameObject)AssetDatabase.LoadAssetAtPath("Assets/test.prefab", typeof(GameObject))).GetComponent<MapData>().Desc);
  //      Debug.Log(((GameObject)AssetDatabase.LoadAssetAtPath("Assets/test.prefab", typeof(GameObject))).GetComponent<MapGenerator>().transform.position);
  //      Debug.Log(FindObjectsOfType<MapGenerator>()[0].GetComponentsInChildren<Unit>()[0].Name);
  //      FindObjectsOfType<MapGenerator>()[0].GetComponentsInChildren<Unit>()[0].transform.position = new Vector3(3, 0, 0);
  //  }
}

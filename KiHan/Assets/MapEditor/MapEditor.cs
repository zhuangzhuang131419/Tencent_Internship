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
    private static Dictionary<MapGenerator, List<Unit>> cacheMapData = new Dictionary<MapGenerator, List<Unit>>();

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


    [MenuItem("MapEditor/导入")]
    public static void load()
    {
        if (FindObjectsOfType<MapData>().Length == 0)
        {
            MessageWindow.CreateMessageBox(
                "请打开Mapdata",
                delegate (EditorWindow window) { window.Close(); },
                delegate (EditorWindow window) { window.Close(); }
            );
        }
        else
        {
            MapData mapData = FindObjectOfType<MapData>();
            // 旧版本
            PrefabUtility.DisconnectPrefabInstance(mapData.gameObject);

            // 新版本
            // PrefabUtility.UnpackPrefabInstance(mapData.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (MapGenerator generator in Resources.FindObjectsOfTypeAll<MapGenerator>())
            {
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(generator)))
                {
                    DestroyImmediate(generator.gameObject);
                }
            }
            Debug.Log("销毁成功");
            

            // 读取新的数据
            if (Directory.Exists(MAP_ID_PATH + "/" + mapData.ID + "/MapGenerator"))
            {
                refreshFromXML();
            }
            else
            {
                MessageWindow.CreateMessageBox(
                    "没有可供导入的数据",
                    delegate (EditorWindow window) { window.Close(); },
                    delegate (EditorWindow window) { window.Close(); }
                );
            }
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


        // 一键修复数据
        autoFix();

        Debug.Log("当前有" + cacheMapData.Count + "个Generator");

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




        foreach (MapGenerator generator in cacheMapData.Keys)
        {
            saveWithXML(mapData, generator);
        }

        // 旧版本
        PrefabUtility.ReplacePrefab(mapData.gameObject, PrefabUtility.CreatePrefab(MAP_ID_PATH + "/" + mapData.ID + "/" + mapData.ID + ".prefab", mapData.gameObject), ReplacePrefabOptions.ConnectToPrefab);

        MessageWindow.CreateMessageBox(
            "保存成功",
            delegate (EditorWindow window) { window.Close(); },
            delegate (EditorWindow window) { window.Close(); }
        );
        // 新版本使用
        // PrefabUtility.SaveAsPrefabAssetAndConnect(mapData.gameObject, MAP_ID_PATH + "/" + mapData.ID + "/" + mapData.ID + ".prefab", InteractionMode.AutomatedAction);
    }

    private static void loadHierarchyInfoToMapDataCache()
    {
        // 依次保存每一个Generator
        cacheMapData.Clear();
        foreach (MapGenerator generator in Resources.FindObjectsOfTypeAll<MapGenerator>())
        {
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(generator)))
            {
                cacheMapData.Add(generator, new List<Unit>());
                foreach (Unit unit in Resources.FindObjectsOfTypeAll<Unit>())
                {
                    if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(unit)) && unit.transform.parent == generator.transform)
                    {
                        cacheMapData[generator].Add(unit);
                    }
                }
                cacheMapData[generator].Reverse();
            }
        }
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

        xmlFile.AppendChild(root);
        root.AppendChild(packID);
        root.AppendChild(sceneResID);
        root.AppendChild(jumpInNumFrame);
        root.AppendChild(ary);

        // Debug.Log(generator.name + "有" + cacheMapData[generator].Count + "个Unit");
        foreach (var unit in cacheMapData[generator])
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
            map_pos_x.InnerText = Convert.ToDecimal(unit.transform.position.x * 10000).ToString();
            map_pos_y.InnerText = Convert.ToDecimal(unit.CreateHeight * 10000).ToString();
            map_pos_z.InnerText = Convert.ToDecimal(unit.transform.position.y * 10000).ToString();
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

    /// <summary>
    /// 进行MapGenerator, Unit的index检查[已弃用]
    /// </summary>
    private static bool checkValidity()
    {
        MapData mapData = FindObjectOfType<MapData>();
        if (mapData == null) { return true; }
        HashSet<int> tempHashSet = new HashSet<int>();
        Debug.LogWarning("当前有" + FindObjectsOfType<MapGenerator>().Length + "个MapGenerator");
        foreach (var generator in FindObjectsOfType<MapGenerator>())
        {
            if (!tempHashSet.Add(generator.Index))
            {
                // index already exist
                MessageWindow.CreateMessageBox(
                    generator.Name + "_" + generator.ID + "命名非法, 是否一键修复",
                    delegate (EditorWindow window)
                    {
                        // 一键修复index
                        int i = FindObjectsOfType<MapGenerator>().Length;
                        foreach (var item in FindObjectsOfType<MapGenerator>())
                        {
                            item.Index = --i;
                        }
                        save();
                        window.Close();
                    },
                    delegate (EditorWindow window) { window.Close(); }
                );


                return false;
            }
        }

        foreach (var generator in FindObjectsOfType<MapGenerator>())
        {
            tempHashSet.Clear();
            foreach (var unit in generator.GetComponentsInChildren<Unit>())
            {
                if (!tempHashSet.Add(unit.Index))
                {

                    // index already exist
                    MessageWindow.CreateMessageBox(
                        unit.Name + "_" + unit.ID + "命名非法, 是否一键修复",
                        delegate (EditorWindow window)
                        {
                            // 一键修复index
                            int i = 0;
                            foreach (var item in generator.GetComponentsInChildren<Unit>())
                            {
                                item.Index = i++;
                            }
                            save();
                            window.Close();
                        },
                        delegate (EditorWindow window) { window.Close(); }
                    );

                    return false;
                }

            }
            tempHashSet.Clear();
        }
        return true;
    }

    public static void autoFix()
    {
        MapData mapData = FindObjectOfType<MapData>();
        if (mapData == null) { return; }
        loadHierarchyInfoToMapDataCache();
        Dictionary<int, MapGenerator> tempGenerators = new Dictionary<int, MapGenerator>();

        int maxGeneratorIndex = 0;
        foreach (var generator in cacheMapData.Keys)
        {
            if (generator.Index > maxGeneratorIndex)
            {
                maxGeneratorIndex = generator.Index;
            }
        }

        foreach (var generator in cacheMapData.Keys)
        {
            if (tempGenerators.ContainsKey(generator.Index))
            {
                MapGenerator newGenerator;
                tempGenerators.TryGetValue(generator.Index, out newGenerator);
                newGenerator.Index = maxGeneratorIndex + 1;
                tempGenerators.Add(newGenerator.Index, newGenerator);
                tempGenerators[generator.Index] = generator;
                maxGeneratorIndex++;
            }
            else
            {
                tempGenerators.Add(generator.Index, generator);
            }
        }

        foreach (var generator in cacheMapData.Keys)
        {
            int maxUnitIndex = 0;
            foreach (var unit in cacheMapData[generator])
            {
                if (unit.Index > maxUnitIndex)
                {
                    maxUnitIndex = unit.Index;
                }
            }

            Dictionary<int, Unit> tempUnits = new Dictionary<int, Unit>();
            foreach (var unit in cacheMapData[generator])
            {
                if (tempUnits.ContainsKey(unit.Index))
                {
                    unit.Index = maxUnitIndex + 1;
                    tempUnits.Add(unit.Index, unit);
                    maxUnitIndex++;
                }
                else
                {
                    tempUnits.Add(unit.Index, unit);
                }
            }
        }
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
            int maxIndex = 0;
            int maxID = FindObjectOfType<MapData>().ID * 1000;
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

            MapGenerator mapGenerator = InstantializeGenerator(maxIndex + 1, maxID + 1, generatorRoot);

            // 选中
            EditorGUIUtility.PingObject(mapGenerator.gameObject);
            Selection.activeGameObject = mapGenerator.gameObject;
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

    public static MapGenerator InstantializeGenerator(int index, int id, GameObject parent)
    {
        GameObject newGeneratorObject = new GameObject();
        newGeneratorObject.transform.parent = parent.transform;
        MapGenerator mapGenerator = newGeneratorObject.AddComponent<MapGenerator>();

        // 初始化新建的Generator
        mapGenerator.Index = index;
        mapGenerator.ID = id;
        mapGenerator.Name = "MapGenerator" + mapGenerator.ID;
        mapGenerator.Type = 1;

        return mapGenerator;
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
            int maxIndex = 0;
            foreach (var unit in Selection.gameObjects[0].GetComponentsInChildren<Unit>())
            {
                if (unit.Index > maxIndex)
                {
                    maxIndex = unit.Index;
                }
            }

            Unit unitComponent = InitializeUnit(maxIndex, Selection.gameObjects[0]);

            // 选中
            EditorGUIUtility.PingObject(unitComponent.gameObject);
            Selection.activeGameObject = unitComponent.gameObject;
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

    private static Unit InitializeUnit(int index, GameObject parent)
    {
        GameObject newUnit = new GameObject();
        Unit unitComponent = newUnit.AddComponent<Unit>();

        // 默认数据
        unitComponent.Index = index;
        unitComponent.Name = "MonsterGenerator" + unitComponent.Index;
        unitComponent.CreateAction = -1;
        unitComponent.Direction = -1;
        unitComponent.ID = 0;


        newUnit.transform.parent = parent.transform;

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
        return unitComponent;
    }


    /// <summary>
    /// 加载mapData
    /// </summary>
    /// <param name="mapID"></param>
    public static void loadMap(string targetMapPath, string mapID)
    {
        GameObject mapPrefab = (GameObject)Resources.Load(targetMapPath);
        // UnityEngine.Object mapPrefab = AssetDatabase.LoadAssetAtPath(targetMapPath + ".prefab", typeof(GameObject));
        // PrefabUtility.ReplacePrefab((GameObject)Instantiate(mapPrefab), mapPrefab, ReplacePrefabOptions.ConnectToPrefab);
        PrefabUtility.InstantiatePrefab(mapPrefab);
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

    /// <summary>
    /// 用于打开mapData刷新数据
    /// </summary>
    public static void refreshFromXML()
    {
        // 读取xml读取用户更新的数据
        string mapID = Convert.ToString(FindObjectOfType<MapData>().ID);
        loadGeneratorXML(mapID);
    }

    /// <summary>
    /// 加载xml里面generator的数据
    /// </summary>
    /// <param name="mapID"></param>
    private static void loadGeneratorXML(string mapID)
    {
        Dictionary<int, MapGenerator> generators = new Dictionary<int, MapGenerator>();
        foreach (MapGenerator generator in Resources.FindObjectsOfTypeAll<MapGenerator>())
        {
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(generator)))
            {
                generators.Add(generator.ID, generator);
            }
        }

        if (Directory.Exists(MAP_ID_PATH + "/" + mapID + "/MapGenerator"))
        {
            foreach (var path in Directory.GetFiles(MAP_ID_PATH + "/" + mapID + "/MapGenerator"))
            {
                if (Path.GetExtension(path) == ".bytes")
                {
                    // 依次加载每一个generator
                    XmlDocument xmlFile = new XmlDocument();
                    FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
                    try
                    {
                        xmlFile.Load(fileStream);
                    }
                    catch (XmlException e)
                    {
                        Debug.LogWarning("加载出错" + e.Message);
                    }
                    fileStream.Close();

                    Debug.Log("int.Parse(Path.GetFileNameWithoutExtension(path)):" + int.Parse(Path.GetFileNameWithoutExtension(path)));
                    if (!generators.ContainsKey(int.Parse(Path.GetFileNameWithoutExtension(path))))
                    {
                        MapGenerator mapGenerator = InstantializeGenerator(
                            generators.Count,
                            int.Parse(Path.GetFileNameWithoutExtension(path)),
                            GameObject.Find("MonsterGenerator"));
                        generators.Add(mapGenerator.ID, mapGenerator);
                    }
                    loadUnitToGeneratorXML(xmlFile, generators[int.Parse(Path.GetFileNameWithoutExtension(path))]);
                }
            }
        }
    }

    /// <summary>
    /// 把xml里每个unit的数据加载进mapGenerator
    /// </summary>
    /// <param name="xml"></param>
    /// <param name="mapGenerator"></param>
    private static void loadUnitToGeneratorXML(XmlDocument xmlFile, MapGenerator generator)
    {
        List<Unit> units = new List<Unit>();
        foreach (Unit unit in Resources.FindObjectsOfTypeAll<Unit>())
        {
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(unit)) && unit.transform.parent == generator.transform)
            {
                units.Add(unit);
            }
        }
        Debug.Log(generator.Index + "有" + units.Count + "个units");
        int index = 0;
        foreach (XmlNode item in xmlFile.SelectSingleNode("MonsterPackConfig").SelectSingleNode("ary").SelectNodes("item"))
        {

            // 更新数据
            if (units.Count <= index)
            {
                // item数量多于unit, 新建unit
                Unit newUnit = InitializeUnit(units.Count + 1, generator.gameObject);
                units.Add(newUnit);
            }

            units[index].ID = int.Parse(item.SelectSingleNode("actorID").InnerText);
            units[index].transform.position = new Vector3(
                float.Parse(item.SelectSingleNode("map_pos_x").InnerText) / 10000,
                units[index].transform.position.y,
                float.Parse(item.SelectSingleNode("map_pos_y").InnerText) / 10000
                );
            units[index].CreateAction = int.Parse(item.SelectSingleNode("defaultVKey").InnerText);
            units[index].Direction = int.Parse(item.SelectSingleNode("direction").InnerText);
            units[index].DelayCreateTime = int.Parse(item.SelectSingleNode("delayCreateTime").InnerText);
            units[index].CenterToPlayer = int.Parse(item.SelectSingleNode("centerToPlayer").InnerText);
            index++;
        }
    }

    //[MenuItem("MapEditor/test")]
    //public static void test()
    //{
    //    autoFix();

    //    /*
    //    GameObject mapData = new GameObject("map");
    //    mapData.AddComponent<MapData>().Desc = "测试";
    //    mapData.AddComponent<MapGenerator>().transform.position = new Vector3(7, 0, 0);
    //    PrefabUtility.ReplacePrefab(mapData, PrefabUtility.CreatePrefab("Assets/test.prefab", mapData), ReplacePrefabOptions.ConnectToPrefab);
    //    // PrefabUtility.SaveAsPrefabAssetAndConnect(mapData, "Assets/test.prefab", InteractionMode.AutomatedAction);
    //    */
    //}

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

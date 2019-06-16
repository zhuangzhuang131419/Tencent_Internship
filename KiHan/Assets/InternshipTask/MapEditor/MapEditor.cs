using UnityEngine;
using UnityEditor;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;
using System.Collections.Generic;
using System;

public class MapEditor : MonoBehaviour {

    public static readonly string MAP_PREFAB_ID_PATH = "Assets/Resources/Scene";
    public static readonly string MAP_ID_PATH = "Assets/Resources/Config/Map";
    private static GameObject mapPrefab = null;

    // public static Map currentMap;

    [MenuItem("MapEditor/打开")]
    public static void open()
    {
        EditorWindow.GetWindow(typeof(OpenWindow), true, "打开");
    }

    [MenuItem("MapEditor/创建")]
    public static void create()
    {
        EditorWindow.GetWindow(typeof(CreateWindow), true, "创建");
    }

    /// <summary>
    /// 保存对应的mapdata
    /// </summary>
    [MenuItem("MapEditor/保存")]
    public static void save()
    {
        
        // 检查数据
        if (!checkValidity()) { return; }

        // 通过检查，保存新的mapData
        MapData mapData = (MapData)FindObjectOfType(typeof(MapData));
        if (mapData == null)
        {
            Debug.Log("当前没有可以保存的");
            return;
        }

        Directory.CreateDirectory(MAP_ID_PATH + "/" + mapData.DataStruct.ID + "/MapGenerator");
        // 依次保存每一个Generator
        foreach (var generatorDataStruct in mapData.DataStruct.MapGenerators)
        {
            FileStream fileStream = new FileStream(MAP_ID_PATH + "/" + mapData.DataStruct.ID + "/MapGenerator/" + generatorDataStruct.ID + ".dat", FileMode.Create, FileAccess.ReadWrite);
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(fileStream, generatorDataStruct);
            fileStream.Close();
        }

        Debug.Log(MAP_ID_PATH + "/" + mapData.DataStruct.ID + "/" + mapData.DataStruct.ID + ".prefab");
        if (mapPrefab != null)
        {
            try
            {
                PrefabUtility.CreatePrefab(MAP_ID_PATH + "/" + mapData.DataStruct.ID + "/" + mapData.DataStruct.ID + ".prefab", mapPrefab);
            }
            catch (Exception e)
            {
                Debug.Log("出错了" + e.Message);
            }
        }
    }

    /// <summary>
    /// 进行MapGenerator, Unit的index检查
    /// </summary>
    private static bool checkValidity()
    {
        MapData mapData = FindObjectOfType<MapData>();
        if (mapData == null) { return true; }
        HashSet<int> tempHashSet = new HashSet<int>();
        foreach (var generator in mapData.DataStruct.MapGenerators)
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
            foreach (var unit in generator.Units)
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
            int maxID = int.Parse(Convert.ToSingle(FindObjectOfType<MapData>().DataStruct.ID) + "000");
            foreach (var generator in generatorRoot.GetComponentsInChildren<MapGenerator>())
            {
                if (generator.DataStruct.Index > maxIndex)
                {
                    maxIndex = generator.DataStruct.Index;
                }

                if (generator.DataStruct.ID > maxID)
                {
                    maxID = generator.DataStruct.ID;
                }
            }

            GameObject newGeneratorObject = new GameObject();
            newGeneratorObject.transform.parent = generatorRoot.transform;
            MapGenerator mapGenerator = newGeneratorObject.AddComponent<MapGenerator>();

            // 初始化新建的Generator
            mapGenerator.DataStruct.Index = maxIndex + 1;
            mapGenerator.DataStruct.ID = maxID + 1;
            mapGenerator.DataStruct.Name = "MapGenerator" + mapGenerator.DataStruct.ID;
            mapGenerator.DataStruct.Type = 1;

            // 添加数据
            mapData.DataStruct.MapGenerators.Add(mapGenerator.DataStruct);

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
            && Selection.gameObjects[0].GetComponents<MapGenerator>()!= null
            && Selection.gameObjects[0].GetComponents<MapGenerator>().Length > 0)
        {
            // 自动生成index
            int maxIndex = -1;
            foreach (var unit in Selection.gameObjects[0].GetComponentsInChildren<Unit>())
            {
                if (unit.DataStruct.Index > maxIndex)
                {
                    maxIndex = unit.DataStruct.Index;
                }
            }

            GameObject newUnit = new GameObject();
            Unit unitComponent = newUnit.AddComponent<Unit>();

            // 默认数据
            unitComponent.DataStruct.Index = maxIndex + 1;
            unitComponent.DataStruct.Name = "MonsterGenerator" + unitComponent.DataStruct.Index;
            unitComponent.DataStruct.CreateAction = -1;

            newUnit.transform.parent = Selection.gameObjects[0].transform;

            // 添加数据
            Selection.gameObjects[0].GetComponent<MapGenerator>().DataStruct.Units.Add(unitComponent.DataStruct);

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
    public static void loadMapData(string mapID)
    {
        MapData mapData;
        InitializeHieraychy(mapID, out mapData);

        if (Directory.Exists(MAP_ID_PATH + "/" + mapID))
        {
            // 要根据文件内容来加载

            // 依次加载每一个generator
            foreach (var path in Directory.GetFiles(MAP_ID_PATH + "/" + mapID + "/MapGenerator"))
            {
                // 跳过以.mata结尾的
                if (path.IndexOf(".meta") == -1)
                {
                    FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);
                    BinaryFormatter bf = new BinaryFormatter();
                    mapData.DataStruct.MapGenerators.Add(bf.Deserialize(fileStream) as MapGeneratorDataStruct);
                    fileStream.Close();
                }
            }

            mapData.DataStruct.ID = int.Parse(mapID);
            setHieraychy(mapData);
        }
        else
        {
            Debug.Log("首次打开");
        }
    }

    /// <summary>
    /// // 初始化层级结构
    /// </summary>
    /// <param name="mapID"></param>
    /// <param name="mapData"></param>
    private static void InitializeHieraychy(string mapID, out MapData mapData)
    {
        GameObject mapDataObject = new GameObject("MapData_" + mapID);
        mapData = mapDataObject.AddComponent<MapData>();
        mapData.DataStruct.ID = int.Parse(mapID);
        GameObject mapInfo = new GameObject("MapInfo");
        mapInfo.transform.parent = mapDataObject.transform;
        GameObject mapGenerator = new GameObject("MapGenerator");
        mapGenerator.transform.parent = mapDataObject.transform;
        GameObject monsterGenerator = new GameObject("MonsterGenerator");
        monsterGenerator.transform.parent = mapGenerator.transform;
    }

    /// <summary>
    /// 根据mapdata设计Hieraychy视窗
    /// </summary>
    /// <param name="mapData"></param>
    private static void setHieraychy(MapData mapData)
    {
        // 按照index排序
        mapData.DataStruct.MapGenerators.Sort((MapGeneratorDataStruct m1, MapGeneratorDataStruct m2) =>
        {
            if (m1.Index < m2.Index) return -1;
            else return m1.Index > m2.Index ? 1 : 0;
        });

        GameObject rootGenerator = GameObject.Find("MonsterGenerator");
        if (rootGenerator == null)
        {
            Debug.LogWarning("没有MonsterGenerator");
            return;
        }

        foreach (MapGeneratorDataStruct generators in mapData.DataStruct.MapGenerators)
        {
            GameObject mapGeneratorObject = new GameObject(generators.Name + "_" + generators.Index);
            mapGeneratorObject.transform.parent = rootGenerator.transform;
            MapGenerator mapGeneratorComponent = mapGeneratorObject.AddComponent<MapGenerator>();
            mapGeneratorComponent.DataStruct = generators;

            // 按照index排序
            generators.Units.Sort((UnitStruct u1, UnitStruct u2) =>
            {
                if (u1.Index < u2.Index) return -1;
                else if (u1.Index > u2.Index) return 1;
                else return 0;
            });

            foreach (var unit in generators.Units)
            {
                GameObject unitObject = new GameObject(unit.Name + "_" + unit.Index);
                unitObject.transform.parent = mapGeneratorObject.transform;
                Unit unitComponent = unitObject.AddComponent<Unit>();
                unitComponent.DataStruct = unit;
            }
        }
    }

    public static void loadMap(string targetMapPath, string mapID)
    {
        // 加载Prefab资源
        mapPrefab = (GameObject)Resources.Load(targetMapPath);
        if (mapPrefab != null)
        {
            loadMapData(mapID);
            Instantiate(mapPrefab);
        }
        else
        {
            Debug.Log("Load失败");
        }
    }
}

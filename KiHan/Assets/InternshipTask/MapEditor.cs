using UnityEngine;
using UnityEditor;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;
using System;

public class MapEditor : MonoBehaviour {

    public static readonly string MAP_PREFAB_ID_PATH = "Assets/Resources/Scene";
    public static readonly string MAP_ID_PATH = "Assets/Resources/Config/Map";

    public static Map currentMap;

    [MenuItem("KHEngine/MapEditor/打开")]
    public static void open()
    {
        EditorWindow.GetWindow(typeof(OpenWindow), true, "打开");
    }

    [MenuItem("KHEngine/MapEditor/创建")]
    public static void create()
    {
        EditorWindow.GetWindow(typeof(CreateWindow), true, "创建");
    }

    [MenuItem("KHEngine/MapEditor/保存")]
    public static void save()
    {
        // PrefabUtility.SaveAsPrefabAsset(currentMap.MapObject, MAP_ID_PATH + "/" + currentMap.MapData.ID + "/" + currentMap.MapData.ID + ".prefab");

        // 生成对应的mapdata
        Debug.Log("序列化使用的地址：" + MAP_ID_PATH + "/" + currentMap.MapData.ID + "/MapData.dat");
        FileStream fileStream = new FileStream(MAP_ID_PATH + "/" + currentMap.MapData.ID + "/MapData.dat", FileMode.Create, FileAccess.ReadWrite);
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(fileStream, currentMap.MapData);
        Debug.Log("序列化前：" + currentMap.MapData.MapGenerators.Count);
        fileStream.Close();

        fileStream = new FileStream(MAP_ID_PATH + "/" + currentMap.MapData.ID + "/MapData.dat", FileMode.Open, FileAccess.ReadWrite);
        bf = new BinaryFormatter();
        MapData mapDate = bf.Deserialize(fileStream) as MapData;
        Debug.Log("序列化后：" + mapDate.MapGenerators.Count);

        if (currentMap != null)
        {
            
        }
        else
        {
            Debug.Log("当前没有可以保存的");
        }
        
    }

    [MenuItem("KHEngine/MapEditor/创建生成器")]
    public static void createGenerator()
    {
        Debug.Log("创建生成器");
        MapGenerator mapGenerator = new MapGenerator();
        
    }

    [MenuItem("KHEngine/MapEditor/创建单位")]
    public static void createUnit()
    {
        Debug.Log("创建单位");
    }

    public static MapData loadMapData(string mapID)
    {
        MapData mapData = new MapData(mapID);
        bool isFirstLoad = true;
        // Debug.Log(MAP_ID_PATH + "/" + Convert.ToString(currentMap.MapData.ID));
        foreach (var fileName in Directory.GetFiles(MAP_ID_PATH + "/" + mapID))
        {
            if (fileName.Substring(fileName.IndexOf("\\")) == "\\MapData.dat")
            {
                Debug.Log("不是首次打开");
                isFirstLoad = false;
            }
        }

        GameObject mapDataObject = new GameObject("MapData_" + mapID);
        GameObject mapInfo = new GameObject("MapInfo");
        mapInfo.transform.parent = mapDataObject.transform;
        GameObject mapGenerator = new GameObject("MapGenerator");
        mapGenerator.transform.parent = mapDataObject.transform;

        if (!isFirstLoad)
        {
            // 要根据文件内容来加载
            Debug.Log("反序列化使用的地址：" + MAP_ID_PATH + "/" + mapID + "/MapData.dat");
            FileStream fileStream = new FileStream(MAP_ID_PATH + "/" + mapID + "/MapData.dat", FileMode.Open, FileAccess.ReadWrite);
            BinaryFormatter bf = new BinaryFormatter();
            mapData = bf.Deserialize(fileStream) as MapData;
            Debug.Log("序列化后：" + mapData.MapGenerators[0].Name);

            // 按照index排序
            /*
            mapData.MapGenerators.Sort((MapGenerator m1, MapGenerator m2) =>
            {
                if (m1.Index < m2.Index) return -1;
                else if (m1.Index > m2.Index) return 1;
                else return 0;
            });
            */

            foreach (var generators in mapData.MapGenerators)
            {
                GameObject mapGeneratorObject = new GameObject(generators.Name + "_" + generators.Index);
                mapGeneratorObject.transform.parent = mapGenerator.transform;

                // 按照index排序
                generators.Units.Sort((Unit u1, Unit u2) =>
                {
                    if (u1.Index < u2.Index) return -1;
                    else if (u1.Index > u2.Index) return 1;
                    else return 0;
                });

                foreach (var unit in generators.Units)
                {
                    GameObject unitObject = new GameObject(unit.UnitName + "_" + unit.Index);
                    unitObject.transform.parent = mapGeneratorObject.transform;
                }
            }
            fileStream.Close();
        }
        else
        {
            Debug.Log("首次打开");
        }

        return mapData;
    }

    public static void loadMap(string targetMapPath, string mapID)
    {
        // 加载Prefab资源
        GameObject targetMap = (GameObject)Resources.Load(targetMapPath);
        if (targetMap != null)
        {
            MapData mapData = loadMapData(mapID);

            /*
            MapData mapData = new MapData("10102");
            MapGenerator monsterGenerator = new MapGenerator("MonsterGenerator", 1);
            monsterGenerator.Units.Add(new Unit(50007001, "怪物1"));
            monsterGenerator.Units.Add(new Unit(50006002, "怪物2"));
            mapData.MapGenerators.Add(monsterGenerator);
            */

            currentMap = new Map(Instantiate(targetMap), mapData);
        }
        else
        {
            Debug.Log("Load失败");
        }
    }
}

using UnityEngine;
using UnityEditor;
using System.Collections;

public class MapEditor : MonoBehaviour {

    public static readonly string MAP_PREFAB_ID_PATH = "Assets\\Resources\\Scene";
    public static readonly string MAP_ID_PATH = "Assets\\Resources\\Config\\Map";

    [MenuItem("KHEngine/MapEditor/打开")]
    public static void open()
    {
        EditorWindow.GetWindow(typeof(OpenWindow));
    }

    [MenuItem("KHEngine/MapEditor/创建")]
    public static void create()
    {
        Debug.Log("创建");
        EditorWindow.GetWindow(typeof(CreateWindow));
    }

    [MenuItem("KHEngine/MapEditor/保存")]
    public static void save()
    {
        Debug.Log("保存");
    }

    [MenuItem("KHEngine/MapEditor/创建生成器")]
    public static void createGenerator()
    {
        Debug.Log("创建生成器");
    }

    [MenuItem("KHEngine/MapEditor/创建单位")]
    public static void createUnit()
    {
        Debug.Log("创建单位");
    }

    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}


}

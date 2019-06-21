using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapData))]
public class MapDataEditor : Editor
{
    MapData mapData;

    void OnEnable()
    {
        //获取当前编辑自定义Inspector的对象
        mapData = FindObjectOfType<MapData>();
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.BeginVertical();

        EditorGUILayout.IntField("Map ID", mapData.DataStruct.ID);
        mapData.DataStruct.Desc = EditorGUILayout.TextField("Desc", mapData.DataStruct.Desc);

        EditorGUILayout.EndVertical();
    }
}

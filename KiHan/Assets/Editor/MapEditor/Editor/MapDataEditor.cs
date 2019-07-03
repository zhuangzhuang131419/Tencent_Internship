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
        mapData = (MapData)target;
    }

    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();
        //EditorGUILayout.BeginVertical();

        //EditorGUILayout.IntField("Map ID", mapData.ID);
        //mapData.Desc = EditorGUILayout.TextField("Desc", mapData.Desc);

        //EditorGUILayout.EndVertical();
    }
}

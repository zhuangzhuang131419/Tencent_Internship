using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    MapGenerator mapGenerator;

    void OnEnable()
    {
        //获取当前编辑自定义Inspector的对象
        mapGenerator = (MapGenerator)target;
        // mapGenerator.Name = "MapGenerator" + mapGenerator.ID;
    }

    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();
        EditorGUILayout.BeginVertical();

        // EditorGUILayout.IntField("MapGenerator ID", mapGenerator.ID);
        // EditorGUILayout.TextField("Desc", mapGenerator.Desc);
        mapGenerator.name = mapGenerator.Name;
        // mapGenerator.Name = mapGenerator.Name;
        mapGenerator.ID = FindObjectOfType<MapData>().ID * 1000 + mapGenerator.Index;
        // EditorGUILayout.IntField("Index", mapGenerator.Index);
        // mapGenerator.Type = EditorGUILayout.IntField("Type", mapGenerator.Type);
        EditorGUILayout.EndVertical();
    }
}
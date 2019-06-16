using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    MapGenerator mapGenerator;

    void OnEnable()
    {
        //获取当前编辑自定义Inspector的对象
        mapGenerator = (MapGenerator)target;
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.BeginVertical();

        EditorGUILayout.IntField("MapGenerator ID", mapGenerator.DataStruct.ID);
        mapGenerator.DataStruct.Desc = EditorGUILayout.TextField("Desc", mapGenerator.DataStruct.Desc);
        mapGenerator.name = mapGenerator.DataStruct.Name + "_" + mapGenerator.DataStruct.Index;
        mapGenerator.DataStruct.Name = EditorGUILayout.TextField("Name", mapGenerator.DataStruct.Name);
        EditorGUILayout.IntField("Index", mapGenerator.DataStruct.Index);
        mapGenerator.DataStruct.Type = EditorGUILayout.IntField("Type", mapGenerator.DataStruct.Type);
        EditorGUILayout.EndVertical();
    }
}

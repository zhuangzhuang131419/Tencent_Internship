using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Unit))]

public class UnitEditor : Editor
{
    Unit unit;

    void OnEnable()
    {
        //获取当前编辑自定义Inspector的对象
        unit = (Unit)target;
    }

    public override void OnInspectorGUI()
    {
        
        EditorGUILayout.BeginVertical();
        EditorGUILayout.IntField("Unit Index", unit.DataStruct.Index);
        unit.name = unit.DataStruct.Name + "_" + unit.DataStruct.Index;
        unit.DataStruct.Name = EditorGUILayout.TextField("Name", unit.DataStruct.Name);
        unit.DataStruct.Desc = EditorGUILayout.TextField("Desc", unit.DataStruct.Desc);
        unit.DataStruct.ID = EditorGUILayout.IntField("Unit ID", unit.DataStruct.ID);


        unit.DataStruct.CreateAction = EditorGUILayout.IntField("Create Action", unit.DataStruct.CreateAction);
        unit.DataStruct.CreateFrame = EditorGUILayout.IntField("Create Frame", unit.DataStruct.CreateFrame);
        unit.DataStruct.Direction = EditorGUILayout.IntField("Direction", unit.DataStruct.Direction);
        unit.DataStruct.DelayCreateTime = EditorGUILayout.IntField("Delay Create Time", unit.DataStruct.DelayCreateTime);
        unit.DataStruct.CenterToPlay = EditorGUILayout.IntField("Center To Play", unit.DataStruct.CenterToPlay);
        EditorGUILayout.EndVertical();
    }
}

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
        // unit.Name = "MonsterGenerator" + unit.Index;
    }

    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();
        
        EditorGUILayout.BeginVertical();
        // EditorGUILayout.IntField("Unit Index", unit.Index);
        unit.name = unit.Name;
        //unit.Name = unit.Name;
        //unit.Desc = EditorGUILayout.TextField("Desc", unit.Desc);
        //unit.ID = EditorGUILayout.IntField("Unit ID", unit.ID);
        //unit.CreateAction = EditorGUILayout.IntField("Create Action VKey", unit.CreateAction);
        //unit.CreateFrame = EditorGUILayout.IntField("Create Frame", unit.CreateFrame);
        //unit.CreateHeight = EditorGUILayout.FloatField("CreateHeight", unit.CreateHeight);
        //unit.Direction = EditorGUILayout.IntField("Direction", unit.Direction);
        //unit.DelayCreateTime = EditorGUILayout.IntField("Delay Create Time", unit.DelayCreateTime);
        //unit.CenterToPlayer = EditorGUILayout.IntField("Center To Player", unit.CenterToPlayer);
        EditorGUILayout.EndVertical();
        
    }
}
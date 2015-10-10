//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
//
//                     Version 0.9.1 (20151010)
//                     Copyright © 2011-2015
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------
using UnityEngine;
using System.Collections;
using System.Reflection;
using UnityEditor;

[CustomEditor(typeof(CObjectDebugger))]
public class CObjectDebuggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var dTarget = (CObjectDebugger)target;
        if (dTarget.WatchObject != null)
        {
            foreach (var field in dTarget.WatchObject.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                var value = field.GetValue(dTarget.WatchObject);
                EditorGUILayout.LabelField(field.Name, value != null ? value.ToString() :  "[NULL]");
            }
            foreach (var prop in dTarget.WatchObject.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                var getMethod = prop.GetGetMethod();
                if (getMethod != null)
                {
                    var ret = getMethod.Invoke(dTarget.WatchObject, new object[]{});

                    EditorGUILayout.LabelField(prop.Name, ret != null ? ret.ToString() : "[NULL]");    
                }
                
            }
        }

    }
}

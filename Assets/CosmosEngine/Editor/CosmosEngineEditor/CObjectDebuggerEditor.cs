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

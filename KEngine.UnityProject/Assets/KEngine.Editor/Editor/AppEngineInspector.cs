using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KEngine;
using UnityEditor;
using UnityEngine;


namespace KEngine.Editor
{
    [InitializeOnLoad]
    public class AppEngineInitializeOnLoad
    {
        static AppEngineInitializeOnLoad()
        {
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyItemCB;
        }

        private static void HierarchyItemCB(int instanceid, Rect selectionrect)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceid) as GameObject;
            if (obj != null)
            {
                if (obj.GetComponent<AppEngine>() != null)
                {
                    Rect r = new Rect(selectionrect);
                    r.x = r.width - 80;
                    r.width = 80;
                    var style = new GUIStyle();
                    style.normal.textColor = Color.yellow;
                    style.hover.textColor = Color.cyan;
                    GUI.Label(r, "[KEngine]", style);
                }
            }
        
        }
    }

    [CustomEditor(typeof(AppEngine))]
    public class AppEngineInspector : UnityEditor.Editor
    {
        private bool _showModules = false;
        
        public override void OnInspectorGUI()
        {
            
            var engine = target as AppEngine;
            //Logger.LogLevel
            Logger.LogLevel = (KLogLevel)EditorGUILayout.EnumPopup("Logger Level", Logger.LogLevel);
            EditorGUILayout.LabelField("Modules Count: ", engine.GameModules.Length.ToString());

            _showModules = EditorGUILayout.Foldout(_showModules, "Modules");

            if (_showModules)
            {
                var modCount = engine.GameModules.Length;
                for (var m = 0; m < modCount; m++)
                {
                    var module = engine.GameModules[m];
                    EditorGUILayout.LabelField("- " + module.ToString());
                }
            }

            base.OnInspectorGUI();
        }
    }
}

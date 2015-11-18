using UnityEngine;
using System.Collections;
using UnityEditor;

[InitializeOnLoad]
class KUIPanelAssetEditorInitializer
{
    static KUIPanelAssetEditorInitializer()
    {
        EditorApplication.hierarchyWindowItemOnGUI += HierarchyItemCB;
    }

    private static void HierarchyItemCB(int instanceid, Rect selectionrect)
    {
        var obj = EditorUtility.InstanceIDToObject(instanceid) as GameObject;
        if (obj != null)
        {
            if (obj.GetComponent<KUIWindowAsset>() != null)
            {
                Rect r = new Rect(selectionrect);
                r.x = 0;//r.width - 30;
                r.width = 30;
                var style = new GUIStyle();
                style.normal.textColor = Color.yellow;
                style.hover.textColor = Color.cyan;
                GUI.Label(r, "[UI]", style);
            }
        }
        
    }
}
[CustomEditor(typeof(KUIWindowAsset))]
public class KUIPanelAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("A UI Will be build for name: " + target.name, MessageType.Info);
        base.OnInspectorGUI();
        
    }
}

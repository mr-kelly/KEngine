//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
#if UNITY_3_5
[CustomEditor(typeof(UIWrapContent))]
#else
[CustomEditor(typeof(UIWrapContent), true)]
#endif
public class UIWrapContentEditor : Editor
{
	public override void OnInspectorGUI ()
	{
		GUILayout.Space(6f);
		NGUIEditorTools.SetLabelWidth(90f);

		string fieldName = "Item Size";
		string error = null;
		UIScrollView sv = null;

		if (!serializedObject.isEditingMultipleObjects)
		{
			UIWrapContent list = target as UIWrapContent;
			sv = NGUITools.FindInParents<UIScrollView>(list.gameObject);

			if (sv == null)
			{
				error = "UIWrappedList needs a Scroll View on its parent in order to work properly";
			}
			else if (sv.movement == UIScrollView.Movement.Horizontal) fieldName = "Item Width";
			else if (sv.movement == UIScrollView.Movement.Vertical) fieldName = "Item Height";
			else
			{
				error = "Scroll View needs to be using Horizontal or Vertical movement";
			}
		}

		serializedObject.Update();
		GUILayout.BeginHorizontal();
		NGUIEditorTools.DrawProperty(fieldName, serializedObject, "itemSize", GUILayout.Width(130f));
		GUILayout.Label("pixels");
		GUILayout.EndHorizontal();
		NGUIEditorTools.DrawProperty("Cull Content", serializedObject, "cullContent");

		if (!string.IsNullOrEmpty(error))
		{
			EditorGUILayout.HelpBox(error, MessageType.Error);
			if (sv != null && GUILayout.Button("Select the Scroll View"))
				Selection.activeGameObject = sv.gameObject;
		}

		serializedObject.ApplyModifiedProperties();
	}
}

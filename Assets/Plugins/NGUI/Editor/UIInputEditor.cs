//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

#if UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8 || UNITY_BLACKBERRY
#define MOBILE
#endif

using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
#if UNITY_3_5
[CustomEditor(typeof(UIInput))]
#else
[CustomEditor(typeof(UIInput), true)]
#endif
public class UIInputEditor : UIWidgetContainerEditor
{
	public override void OnInspectorGUI ()
	{
		UIInput input = target as UIInput;
		serializedObject.Update();
		GUILayout.Space(3f);
		NGUIEditorTools.SetLabelWidth(110f);
		//NGUIEditorTools.DrawProperty(serializedObject, "m_Script");

		EditorGUI.BeginDisabledGroup(serializedObject.isEditingMultipleObjects);
		SerializedProperty label = NGUIEditorTools.DrawProperty(serializedObject, "label");
		EditorGUI.EndDisabledGroup();

		EditorGUI.BeginDisabledGroup(label == null || label.objectReferenceValue == null);
		{
			if (Application.isPlaying) NGUIEditorTools.DrawPaddedProperty("Value", serializedObject, "mValue");
			else NGUIEditorTools.DrawPaddedProperty("Starting Value", serializedObject, "mValue");
			NGUIEditorTools.DrawPaddedProperty(serializedObject, "savedAs");
			NGUIEditorTools.DrawProperty("Active Text Color", serializedObject, "activeTextColor");

			EditorGUI.BeginDisabledGroup(serializedObject.isEditingMultipleObjects);
			{
				if (label != null && label.objectReferenceValue != null)
				{
					SerializedObject ob = new SerializedObject(label.objectReferenceValue);
					ob.Update();
					NGUIEditorTools.DrawProperty("Inactive Color", ob, "mColor");
					ob.ApplyModifiedProperties();
				}
				else EditorGUILayout.ColorField("Inactive Color", Color.white);
			}
			EditorGUI.EndDisabledGroup();

			NGUIEditorTools.DrawProperty("Caret Color", serializedObject, "caretColor");
			NGUIEditorTools.DrawProperty("Selection Color", serializedObject, "selectionColor");
#if !MOBILE
			NGUIEditorTools.DrawProperty(serializedObject, "selectOnTab");
#endif
			NGUIEditorTools.DrawPaddedProperty(serializedObject, "inputType");
#if MOBILE
			NGUIEditorTools.DrawPaddedProperty(serializedObject, "keyboardType");
#else
			NGUIEditorTools.DrawPaddedProperty(serializedObject, "onReturnKey");
#endif
			NGUIEditorTools.DrawPaddedProperty(serializedObject, "validation");

			SerializedProperty sp = serializedObject.FindProperty("characterLimit");

			GUILayout.BeginHorizontal();

			if (sp.hasMultipleDifferentValues || input.characterLimit > 0)
			{
				EditorGUILayout.PropertyField(sp);
				GUILayout.Space(18f);
			}
			else
			{
				EditorGUILayout.PropertyField(sp);
				GUILayout.Label("unlimited");
			}
			GUILayout.EndHorizontal();

			NGUIEditorTools.SetLabelWidth(80f);
			EditorGUI.BeginDisabledGroup(serializedObject.isEditingMultipleObjects);
			NGUIEditorTools.DrawEvents("On Submit", input, input.onSubmit);
			NGUIEditorTools.DrawEvents("On Change", input, input.onChange);
			EditorGUI.EndDisabledGroup();
		}
		EditorGUI.EndDisabledGroup();
		serializedObject.ApplyModifiedProperties();
	}
}

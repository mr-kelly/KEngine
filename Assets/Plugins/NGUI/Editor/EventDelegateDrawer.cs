//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

#if !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2

using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;
using Entry = PropertyReferenceDrawer.Entry;

/// <summary>
/// Draws a single event delegate. Contributed by Adam Byrd.
/// </summary>

[CustomPropertyDrawer(typeof(EventDelegate))]
public class EventDelegateDrawer : PropertyDrawer
{
	const int lineHeight = 16;

	public override float GetPropertyHeight (SerializedProperty prop, GUIContent label)
	{
		SerializedProperty targetProp = prop.FindPropertyRelative("mTarget");
		if (targetProp.objectReferenceValue == null) return 2 * lineHeight;
		int lines = 3 * lineHeight;

		SerializedProperty methodProp = prop.FindPropertyRelative("mMethodName");

		EventDelegate del = new EventDelegate();
		del.target = targetProp.objectReferenceValue as MonoBehaviour;
		del.methodName = methodProp.stringValue;
		EventDelegate.Parameter[] ps = del.parameters;

		if (ps != null)
		{
			for (int i = 0; i < ps.Length; ++i)
			{
				lines += lineHeight;
				EventDelegate.Parameter param = ps[i];
				if (param.obj != null) lines += lineHeight;
			}
		}
		return lines;
	}

	public override void OnGUI (Rect rect, SerializedProperty prop, GUIContent label)
	{
		Undo.RecordObject(prop.serializedObject.targetObject, "Delegate Selection");

		SerializedProperty targetProp = prop.FindPropertyRelative("mTarget");
		SerializedProperty methodProp = prop.FindPropertyRelative("mMethodName");

		MonoBehaviour target = targetProp.objectReferenceValue as MonoBehaviour;
		string methodName = methodProp.stringValue;

		EditorGUI.indentLevel = prop.depth;
		EditorGUI.LabelField(rect, label);

		Rect lineRect = rect;
		lineRect.yMin = rect.yMin + lineHeight;
		lineRect.yMax = lineRect.yMin + lineHeight;

		EditorGUI.indentLevel = targetProp.depth;
		target = EditorGUI.ObjectField(lineRect, "Notify", target, typeof(MonoBehaviour), true) as MonoBehaviour;
		targetProp.objectReferenceValue = target;

		if (target != null && target.gameObject != null)
		{
			GameObject go = target.gameObject;
			List<Entry> list = EventDelegateEditor.GetMethods(go);

			int index = 0;
			int choice = 0;

			EventDelegate del = new EventDelegate();
			del.target = target;
			del.methodName = methodName;
			string[] names = PropertyReferenceDrawer.GetNames(list, del.ToString(), out index);

			lineRect.yMin += lineHeight;
			lineRect.yMax += lineHeight;
			choice = EditorGUI.Popup(lineRect, "Method", index, names);

			if (choice > 0)
			{
				if (choice != index)
				{
					Entry entry = list[choice - 1];
					target = entry.target as MonoBehaviour;
					methodName = entry.name;
					targetProp.objectReferenceValue = target;
					methodProp.stringValue = methodName;
				}
			}

			// Unfortunately Unity's property drawers only work with UnityEngine.Object-derived types.
			// This means that arrays are not supported. And since EventDelegate is not derived from
			// UnityEngine.Object either, it means that it's not possible to modify the parameter array.
			EditorGUI.BeginDisabledGroup(true);

			//SerializedProperty paramProp = prop.FindPropertyRelative("mParameters");
			EventDelegate.Parameter[] ps = del.parameters;

			if (ps != null)
			{
				for (int i = 0; i < ps.Length; ++i)
				{
					EventDelegate.Parameter param = ps[i];
					lineRect.yMin += lineHeight;
					lineRect.yMax += lineHeight;
					param.obj = EditorGUI.ObjectField(lineRect, "   Arg " + i, param.obj, typeof(Object), true);
					if (param.obj == null) continue;

					GameObject selGO = null;
					System.Type type = param.obj.GetType();
					if (type == typeof(GameObject)) selGO = param.obj as GameObject;
					else if (type.IsSubclassOf(typeof(Component))) selGO = (param.obj as Component).gameObject;

					if (selGO != null)
					{
						// Parameters must be exact -- they can't be converted like property bindings
						PropertyReferenceDrawer.filter = param.expectedType;
						PropertyReferenceDrawer.canConvert = false;
						List<PropertyReferenceDrawer.Entry> ents = PropertyReferenceDrawer.GetProperties(selGO, true, false);

						int selection;
						string[] props = EventDelegateEditor.GetNames(ents, NGUITools.GetFuncName(param.obj, param.field), out selection);

						lineRect.yMin += lineHeight;
						lineRect.yMax += lineHeight;
						int newSel = EditorGUI.Popup(lineRect, " ", selection, props);

						if (newSel != selection)
						{
							if (newSel == 0)
							{
								param.obj = selGO;
								param.field = null;
							}
							else
							{
								param.obj = ents[newSel - 1].target;
								param.field = ents[newSel - 1].name;
							}
						}
					}
					else if (!string.IsNullOrEmpty(param.field))
					{
						param.field = null;
					}

					PropertyReferenceDrawer.filter = typeof(void);
					PropertyReferenceDrawer.canConvert = true;
				}
			}

			EditorGUI.EndDisabledGroup();
		}
		//else paramProp.objectReferenceValue = null;
	}
}
#endif

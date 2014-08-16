//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(UIEventTrigger))]
public class UIEventTriggerEditor : Editor
{
	UIEventTrigger mTrigger;

	void OnEnable ()
	{
		mTrigger = target as UIEventTrigger;
		EditorPrefs.SetBool("ET0", EventDelegate.IsValid(mTrigger.onHoverOver));
		EditorPrefs.SetBool("ET1", EventDelegate.IsValid(mTrigger.onHoverOut));
		EditorPrefs.SetBool("ET2", EventDelegate.IsValid(mTrigger.onPress));
		EditorPrefs.SetBool("ET3", EventDelegate.IsValid(mTrigger.onRelease));
		EditorPrefs.SetBool("ET4", EventDelegate.IsValid(mTrigger.onSelect));
		EditorPrefs.SetBool("ET5", EventDelegate.IsValid(mTrigger.onDeselect));
		EditorPrefs.SetBool("ET6", EventDelegate.IsValid(mTrigger.onClick));
		EditorPrefs.SetBool("ET7", EventDelegate.IsValid(mTrigger.onDoubleClick));
		EditorPrefs.SetBool("ET8", EventDelegate.IsValid(mTrigger.onDragOver));
		EditorPrefs.SetBool("ET9", EventDelegate.IsValid(mTrigger.onDragOut));
	}

	public override void OnInspectorGUI ()
	{
		GUILayout.Space(3f);
		NGUIEditorTools.SetLabelWidth(80f);
		DrawEvents("ET0", "On Hover Over", mTrigger.onHoverOver);
		DrawEvents("ET1", "On Hover Out", mTrigger.onHoverOut);
		DrawEvents("ET2", "On Press", mTrigger.onPress);
		DrawEvents("ET3", "On Release", mTrigger.onRelease);
		DrawEvents("ET4", "On Select", mTrigger.onSelect);
		DrawEvents("ET5", "On Deselect", mTrigger.onDeselect);
		DrawEvents("ET6", "On Click/Tap", mTrigger.onClick);
		DrawEvents("ET7", "On Double-Click/Tap", mTrigger.onDoubleClick);
		DrawEvents("ET8", "On Drag Over", mTrigger.onDragOver);
		DrawEvents("ET9", "On Drag Out", mTrigger.onDragOut);
	}

	void DrawEvents (string key, string text, List<EventDelegate> list)
	{
		if (!NGUIEditorTools.DrawHeader(text, key, false)) return;
		NGUIEditorTools.BeginContents();
		EventDelegateEditor.Field(mTrigger, list, null, null);
		NGUIEditorTools.EndContents();
	}
}

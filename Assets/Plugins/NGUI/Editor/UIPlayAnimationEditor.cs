//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

#if !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
#define USE_MECANIM
#endif

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UIPlayAnimation))]
public class UIPlayAnimationEditor : Editor
{
	enum ResetOnPlay
	{
		Continue,
		StartFromBeginning,
	}

	enum SelectedObject
	{
		KeepCurrent,
		SetToNothing,
	}

	public override void OnInspectorGUI ()
	{
		NGUIEditorTools.SetLabelWidth(120f);
		UIPlayAnimation pa = target as UIPlayAnimation;
		GUILayout.Space(6f);

		GUI.changed = false;

#if USE_MECANIM
		EditorGUI.BeginDisabledGroup(pa.target);
		Animator animator = (Animator)EditorGUILayout.ObjectField("Animator", pa.animator, typeof(Animator), true);
		EditorGUI.EndDisabledGroup();
		EditorGUI.BeginDisabledGroup(pa.animator);
#endif
		Animation anim = (Animation)EditorGUILayout.ObjectField("Animation", pa.target, typeof(Animation), true);

#if USE_MECANIM
		EditorGUI.EndDisabledGroup();
		EditorGUI.BeginDisabledGroup(anim == null && animator == null);
		string clipName = EditorGUILayout.TextField("State Name", pa.clipName);
#else
		EditorGUI.BeginDisabledGroup(anim == null);
		string clipName = EditorGUILayout.TextField("Clip Name", pa.clipName);
#endif

		AnimationOrTween.Trigger trigger = (AnimationOrTween.Trigger)EditorGUILayout.EnumPopup("Trigger condition", pa.trigger);

#if USE_MECANIM
		EditorGUI.BeginDisabledGroup(animator != null && !string.IsNullOrEmpty(clipName));
		AnimationOrTween.Direction dir = (AnimationOrTween.Direction)EditorGUILayout.EnumPopup("Play direction", pa.playDirection);
		EditorGUI.EndDisabledGroup();
#else
		AnimationOrTween.Direction dir = (AnimationOrTween.Direction)EditorGUILayout.EnumPopup("Play direction", pa.playDirection);
#endif

		SelectedObject so = pa.clearSelection ? SelectedObject.SetToNothing : SelectedObject.KeepCurrent;
		bool clear = (SelectedObject)EditorGUILayout.EnumPopup("Selected object", so) == SelectedObject.SetToNothing;
		AnimationOrTween.EnableCondition enab = (AnimationOrTween.EnableCondition)EditorGUILayout.EnumPopup("If disabled on start", pa.ifDisabledOnPlay);
		ResetOnPlay rs = pa.resetOnPlay ? ResetOnPlay.StartFromBeginning : ResetOnPlay.Continue;
		bool reset = (ResetOnPlay)EditorGUILayout.EnumPopup("If already playing", rs) == ResetOnPlay.StartFromBeginning;
		AnimationOrTween.DisableCondition dis = (AnimationOrTween.DisableCondition)EditorGUILayout.EnumPopup("When finished", pa.disableWhenFinished);
		EditorGUI.EndDisabledGroup();

		if (GUI.changed)
		{
			NGUIEditorTools.RegisterUndo("PlayAnimation Change", pa);
			pa.target = anim;
#if USE_MECANIM
			pa.animator = animator;
#endif
			pa.clipName = clipName;
			pa.trigger = trigger;
			pa.playDirection = dir;
			pa.clearSelection = clear;
			pa.ifDisabledOnPlay = enab;
			pa.resetOnPlay = reset;
			pa.disableWhenFinished = dis;
			NGUITools.SetDirty(pa);
		}

		NGUIEditorTools.SetLabelWidth(80f);
		NGUIEditorTools.DrawEvents("On Finished", pa, pa.onFinished);
	}
}

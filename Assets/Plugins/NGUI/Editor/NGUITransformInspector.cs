//----------------------------------------------
//			  NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

#if UNITY_3_5

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Transform))]
public class NGUITransformInspector : Editor
{
	/// <summary>
	/// Draw the inspector widget.
	/// </summary>

	public override void OnInspectorGUI ()
	{
		Transform trans = target as Transform;
		NGUIEditorTools.SetLabelWidth(15f);

		Vector3 pos;
		Vector3 rot;
		Vector3 scale;

		// Position
		EditorGUILayout.BeginHorizontal();
		{
			if (DrawButton("P", "Reset Position", IsResetPositionValid(trans), 20f))
			{
				NGUIEditorTools.RegisterUndo("Reset Position", trans);
				trans.localPosition = Vector3.zero;
			}
			pos = DrawVector3(trans.localPosition);
		}
		EditorGUILayout.EndHorizontal();

		// Rotation
		EditorGUILayout.BeginHorizontal();
		{
			if (DrawButton("R", "Reset Rotation", IsResetRotationValid(trans), 20f))
			{
				NGUIEditorTools.RegisterUndo("Reset Rotation", trans);
				trans.localEulerAngles = Vector3.zero;
			}
			rot = DrawVector3(trans.localEulerAngles);
		}
		EditorGUILayout.EndHorizontal();

		// Scale
		EditorGUILayout.BeginHorizontal();
		{
			if (DrawButton("S", "Reset Scale", IsResetScaleValid(trans), 20f))
			{
				NGUIEditorTools.RegisterUndo("Reset Scale", trans);
				trans.localScale = Vector3.one;
			}
			scale = DrawVector3(trans.localScale);
		}
		EditorGUILayout.EndHorizontal();

		// If something changes, set the transform values
		if (GUI.changed)
		{
			NGUIEditorTools.RegisterUndo("Transform Change", trans);
			trans.localPosition = Validate(pos);
			trans.localEulerAngles = Validate(rot);
			trans.localScale = Validate(scale);
		}
	}

	/// <summary>
	/// Helper function that draws a button in an enabled or disabled state.
	/// </summary>

	static bool DrawButton (string title, string tooltip, bool enabled, float width)
	{
		if (enabled)
		{
			// Draw a regular button
			return GUILayout.Button(new GUIContent(title, tooltip), GUILayout.Width(width));
		}
		else
		{
			// Button should be disabled -- draw it darkened and ignore its return value
			Color color = GUI.color;
			GUI.color = new Color(1f, 1f, 1f, 0.25f);
			GUILayout.Button(new GUIContent(title, tooltip), GUILayout.Width(width));
			GUI.color = color;
			return false;
		}
	}

	/// <summary>
	/// Helper function that draws a field of 3 floats.
	/// </summary>

	static Vector3 DrawVector3 (Vector3 value)
	{
		GUILayoutOption opt = GUILayout.MinWidth(30f);
		value.x = EditorGUILayout.FloatField("X", value.x, opt);
		value.y = EditorGUILayout.FloatField("Y", value.y, opt);
		value.z = EditorGUILayout.FloatField("Z", value.z, opt);
		return value;
	}

	/// <summary>
	/// Helper function that determines whether its worth it to show the reset position button.
	/// </summary>

	static bool IsResetPositionValid (Transform targetTransform)
	{
		Vector3 v = targetTransform.localPosition;
		return (v.x != 0f || v.y != 0f || v.z != 0f);
	}

	/// <summary>
	/// Helper function that determines whether its worth it to show the reset rotation button.
	/// </summary>

	static bool IsResetRotationValid (Transform targetTransform)
	{
		Vector3 v = targetTransform.localEulerAngles;
		return (v.x != 0f || v.y != 0f || v.z != 0f);
	}

	/// <summary>
	/// Helper function that determines whether its worth it to show the reset scale button.
	/// </summary>

	static bool IsResetScaleValid (Transform targetTransform)
	{
		Vector3 v = targetTransform.localScale;
		return (v.x != 1f || v.y != 1f || v.z != 1f);
	}

	/// <summary>
	/// Helper function that removes not-a-number values from the vector.
	/// </summary>

	static Vector3 Validate (Vector3 vector)
	{
		vector.x = float.IsNaN(vector.x) ? 0f : vector.x;
		vector.y = float.IsNaN(vector.y) ? 0f : vector.y;
		vector.z = float.IsNaN(vector.z) ? 0f : vector.z;
		return vector;
	}
}
#else
using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(Transform), true)]
public class NGUITransformInspector : Editor
{
	static public NGUITransformInspector instance;

	SerializedProperty mPos;
	SerializedProperty mRot;
	SerializedProperty mScale;

	void OnEnable ()
	{
		instance = this;

		mPos = serializedObject.FindProperty("m_LocalPosition");
		mRot = serializedObject.FindProperty("m_LocalRotation");
		mScale = serializedObject.FindProperty("m_LocalScale");
	}

	void OnDestroy () { instance = null; }

	/// <summary>
	/// Draw the inspector widget.
	/// </summary>

	public override void OnInspectorGUI ()
	{
		NGUIEditorTools.SetLabelWidth(15f);

		serializedObject.Update();

		bool widgets = false;

		foreach (Object obj in serializedObject.targetObjects)
		{
			Transform t = obj as Transform;

			if (t.GetComponent<UIWidget>() != null)
			{
				widgets = true;
				break;
			}
		}

		DrawPosition();
		DrawRotation(widgets);
		DrawScale(widgets);

		serializedObject.ApplyModifiedProperties();
	}

	void DrawPosition ()
	{
		GUILayout.BeginHorizontal();
		{
			bool reset = GUILayout.Button("P", GUILayout.Width(20f));

			EditorGUILayout.PropertyField(mPos.FindPropertyRelative("x"));
			EditorGUILayout.PropertyField(mPos.FindPropertyRelative("y"));
			EditorGUILayout.PropertyField(mPos.FindPropertyRelative("z"));

			if (reset) mPos.vector3Value = Vector3.zero;
		}
		GUILayout.EndHorizontal();
	}

	void DrawScale (bool isWidget)
	{
		GUILayout.BeginHorizontal();
		{
			bool reset = GUILayout.Button("S", GUILayout.Width(20f));

			if (isWidget) GUI.color = new Color(0.7f, 0.7f, 0.7f);
			EditorGUILayout.PropertyField(mScale.FindPropertyRelative("x"));
			EditorGUILayout.PropertyField(mScale.FindPropertyRelative("y"));
			EditorGUILayout.PropertyField(mScale.FindPropertyRelative("z"));
			if (isWidget) GUI.color = Color.white;

			if (reset) mScale.vector3Value = Vector3.one;
		}
		GUILayout.EndHorizontal();
	}

#region Rotation is ugly as hell... since there is no native support for quaternion property drawing
	enum Axes : int
	{
		None = 0,
		X = 1,
		Y = 2,
		Z = 4,
		All = 7,
	}

	Axes CheckDifference (Transform t, Vector3 original)
	{
		Vector3 next = t.localEulerAngles;

		Axes axes = Axes.None;

		if (Differs(next.x, original.x)) axes |= Axes.X;
		if (Differs(next.y, original.y)) axes |= Axes.Y;
		if (Differs(next.z, original.z)) axes |= Axes.Z;

		return axes;
	}

	Axes CheckDifference (SerializedProperty property)
	{
		Axes axes = Axes.None;

		if (property.hasMultipleDifferentValues)
		{
			Vector3 original = property.quaternionValue.eulerAngles;

			foreach (Object obj in serializedObject.targetObjects)
			{
				axes |= CheckDifference(obj as Transform, original);
				if (axes == Axes.All) break;
			}
		}
		return axes;
	}

	/// <summary>
	/// Draw an editable float field.
	/// </summary>
	/// <param name="hidden">Whether to replace the value with a dash</param>
	/// <param name="greyedOut">Whether the value should be greyed out or not</param>

	static bool FloatField (string name, ref float value, bool hidden, bool greyedOut, GUILayoutOption opt)
	{
		float newValue = value;
		GUI.changed = false;

		if (!hidden)
		{
			if (greyedOut)
			{
				GUI.color = new Color(0.7f, 0.7f, 0.7f);
				newValue = EditorGUILayout.FloatField(name, newValue, opt);
				GUI.color = Color.white;
			}
			else
			{
				newValue = EditorGUILayout.FloatField(name, newValue, opt);
			}
		}
		else if (greyedOut)
		{
			GUI.color = new Color(0.7f, 0.7f, 0.7f);
			float.TryParse(EditorGUILayout.TextField(name, "--", opt), out newValue);
			GUI.color = Color.white;
		}
		else
		{
			float.TryParse(EditorGUILayout.TextField(name, "--", opt), out newValue);
		}

		if (GUI.changed && Differs(newValue, value))
		{
			value = newValue;
			return true;
		}
		return false;
	}

	/// <summary>
	/// Because Mathf.Approximately is too sensitive.
	/// </summary>

	static bool Differs (float a, float b) { return Mathf.Abs(a - b) > 0.0001f; }

	void DrawRotation (bool isWidget)
	{
		GUILayout.BeginHorizontal();
		{
			bool reset = GUILayout.Button("R", GUILayout.Width(20f));

			Vector3 visible = (serializedObject.targetObject as Transform).localEulerAngles;

			visible.x = NGUIMath.WrapAngle(visible.x);
			visible.y = NGUIMath.WrapAngle(visible.y);
			visible.z = NGUIMath.WrapAngle(visible.z);

			Axes changed = CheckDifference(mRot);
			Axes altered = Axes.None;

			GUILayoutOption opt = GUILayout.MinWidth(30f);

			if (FloatField("X", ref visible.x, (changed & Axes.X) != 0, isWidget, opt)) altered |= Axes.X;
			if (FloatField("Y", ref visible.y, (changed & Axes.Y) != 0, isWidget, opt)) altered |= Axes.Y;
			if (FloatField("Z", ref visible.z, (changed & Axes.Z) != 0, false, opt)) altered |= Axes.Z;

			if (reset)
			{
				mRot.quaternionValue = Quaternion.identity;
			}
			else if (altered != Axes.None)
			{
				NGUIEditorTools.RegisterUndo("Change Rotation", serializedObject.targetObjects);

				foreach (Object obj in serializedObject.targetObjects)
				{
					Transform t = obj as Transform;
					Vector3 v = t.localEulerAngles;

					if ((altered & Axes.X) != 0) v.x = visible.x;
					if ((altered & Axes.Y) != 0) v.y = visible.y;
					if ((altered & Axes.Z) != 0) v.z = visible.z;

					t.localEulerAngles = v;
				}
			}
		}
		GUILayout.EndHorizontal();
	}
#endregion
}
#endif

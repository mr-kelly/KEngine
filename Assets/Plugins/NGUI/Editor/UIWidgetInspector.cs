//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Inspector class used to edit UIWidgets.
/// </summary>

[CanEditMultipleObjects]
#if UNITY_3_5
[CustomEditor(typeof(UIWidget))]
#else
[CustomEditor(typeof(UIWidget), true)]
#endif
public class UIWidgetInspector : UIRectEditor
{
	static public UIWidgetInspector instance;

	public enum Action
	{
		None,
		Move,
		Scale,
		Rotate,
	}

	Action mAction = Action.None;
	Action mActionUnderMouse = Action.None;
	bool mAllowSelection = true;

	protected UIWidget mWidget;

	static protected bool mUseShader = false;
	static GUIStyle mBlueDot = null;
	static GUIStyle mYellowDot = null;
	static GUIStyle mRedDot = null;
	static GUIStyle mOrangeDot = null;
	static GUIStyle mGreenDot = null;
	static GUIStyle mGreyDot = null;
	static MouseCursor mCursor = MouseCursor.Arrow;

	static public UIWidget.Pivot[] pivotPoints =
	{
		UIWidget.Pivot.BottomLeft,
		UIWidget.Pivot.TopLeft,
		UIWidget.Pivot.TopRight,
		UIWidget.Pivot.BottomRight,
		UIWidget.Pivot.Left,
		UIWidget.Pivot.Top,
		UIWidget.Pivot.Right,
		UIWidget.Pivot.Bottom,
	};

	static int s_Hash = "WidgetHash".GetHashCode();
	Vector3 mLocalPos = Vector3.zero;
	Vector3 mWorldPos = Vector3.zero;
	int mStartWidth = 0;
	int mStartHeight = 0;
	Vector3 mStartDrag = Vector3.zero;
	Vector2 mStartMouse = Vector2.zero;
	Vector3 mStartRot = Vector3.zero;
	Vector3 mStartDir = Vector3.right;
	Vector2 mStartLeft = Vector2.zero;
	Vector2 mStartRight = Vector2.zero;
	Vector2 mStartBottom = Vector2.zero;
	Vector2 mStartTop = Vector2.zero;
	UIWidget.Pivot mDragPivot = UIWidget.Pivot.Center;

	/// <summary>
	/// Raycast into the screen.
	/// </summary>

	static public bool Raycast (Vector3[] corners, out Vector3 hit)
	{
		Plane plane = new Plane(corners[0], corners[1], corners[2]);
		Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
		float dist = 0f;
		bool isHit = plane.Raycast(ray, out dist);
		hit = isHit ? ray.GetPoint(dist) : Vector3.zero;
		return isHit;
	}

	/// <summary>
	/// Color used by the handles based on the current color scheme.
	/// </summary>

	static public Color handlesColor
	{
		get
		{
			if (NGUISettings.colorMode == NGUISettings.ColorMode.Orange)
			{
				return new Color(1f, 0.5f, 0f);
			}
			else if (NGUISettings.colorMode == NGUISettings.ColorMode.Green)
			{
				return Color.green;
			}
			return Color.white;
		}
	}

	/// <summary>
	/// Draw a control dot at the specified world position.
	/// </summary>

	static public void DrawKnob (Vector3 point, bool selected, bool canResize, int id)
	{
		if (mGreyDot == null) mGreyDot = "sv_label_0";
		if (mBlueDot == null) mBlueDot = "sv_label_1";
		if (mGreenDot == null) mGreenDot = "sv_label_3";
		if (mYellowDot == null) mYellowDot = "sv_label_4";
		if (mOrangeDot == null) mOrangeDot = "sv_label_5";
		if (mRedDot == null) mRedDot = "sv_label_6";

		Vector2 screenPoint = HandleUtility.WorldToGUIPoint(point);

		Rect rect = new Rect(screenPoint.x - 7f, screenPoint.y - 7f, 14f, 14f);

		if (selected)
		{
			if (NGUISettings.colorMode == NGUISettings.ColorMode.Orange)
			{
				mRedDot.Draw(rect, GUIContent.none, id);
			}
			else
			{
				mOrangeDot.Draw(rect, GUIContent.none, id);
			}
		}
		else if (canResize)
		{
			if (NGUISettings.colorMode == NGUISettings.ColorMode.Orange)
			{
				mOrangeDot.Draw(rect, GUIContent.none, id);
			}
			else if (NGUISettings.colorMode == NGUISettings.ColorMode.Green)
			{
				mGreenDot.Draw(rect, GUIContent.none, id);
			}
			else
			{
				mBlueDot.Draw(rect, GUIContent.none, id);
			}
		}
		else mGreyDot.Draw(rect, GUIContent.none, id);
	}

	/// <summary>
	/// Screen-space distance from the mouse position to the specified world position.
	/// </summary>

	static public float GetScreenDistance (Vector3 worldPos, Vector2 mousePos)
	{
		Vector2 screenPos = HandleUtility.WorldToGUIPoint(worldPos);
		return Vector2.Distance(mousePos, screenPos);
	}

	/// <summary>
	/// Closest screen-space distance from the mouse position to one of the specified world points.
	/// </summary>

	static public float GetScreenDistance (Vector3[] worldPoints, Vector2 mousePos, out int index)
	{
		float min = float.MaxValue;
		index = 0;

		for (int i = 0; i < worldPoints.Length; ++i)
		{
			float distance = GetScreenDistance(worldPoints[i], mousePos);
			
			if (distance < min)
			{
				index = i;
				min = distance;
			}
		}
		return min;
	}

	/// <summary>
	/// Set the mouse cursor rectangle, refreshing the screen when it gets changed.
	/// </summary>

	static public void SetCursorRect (Rect rect, MouseCursor cursor)
	{
		EditorGUIUtility.AddCursorRect(rect, cursor);

		if (Event.current.type == EventType.MouseMove)
		{
			if (mCursor != cursor)
			{
				mCursor = cursor;
				Event.current.Use();
			}
		}
	}

	void OnDisable ()
	{
		NGUIEditorTools.HideMoveTool(false);
		instance = null;
	}

	/// <summary>
	/// Convert the specified 4 corners into 8 pivot points (adding left, top, right, bottom -- in that order).
	/// </summary>

	static public Vector3[] GetHandles (Vector3[] corners)
	{
		Vector3[] v = new Vector3[8];

		v[0] = corners[0];
		v[1] = corners[1];
		v[2] = corners[2];
		v[3] = corners[3];

		v[4] = (corners[0] + corners[1]) * 0.5f;
		v[5] = (corners[1] + corners[2]) * 0.5f;
		v[6] = (corners[2] + corners[3]) * 0.5f;
		v[7] = (corners[0] + corners[3]) * 0.5f;

		return v;
	}

	/// <summary>
	/// Determine what kind of pivot point is under the mouse and update the cursor accordingly.
	/// </summary>

	static public UIWidget.Pivot GetPivotUnderMouse (Vector3[] worldPos, Event e, bool[] resizable, bool movable, ref Action action)
	{
		// Time to figure out what kind of action is underneath the mouse
		UIWidget.Pivot pivotUnderMouse = UIWidget.Pivot.Center;

		if (action == Action.None)
		{
			int index = 0;
			float dist = GetScreenDistance(worldPos, e.mousePosition, out index);
			bool alt = (e.modifiers & EventModifiers.Alt) != 0;

			if (resizable[index] && dist < 10f)
			{
				pivotUnderMouse = pivotPoints[index];
				action = Action.Scale;
			}
			else if (!alt && NGUIEditorTools.SceneViewDistanceToRectangle(worldPos, e.mousePosition) == 0f)
			{
				action = movable ? Action.Move : Action.Rotate;
			}
			else if (dist < 30f)
			{
				action = Action.Rotate;
			}
		}

		// Change the mouse cursor to a more appropriate one
#if !UNITY_3_5
		{
			Vector2[] screenPos = new Vector2[8];
			for (int i = 0; i < 8; ++i) screenPos[i] = HandleUtility.WorldToGUIPoint(worldPos[i]);

			Bounds b = new Bounds(screenPos[0], Vector3.zero);
			for (int i = 1; i < 8; ++i) b.Encapsulate(screenPos[i]);

			Vector2 min = b.min;
			Vector2 max = b.max;

			min.x -= 30f;
			max.x += 30f;
			min.y -= 30f;
			max.y += 30f;

			Rect rect = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);

			if (action == Action.Rotate)
			{
				SetCursorRect(rect, MouseCursor.RotateArrow);
			}
			else if (action == Action.Move)
			{
				SetCursorRect(rect, MouseCursor.MoveArrow);
			}
			else if (action == Action.Scale)
			{
				SetCursorRect(rect, MouseCursor.ScaleArrow);
			}
			else SetCursorRect(rect, MouseCursor.Arrow);
		}
#endif
		return pivotUnderMouse;
	}

	/// <summary>
	/// Draw the specified anchor point.
	/// </summary>

	static public void DrawAnchorHandle (UIRect.AnchorPoint anchor, Transform myTrans, Vector3[] myCorners, int side, int id)
	{
		if (!anchor.target) return;

		int i0, i1;

		if (side == 0)
		{
			// Left
			i0 = 0;
			i1 = 1;
		}
		else if (side == 1)
		{
			// Top
			i0 = 1;
			i1 = 2;
		}
		else if (side == 2)
		{
			// Right
			i0 = 3;
			i1 = 2;
		}
		else
		{
			// Bottom
			i0 = 0;
			i1 = 3;
		}

		Vector3 myPos = (myCorners[i0] + myCorners[i1]) * 0.5f;
		Vector3[] sides = null;

		if (anchor.rect != null)
		{
			sides = anchor.rect.worldCorners;
		}
		else if (anchor.target.camera != null)
		{
			sides = anchor.target.camera.GetWorldCorners();
		}

		Vector3 theirPos;

		if (sides != null)
		{
			Vector3 v0, v1;

			if (side == 0 || side == 2)
			{
				// Left or right
				v0 = Vector3.Lerp(sides[0], sides[3], anchor.relative);
				v1 = Vector3.Lerp(sides[1], sides[2], anchor.relative);
			}
			else
			{
				// Top or bottom
				v0 = Vector3.Lerp(sides[0], sides[1], anchor.relative);
				v1 = Vector3.Lerp(sides[3], sides[2], anchor.relative);
			}

			theirPos = HandleUtility.ProjectPointLine(myPos, v0, v1);
		}
		else
		{
			theirPos = anchor.target.position;
		}

		NGUIHandles.DrawShadowedLine(myCorners, myPos, theirPos, Color.yellow);

		if (Event.current.GetTypeForControl(id) == EventType.Repaint)
		{
			Vector2 screenPoint = HandleUtility.WorldToGUIPoint(theirPos);
			Rect rect = new Rect(screenPoint.x - 7f, screenPoint.y - 7f, 14f, 14f);
			if (mYellowDot == null) mYellowDot = "sv_label_4";

			Vector3 v0 = HandleUtility.WorldToGUIPoint(myPos);
			Vector3 v1 = HandleUtility.WorldToGUIPoint(theirPos);

			Handles.BeginGUI();
				
			mYellowDot.Draw(rect, GUIContent.none, id);

			Vector3 diff = v1 - v0;
			bool isHorizontal = Mathf.Abs(diff.x) > Mathf.Abs(diff.y);
			float mag = diff.magnitude;

			if ((isHorizontal && mag > 60f) || (!isHorizontal && mag > 30f))
			{
				Vector3 pos = (myPos + theirPos) * 0.5f;
				string text = anchor.absolute.ToString();

				GUI.color = Color.yellow;

				if (side == 0)
				{
					if (theirPos.x < myPos.x)
						NGUIHandles.DrawCenteredLabel(pos, text);
				}
				else if (side == 1)
				{
					if (theirPos.y > myPos.y)
						NGUIHandles.DrawCenteredLabel(pos, text);
				}
				else if (side == 2)
				{
					if (theirPos.x > myPos.x)
						NGUIHandles.DrawCenteredLabel(pos, text);
				}
				else if (side == 3)
				{
					if (theirPos.y < myPos.y)
						NGUIHandles.DrawCenteredLabel(pos, text);
				}
				GUI.color = Color.white;
			}
			Handles.EndGUI();
		}
	}

	/// <summary>
	/// Draw the on-screen selection, knobs, and handle all interaction logic.
	/// </summary>

	public void OnSceneGUI ()
	{
		NGUIEditorTools.HideMoveTool(true);
		if (!UIWidget.showHandles) return;

		mWidget = target as UIWidget;

		Transform t = mWidget.cachedTransform;

		Event e = Event.current;
		int id = GUIUtility.GetControlID(s_Hash, FocusType.Passive);
		EventType type = e.GetTypeForControl(id);

		Action actionUnderMouse = mAction;
		Vector3[] handles = GetHandles(mWidget.worldCorners);
		
		NGUIHandles.DrawShadowedLine(handles, handles[0], handles[1], handlesColor);
		NGUIHandles.DrawShadowedLine(handles, handles[1], handles[2], handlesColor);
		NGUIHandles.DrawShadowedLine(handles, handles[2], handles[3], handlesColor);
		NGUIHandles.DrawShadowedLine(handles, handles[0], handles[3], handlesColor);

		// If the widget is anchored, draw the anchors
		if (mWidget.isAnchored)
		{
			DrawAnchorHandle(mWidget.leftAnchor, mWidget.cachedTransform, handles, 0, id);
			DrawAnchorHandle(mWidget.topAnchor, mWidget.cachedTransform, handles, 1, id);
			DrawAnchorHandle(mWidget.rightAnchor, mWidget.cachedTransform, handles, 2, id);
			DrawAnchorHandle(mWidget.bottomAnchor, mWidget.cachedTransform, handles, 3, id);
		}

		if (type == EventType.Repaint)
		{
			bool showDetails = (mAction == UIWidgetInspector.Action.Scale) || NGUISettings.drawGuides;
			if (mAction == UIWidgetInspector.Action.None && e.modifiers == EventModifiers.Control) showDetails = true;
			if (NGUITools.GetActive(mWidget) && mWidget.parent == null) showDetails = true;
			if (showDetails) NGUIHandles.DrawSize(handles, mWidget.width, mWidget.height);
		}

		// Presence of the legacy stretch component prevents resizing
		bool canResize = (mWidget.GetComponent<UIStretch>() == null);
		bool[] resizable = new bool[8];

		resizable[4] = canResize;	// left
		resizable[5] = canResize;	// top
		resizable[6] = canResize;	// right
		resizable[7] = canResize;	// bottom

		UILabel lbl = mWidget as UILabel;
		
		if (lbl != null)
		{
			if (lbl.overflowMethod == UILabel.Overflow.ResizeFreely)
			{
				resizable[4] = false;	// left
				resizable[5] = false;	// top
				resizable[6] = false;	// right
				resizable[7] = false;	// bottom
			}
			else if (lbl.overflowMethod == UILabel.Overflow.ResizeHeight)
			{
				resizable[5] = false;	// top
				resizable[7] = false;	// bottom
			}
		}

		if (mWidget.keepAspectRatio == UIWidget.AspectRatioSource.BasedOnHeight)
		{
			resizable[4] = false;
			resizable[6] = false;
		}
		else if (mWidget.keepAspectRatio == UIWidget.AspectRatioSource.BasedOnWidth)
		{
			resizable[5] = false;
			resizable[7] = false;
		}

		resizable[0] = resizable[7] && resizable[4]; // bottom-left
		resizable[1] = resizable[5] && resizable[4]; // top-left
		resizable[2] = resizable[5] && resizable[6]; // top-right
		resizable[3] = resizable[7] && resizable[6]; // bottom-right
		
		UIWidget.Pivot pivotUnderMouse = GetPivotUnderMouse(handles, e, resizable, true, ref actionUnderMouse);
		
		switch (type)
		{
			case EventType.Repaint:
			{
				Vector3 v0 = HandleUtility.WorldToGUIPoint(handles[0]);
				Vector3 v2 = HandleUtility.WorldToGUIPoint(handles[2]);
				
				if ((v2 - v0).magnitude > 60f)
				{
					Vector3 v1 = HandleUtility.WorldToGUIPoint(handles[1]);
					Vector3 v3 = HandleUtility.WorldToGUIPoint(handles[3]);

					Handles.BeginGUI();
					{
						for (int i = 0; i < 4; ++i)
							DrawKnob(handles[i], mWidget.pivot == pivotPoints[i], resizable[i], id);

						if ((v1 - v0).magnitude > 80f)
						{
							if (mWidget.leftAnchor.target == null || mWidget.leftAnchor.absolute != 0)
								DrawKnob(handles[4], mWidget.pivot == pivotPoints[4], resizable[4], id);

							if (mWidget.rightAnchor.target == null || mWidget.rightAnchor.absolute != 0)
								DrawKnob(handles[6], mWidget.pivot == pivotPoints[6], resizable[6], id);
						}

						if ((v3 - v0).magnitude > 80f)
						{
							if (mWidget.topAnchor.target == null || mWidget.topAnchor.absolute != 0)
								DrawKnob(handles[5], mWidget.pivot == pivotPoints[5], resizable[5], id);

							if (mWidget.bottomAnchor.target == null || mWidget.bottomAnchor.absolute != 0)
								DrawKnob(handles[7], mWidget.pivot == pivotPoints[7], resizable[7], id);
						}
					}
					Handles.EndGUI();
				}
			}
			break;

			case EventType.MouseDown:
			{
				if (actionUnderMouse != Action.None)
				{
					mStartMouse = e.mousePosition;
					mAllowSelection = true;

					if (e.button == 1)
					{
						if (e.modifiers == 0)
						{
							GUIUtility.hotControl = GUIUtility.keyboardControl = id;
							e.Use();
						}
					}
					else if (e.button == 0 && actionUnderMouse != Action.None && Raycast(handles, out mStartDrag))
					{
						mWorldPos = t.position;
						mLocalPos = t.localPosition;
						mStartRot = t.localRotation.eulerAngles;
						mStartDir = mStartDrag - t.position;
						mStartWidth = mWidget.width;
						mStartHeight = mWidget.height;
						mStartLeft.x = mWidget.leftAnchor.relative;
						mStartLeft.y = mWidget.leftAnchor.absolute;
						mStartRight.x = mWidget.rightAnchor.relative;
						mStartRight.y = mWidget.rightAnchor.absolute;
						mStartBottom.x = mWidget.bottomAnchor.relative;
						mStartBottom.y = mWidget.bottomAnchor.absolute;
						mStartTop.x = mWidget.topAnchor.relative;
						mStartTop.y = mWidget.topAnchor.absolute;

						mDragPivot = pivotUnderMouse;
						mActionUnderMouse = actionUnderMouse;
						GUIUtility.hotControl = GUIUtility.keyboardControl = id;
						e.Use();
					}
				}
			}
			break;

			case EventType.MouseDrag:
			{
				// Prevent selection once the drag operation begins
				bool dragStarted = (e.mousePosition - mStartMouse).magnitude > 3f;
				if (dragStarted) mAllowSelection = false;

				if (GUIUtility.hotControl == id)
				{
					e.Use();

					if (mAction != Action.None || mActionUnderMouse != Action.None)
					{
						Vector3 pos;

						if (Raycast(handles, out pos))
						{
							if (mAction == Action.None && mActionUnderMouse != Action.None)
							{
								// Wait until the mouse moves by more than a few pixels
								if (dragStarted)
								{
									if (mActionUnderMouse == Action.Move)
									{
										NGUISnap.Recalculate(mWidget);
									}
									else if (mActionUnderMouse == Action.Rotate)
									{
										mStartRot = t.localRotation.eulerAngles;
										mStartDir = mStartDrag - t.position;
									}
									else if (mActionUnderMouse == Action.Scale)
									{
										mStartWidth = mWidget.width;
										mStartHeight = mWidget.height;
										mDragPivot = pivotUnderMouse;
									}
									mAction = actionUnderMouse;
								}
							}

							if (mAction != Action.None)
							{
								NGUIEditorTools.RegisterUndo("Change Rect", t);
								NGUIEditorTools.RegisterUndo("Change Rect", mWidget);

								// Reset the widget before adjusting anything
								t.position = mWorldPos;
								mWidget.width = mStartWidth;
								mWidget.height = mStartHeight;
								mWidget.leftAnchor.Set(mStartLeft.x, mStartLeft.y);
								mWidget.rightAnchor.Set(mStartRight.x, mStartRight.y);
								mWidget.bottomAnchor.Set(mStartBottom.x, mStartBottom.y);
								mWidget.topAnchor.Set(mStartTop.x, mStartTop.y);

								if (mAction == Action.Move)
								{
									// Move the widget
									t.position = mWorldPos + (pos - mStartDrag);

									// Snap the widget
									Vector3 after = NGUISnap.Snap(t.localPosition, mWidget.localCorners, e.modifiers != EventModifiers.Control);

									// Calculate the final delta
									Vector3 localDelta = (after - mLocalPos);

									// Restore the position
									t.position = mWorldPos;

									// Adjust the widget by the delta
									NGUIMath.MoveRect(mWidget, localDelta.x, localDelta.y);
								}
								else if (mAction == Action.Rotate)
								{
									Vector3 dir = pos - t.position;
									float angle = Vector3.Angle(mStartDir, dir);

									if (angle > 0f)
									{
										float dot = Vector3.Dot(Vector3.Cross(mStartDir, dir), t.forward);
										if (dot < 0f) angle = -angle;
										angle = mStartRot.z + angle;
										angle = (NGUISnap.allow && e.modifiers != EventModifiers.Control) ?
											Mathf.Round(angle / 15f) * 15f : Mathf.Round(angle);
										t.localRotation = Quaternion.Euler(mStartRot.x, mStartRot.y, angle);
									}
								}
								else if (mAction == Action.Scale)
								{
									// Move the widget
									t.position = mWorldPos + (pos - mStartDrag);

									// Calculate the final delta
									Vector3 localDelta = (t.localPosition - mLocalPos);

									// Restore the position
									t.position = mWorldPos;

									// Adjust the widget's position and scale based on the delta, restricted by the pivot
									NGUIMath.ResizeWidget(mWidget, mDragPivot, localDelta.x, localDelta.y, 2, 2);
									ReEvaluateAnchorType();
								}
							}
						}
					}
				}
			}
			break;

			case EventType.MouseUp:
			{
				if (e.button == 2) break;
				if (GUIUtility.hotControl == id)
				{
					GUIUtility.hotControl = 0;
					GUIUtility.keyboardControl = 0;

					if (e.button < 2)
					{
						bool handled = false;

						if (e.button == 1)
						{
							// Right-click: Open a context menu listing all widgets underneath
							NGUIEditorTools.ShowSpriteSelectionMenu(e.mousePosition);
							handled = true;
						}
						else if (mAction == Action.None)
						{
							if (mAllowSelection)
							{
								// Left-click: Select the topmost widget
								NGUIEditorTools.SelectWidget(e.mousePosition);
								handled = true;
							}
						}
						else
						{
							// Finished dragging something
							Vector3 pos = t.localPosition;
							pos.x = Mathf.Round(pos.x);
							pos.y = Mathf.Round(pos.y);
							pos.z = Mathf.Round(pos.z);
							t.localPosition = pos;
							handled = true;
						}

						if (handled) e.Use();
					}

					// Clear the actions
					mActionUnderMouse = Action.None;
					mAction = Action.None;
				}
				else if (mAllowSelection)
				{
					BetterList<UIWidget> widgets = NGUIEditorTools.SceneViewRaycast(e.mousePosition);
					if (widgets.size > 0) Selection.activeGameObject = widgets[0].gameObject;
				}
				mAllowSelection = true;
			}
			break;

			case EventType.KeyDown:
			{
				if (e.keyCode == KeyCode.UpArrow)
				{
					NGUIEditorTools.RegisterUndo("Nudge Rect", t);
					NGUIEditorTools.RegisterUndo("Nudge Rect", mWidget);
					NGUIMath.MoveRect(mWidget, 0f, 1f);
					e.Use();
				}
				else if (e.keyCode == KeyCode.DownArrow)
				{
					NGUIEditorTools.RegisterUndo("Nudge Rect", t);
					NGUIEditorTools.RegisterUndo("Nudge Rect", mWidget);
					NGUIMath.MoveRect(mWidget, 0f, -1f);
					e.Use();
				}
				else if (e.keyCode == KeyCode.LeftArrow)
				{
					NGUIEditorTools.RegisterUndo("Nudge Rect", t);
					NGUIEditorTools.RegisterUndo("Nudge Rect", mWidget);
					NGUIMath.MoveRect(mWidget, -1f, 0f);
					e.Use();
				}
				else if (e.keyCode == KeyCode.RightArrow)
				{
					NGUIEditorTools.RegisterUndo("Nudge Rect", t);
					NGUIEditorTools.RegisterUndo("Nudge Rect", mWidget);
					NGUIMath.MoveRect(mWidget, 1f, 0f);
					e.Use();
				}
				else if (e.keyCode == KeyCode.Escape)
				{
					if (GUIUtility.hotControl == id)
					{
						if (mAction != Action.None)
							Undo.PerformUndo();

						GUIUtility.hotControl = 0;
						GUIUtility.keyboardControl = 0;

						mActionUnderMouse = Action.None;
						mAction = Action.None;
						e.Use();
					}
					else Selection.activeGameObject = null;
				}
			}
			break;
		}
	}

	/// <summary>
	/// Cache the reference.
	/// </summary>

	protected override void OnEnable ()
	{
		base.OnEnable();
		instance = this;
		mWidget = target as UIWidget;
	}

	/// <summary>
	/// By default all non-widgets should use their color.
	/// </summary>

	protected virtual bool drawColor
	{
		get
		{
			return (target.GetType() != typeof(UIWidget));
		}
	}

	/// <summary>
	/// All widgets have depth, color and make pixel-perfect options
	/// </summary>

	protected override void DrawCustomProperties ()
	{
		PrefabType type = PrefabUtility.GetPrefabType(mWidget.gameObject);

		if (NGUIEditorTools.DrawHeader("Widget"))
		{
			NGUIEditorTools.BeginContents();

			if (drawColor)
			{
#if UNITY_3_5 || UNITY_4_0 || UNITY_4_1
				// Color tint
				GUILayout.BeginHorizontal();
				SerializedProperty sp = NGUIEditorTools.DrawProperty("Color", serializedObject, "mColor", GUILayout.MinWidth(20f));
				if (GUILayout.Button("Copy", GUILayout.Width(50f)))
					NGUISettings.color = sp.colorValue;
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				NGUISettings.color = EditorGUILayout.ColorField("Clipboard", NGUISettings.color);
				if (GUILayout.Button("Paste", GUILayout.Width(50f)))
					sp.colorValue = NGUISettings.color;
				GUILayout.EndHorizontal();
				GUILayout.Space(6f);
#else
				NGUIEditorTools.DrawProperty("Color", serializedObject, "mColor", GUILayout.MinWidth(20f));
#endif
			}
			else if (serializedObject.isEditingMultipleObjects)
			{
				NGUIEditorTools.DrawProperty("Alpha", serializedObject, "mColor.a", GUILayout.Width(120f));
			}
			else
			{
				GUI.changed = false;
				float alpha = EditorGUILayout.Slider("Alpha", mWidget.alpha, 0f, 1f);

				if (GUI.changed)
				{
					NGUIEditorTools.RegisterUndo("Alpha change", mWidget);
					mWidget.alpha = alpha;
				}
			}

			DrawPivot();
			DrawDepth(type == PrefabType.Prefab);
			DrawDimensions(type == PrefabType.Prefab);

			SerializedProperty ratio = serializedObject.FindProperty("aspectRatio");
			SerializedProperty aspect = serializedObject.FindProperty("keepAspectRatio");

			GUILayout.BeginHorizontal();
			{
				if (!aspect.hasMultipleDifferentValues && aspect.intValue == 0)
				{
					EditorGUI.BeginDisabledGroup(true);
					NGUIEditorTools.DrawProperty("Aspect Ratio", ratio, false, GUILayout.Width(130f));
					EditorGUI.EndDisabledGroup();
				}
				else NGUIEditorTools.DrawProperty("Aspect Ratio", ratio, false, GUILayout.Width(130f));

				NGUIEditorTools.DrawProperty("", aspect, false, GUILayout.MinWidth(20f));
			}
			GUILayout.EndHorizontal();

			if (serializedObject.isEditingMultipleObjects || mWidget.hasBoxCollider)
			{
				GUILayout.BeginHorizontal();
				{
					NGUIEditorTools.DrawProperty("Box Collider", serializedObject, "autoResizeBoxCollider", GUILayout.Width(100f));
					GUILayout.Label("auto-adjust to match");
				}
				GUILayout.EndHorizontal();
			}
			NGUIEditorTools.EndContents();
		}
	}

	/// <summary>
	/// Draw widget's dimensions.
	/// </summary>

	void DrawDimensions (bool isPrefab)
	{
		GUILayout.BeginHorizontal();
		{
			bool freezeSize = serializedObject.isEditingMultipleObjects;

			UILabel lbl = mWidget as UILabel;

			if (!freezeSize && lbl) freezeSize = (lbl.overflowMethod == UILabel.Overflow.ResizeFreely);

			if (freezeSize)
			{
				EditorGUI.BeginDisabledGroup(true);
				NGUIEditorTools.DrawProperty("Dimensions", serializedObject, "mWidth", GUILayout.MinWidth(100f));
				EditorGUI.EndDisabledGroup();
			}
			else
			{
				GUI.changed = false;
				int val = EditorGUILayout.IntField("Dimensions", mWidget.width, GUILayout.MinWidth(100f));

				if (GUI.changed)
				{
					NGUIEditorTools.RegisterUndo("Dimensions Change", mWidget);
					mWidget.width = val;
				}
			}

			if (!freezeSize && lbl)
			{
				UILabel.Overflow ov = lbl.overflowMethod;
				freezeSize = (ov == UILabel.Overflow.ResizeFreely || ov == UILabel.Overflow.ResizeHeight);
			}

			NGUIEditorTools.SetLabelWidth(12f);

			if (freezeSize)
			{
				EditorGUI.BeginDisabledGroup(true);
				NGUIEditorTools.DrawProperty("x", serializedObject, "mHeight", GUILayout.MinWidth(30f));
				EditorGUI.EndDisabledGroup();
			}
			else
			{
				GUI.changed = false;
				int val = EditorGUILayout.IntField("x", mWidget.height, GUILayout.MinWidth(30f));

				if (GUI.changed)
				{
					NGUIEditorTools.RegisterUndo("Dimensions Change", mWidget);
					mWidget.height = val;
				}
			}

			NGUIEditorTools.SetLabelWidth(80f);

			if (isPrefab)
			{
				GUILayout.Space(70f);
			}
			else
			{
				EditorGUI.BeginDisabledGroup(serializedObject.isEditingMultipleObjects);

				if (GUILayout.Button("Snap", GUILayout.Width(68f)))
				{
					foreach (GameObject go in Selection.gameObjects)
					{
						UIWidget w = go.GetComponent<UIWidget>();

						if (w != null)
						{
							NGUIEditorTools.RegisterUndo("Snap Dimensions", w);
							NGUIEditorTools.RegisterUndo("Snap Dimensions", w.transform);
							w.MakePixelPerfect();
						}
					}
				}
				EditorGUI.EndDisabledGroup();
			}
		}
		GUILayout.EndHorizontal();
	}

	/// <summary>
	/// Draw widget's depth.
	/// </summary>

	void DrawDepth (bool isPrefab)
	{
		if (isPrefab) return;

		GUILayout.Space(2f);
		GUILayout.BeginHorizontal();
		{
			EditorGUILayout.PrefixLabel("Depth");

			if (GUILayout.Button("Back", GUILayout.MinWidth(46f)))
			{
				foreach (GameObject go in Selection.gameObjects)
				{
					UIWidget w = go.GetComponent<UIWidget>();
					if (w != null) w.depth = w.depth - 1;
				}
			}

			NGUIEditorTools.DrawProperty("", serializedObject, "mDepth", GUILayout.MinWidth(20f));

			if (GUILayout.Button("Forward", GUILayout.MinWidth(60f)))
			{
				foreach (GameObject go in Selection.gameObjects)
				{
					UIWidget w = go.GetComponent<UIWidget>();
					if (w != null) w.depth = w.depth + 1;
				}
			}
		}
		GUILayout.EndHorizontal();

		int matchingDepths = 1;

		UIPanel p = mWidget.panel;

		if (p != null)
		{
			for (int i = 0; i < p.widgets.size; ++i)
			{
				UIWidget w = p.widgets[i];
				if (w != mWidget && w.depth == mWidget.depth)
					++matchingDepths;
			}
		}

		if (matchingDepths > 1)
		{
			EditorGUILayout.HelpBox(matchingDepths + " widgets are sharing the depth value of " + mWidget.depth, MessageType.Info);
		}
	}

	/// <summary>
	/// Draw the widget's pivot.
	/// </summary>

	void DrawPivot ()
	{
		SerializedProperty pv = serializedObject.FindProperty("mPivot");

		if (pv.hasMultipleDifferentValues)
		{
			// TODO: Doing this doesn't keep the widget's position where it was. Another approach is needed.
			NGUIEditorTools.DrawProperty("Pivot", serializedObject, "mPivot");
		}
		else
		{
			// Pivot point -- the new, more visual style
			GUILayout.BeginHorizontal();
			GUILayout.Label("Pivot", GUILayout.Width(76f));
#if !UNITY_3_5
			Toggle("\u25C4", "ButtonLeft", UIWidget.Pivot.Left, true);
			Toggle("\u25AC", "ButtonMid", UIWidget.Pivot.Center, true);
			Toggle("\u25BA", "ButtonRight", UIWidget.Pivot.Right, true);
#else
			Toggle("<", "ButtonLeft", UIWidget.Pivot.Left, true);
			Toggle("--", "ButtonMid", UIWidget.Pivot.Center, true);
			Toggle(">", "ButtonRight", UIWidget.Pivot.Right, true);
#endif
			Toggle("\u25B2", "ButtonLeft", UIWidget.Pivot.Top, false);
			Toggle("\u258C", "ButtonMid", UIWidget.Pivot.Center, false);
			Toggle("\u25BC", "ButtonRight", UIWidget.Pivot.Bottom, false);

			GUILayout.EndHorizontal();
			pv.enumValueIndex = (int)mWidget.pivot;
		}
	}

	/// <summary>
	/// Draw a toggle button for the pivot point.
	/// </summary>

	void Toggle (string text, string style, UIWidget.Pivot pivot, bool isHorizontal)
	{
		bool isActive = false;

		switch (pivot)
		{
			case UIWidget.Pivot.Left:
			isActive = IsLeft(mWidget.pivot);
			break;

			case UIWidget.Pivot.Right:
			isActive = IsRight(mWidget.pivot);
			break;

			case UIWidget.Pivot.Top:
			isActive = IsTop(mWidget.pivot);
			break;

			case UIWidget.Pivot.Bottom:
			isActive = IsBottom(mWidget.pivot);
			break;

			case UIWidget.Pivot.Center:
			isActive = isHorizontal ? pivot == GetHorizontal(mWidget.pivot) : pivot == GetVertical(mWidget.pivot);
			break;
		}

		if (GUILayout.Toggle(isActive, text, style) != isActive)
			SetPivot(pivot, isHorizontal);
	}

	static bool IsLeft (UIWidget.Pivot pivot)
	{
		return pivot == UIWidget.Pivot.Left ||
			pivot == UIWidget.Pivot.TopLeft ||
			pivot == UIWidget.Pivot.BottomLeft;
	}

	static bool IsRight (UIWidget.Pivot pivot)
	{
		return pivot == UIWidget.Pivot.Right ||
			pivot == UIWidget.Pivot.TopRight ||
			pivot == UIWidget.Pivot.BottomRight;
	}

	static bool IsTop (UIWidget.Pivot pivot)
	{
		return pivot == UIWidget.Pivot.Top ||
			pivot == UIWidget.Pivot.TopLeft ||
			pivot == UIWidget.Pivot.TopRight;
	}

	static bool IsBottom (UIWidget.Pivot pivot)
	{
		return pivot == UIWidget.Pivot.Bottom ||
			pivot == UIWidget.Pivot.BottomLeft ||
			pivot == UIWidget.Pivot.BottomRight;
	}

	static UIWidget.Pivot GetHorizontal (UIWidget.Pivot pivot)
	{
		if (IsLeft(pivot)) return UIWidget.Pivot.Left;
		if (IsRight(pivot)) return UIWidget.Pivot.Right;
		return UIWidget.Pivot.Center;
	}

	static UIWidget.Pivot GetVertical (UIWidget.Pivot pivot)
	{
		if (IsTop(pivot)) return UIWidget.Pivot.Top;
		if (IsBottom(pivot)) return UIWidget.Pivot.Bottom;
		return UIWidget.Pivot.Center;
	}

	static UIWidget.Pivot Combine (UIWidget.Pivot horizontal, UIWidget.Pivot vertical)
	{
		if (horizontal == UIWidget.Pivot.Left)
		{
			if (vertical == UIWidget.Pivot.Top) return UIWidget.Pivot.TopLeft;
			if (vertical == UIWidget.Pivot.Bottom) return UIWidget.Pivot.BottomLeft;
			return UIWidget.Pivot.Left;
		}

		if (horizontal == UIWidget.Pivot.Right)
		{
			if (vertical == UIWidget.Pivot.Top) return UIWidget.Pivot.TopRight;
			if (vertical == UIWidget.Pivot.Bottom) return UIWidget.Pivot.BottomRight;
			return UIWidget.Pivot.Right;
		}
		return vertical;
	}

	void SetPivot (UIWidget.Pivot pivot, bool isHorizontal)
	{
		UIWidget.Pivot horizontal = GetHorizontal(mWidget.pivot);
		UIWidget.Pivot vertical = GetVertical(mWidget.pivot);

		pivot = isHorizontal ? Combine(pivot, vertical) : Combine(horizontal, pivot);

		if (mWidget.pivot != pivot)
		{
			NGUIEditorTools.RegisterUndo("Pivot change", mWidget);
			mWidget.pivot = pivot;
		}
	}

	protected override void OnDrawFinalProperties ()
	{
		if (mAnchorType == AnchorType.Advanced || !mWidget.isAnchored) return;

		SerializedProperty sp = serializedObject.FindProperty("leftAnchor.target");

		if (!IsRect(sp))
		{
			GUILayout.Space(3f);
			GUILayout.BeginHorizontal();
			GUILayout.Space(6f);
			NGUIEditorTools.DrawProperty("", serializedObject, "hideIfOffScreen", GUILayout.Width(18f));
			GUILayout.Label("Hide if off-screen", GUILayout.MinWidth(20f));
			GUILayout.EndHorizontal();
		}
	}
}

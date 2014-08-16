//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Extended progress bar that has backwards compatibility logic and adds interaction support.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/Interaction/NGUI Slider")]
public class UISlider : UIProgressBar
{
	enum Direction
	{
		Horizontal,
		Vertical,
		Upgraded,
	}

	// Deprecated functionality. Use 'foregroundWidget' instead.
	[HideInInspector][SerializeField] Transform foreground = null;

	// Deprecated functionality
	[HideInInspector][SerializeField] float rawValue = 1f; // Use 'value'
	[HideInInspector][SerializeField] Direction direction = Direction.Upgraded; // Use 'fillDirection'
	[HideInInspector][SerializeField] protected bool mInverted = false;

	[System.Obsolete("Use 'value' instead")]
	public float sliderValue { get { return this.value; } set { this.value = value; } }

	[System.Obsolete("Use 'fillDirection' instead")]
	public bool inverted { get { return isInverted; } set { } }

	/// <summary>
	/// Upgrade from legacy functionality.
	/// </summary>

	protected override void Upgrade ()
	{
		if (direction != Direction.Upgraded)
		{
			mValue = rawValue;

			if (foreground != null)
				mFG = foreground.GetComponent<UIWidget>();

			if (direction == Direction.Horizontal)
			{
				mFill = mInverted ? FillDirection.RightToLeft : FillDirection.LeftToRight;
			}
			else
			{
				mFill = mInverted ? FillDirection.TopToBottom : FillDirection.BottomToTop;
			}
			direction = Direction.Upgraded;
#if UNITY_EDITOR
			NGUITools.SetDirty(this);
#endif
		}
	}

	/// <summary>
	/// Register an event listener.
	/// </summary>

	protected override void OnStart ()
	{
		GameObject bg = (mBG != null && mBG.collider != null) ? mBG.gameObject : gameObject;
		UIEventListener bgl = UIEventListener.Get(bg);
		bgl.onPress += OnPressBackground;
		bgl.onDrag += OnDragBackground;

		if (thumb != null && thumb.collider != null && (mFG == null || thumb != mFG.cachedTransform))
		{
			UIEventListener fgl = UIEventListener.Get(thumb.gameObject);
			fgl.onPress += OnPressForeground;
			fgl.onDrag += OnDragForeground;
		}
	}

	/// <summary>
	/// Position the scroll bar to be under the current touch.
	/// </summary>

	protected void OnPressBackground (GameObject go, bool isPressed)
	{
		if (UICamera.currentScheme == UICamera.ControlScheme.Controller) return;
		mCam = UICamera.currentCamera;
		value = ScreenToValue(UICamera.lastTouchPosition);
		if (!isPressed && onDragFinished != null) onDragFinished();
	}

	/// <summary>
	/// Position the scroll bar to be under the current touch.
	/// </summary>

	protected void OnDragBackground (GameObject go, Vector2 delta)
	{
		if (UICamera.currentScheme == UICamera.ControlScheme.Controller) return;
		mCam = UICamera.currentCamera;
		value = ScreenToValue(UICamera.lastTouchPosition);
	}

	/// <summary>
	/// Save the position of the foreground on press.
	/// </summary>

	protected void OnPressForeground (GameObject go, bool isPressed)
	{
		if (UICamera.currentScheme == UICamera.ControlScheme.Controller) return;

		if (isPressed)
		{
			mOffset = (mFG == null) ? 0f :
				value - ScreenToValue(UICamera.lastTouchPosition);
		}
		else if (onDragFinished != null) onDragFinished();
	}

	/// <summary>
	/// Drag the scroll bar in the specified direction.
	/// </summary>

	protected void OnDragForeground (GameObject go, Vector2 delta)
	{
		if (UICamera.currentScheme == UICamera.ControlScheme.Controller) return;
		mCam = UICamera.currentCamera;
		value = mOffset + ScreenToValue(UICamera.lastTouchPosition);
	}

	/// <summary>
	/// Watch for key events and adjust the value accordingly.
	/// </summary>

	protected void OnKey (KeyCode key)
	{
		if (enabled)
		{
			float step = (numberOfSteps > 1f) ? 1f / (numberOfSteps - 1) : 0.125f;

			if (fillDirection == FillDirection.LeftToRight || fillDirection == FillDirection.RightToLeft)
			{
				if (key == KeyCode.LeftArrow) value = mValue - step;
				else if (key == KeyCode.RightArrow) value = mValue + step;
			}
			else
			{
				if (key == KeyCode.DownArrow) value = mValue - step;
				else if (key == KeyCode.UpArrow) value = mValue + step;
			}
		}
	}
}

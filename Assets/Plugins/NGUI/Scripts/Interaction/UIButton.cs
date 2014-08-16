//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Similar to UIButtonColor, but adds a 'disabled' state based on whether the collider is enabled or not.
/// </summary>

[AddComponentMenu("NGUI/Interaction/Button")]
public class UIButton : UIButtonColor
{
	/// <summary>
	/// Current button that sent out the onClick event.
	/// </summary>

	static public UIButton current;

	/// <summary>
	/// Whether the button will highlight when you drag something over it.
	/// </summary>

	public bool dragHighlight = false;

	/// <summary>
	/// Name of the hover state sprite.
	/// </summary>

	public string hoverSprite;

	/// <summary>
	/// Name of the pressed sprite.
	/// </summary>

	public string pressedSprite;

	/// <summary>
	/// Name of the disabled sprite.
	/// </summary>

	public string disabledSprite;

	/// <summary>
	/// Whether the sprite changes will elicit a call to MakePixelPerfect() or not.
	/// </summary>

	public bool pixelSnap = false;

	/// <summary>
	/// Click event listener.
	/// </summary>

	public List<EventDelegate> onClick = new List<EventDelegate>();

	// Cached value
	[System.NonSerialized] string mNormalSprite;
	[System.NonSerialized] UISprite mSprite;

	/// <summary>
	/// Whether the button should be enabled.
	/// </summary>

	public override bool isEnabled
	{
		get
		{
			if (!enabled) return false;
			Collider col = collider;
			if (col && col.enabled) return true;
#if !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
			Collider2D c2d = GetComponent<Collider2D>();
			return (c2d && c2d.enabled);
#else
			return false;
#endif
		}
		set
		{
			if (isEnabled != value)
			{
				Collider col = collider;

				if (col != null)
				{
					col.enabled = value;
					SetState(value ? State.Normal : State.Disabled, false);
				}
#if !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
				else
				{
					Collider2D c2d = GetComponent<Collider2D>();

					if (c2d != null)
					{
						c2d.enabled = value;
						SetState(value ? State.Normal : State.Disabled, false);
					}
					else enabled = value;
				}
#else
				else enabled = value;
#endif
			}
		}
	}

	/// <summary>
	/// Convenience function that changes the normal sprite.
	/// </summary>

	public string normalSprite
	{
		get
		{
			if (!mInitDone) OnInit();
			return mNormalSprite;
		}
		set
		{
			if (mSprite != null && !string.IsNullOrEmpty(mNormalSprite) && mNormalSprite == mSprite.spriteName)
			{
				mNormalSprite = value;
				SetSprite(value);
				NGUITools.SetDirty(mSprite);
			}
			else
			{
				mNormalSprite = value;
				if (mState == State.Normal) SetSprite(value);
			}
		}
	}

	/// <summary>
	/// Cache the sprite we'll be working with.
	/// </summary>

	protected override void OnInit ()
	{
		base.OnInit();
		mSprite = (mWidget as UISprite);
		if (mSprite != null) mNormalSprite = mSprite.spriteName;
	}

	/// <summary>
	/// Set the initial state.
	/// </summary>

	protected override void OnEnable ()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying)
		{
			mInitDone = false;
			return;
		}
#endif
		if (isEnabled)
		{
			if (mInitDone)
			{
				if (UICamera.currentScheme == UICamera.ControlScheme.Controller)
				{
					OnHover(UICamera.selectedObject == gameObject);
				}
				else if (UICamera.currentScheme == UICamera.ControlScheme.Mouse)
				{
					OnHover(UICamera.hoveredObject == gameObject);
				}
				else SetState(State.Normal, false);
			}
		}
		else SetState(State.Disabled, true);
	}

	/// <summary>
	/// Drag over state logic is a bit different for the button.
	/// </summary>
	
	protected override void OnDragOver ()
	{
		if (isEnabled && (dragHighlight || UICamera.currentTouch.pressed == gameObject))
			base.OnDragOver();
	}

	/// <summary>
	/// Drag out state logic is a bit different for the button.
	/// </summary>
	
	protected override void OnDragOut ()
	{
		if (isEnabled && (dragHighlight || UICamera.currentTouch.pressed == gameObject))
			base.OnDragOut();
	}

	/// <summary>
	/// Call the listener function.
	/// </summary>

	protected virtual void OnClick ()
	{
		if (current == null && isEnabled)
		{
			current = this;
			EventDelegate.Execute(onClick);
			current = null;
		}
	}

	/// <summary>
	/// Change the visual state.
	/// </summary>

	public override void SetState (State state, bool immediate)
	{
		base.SetState(state, immediate);

		switch (state)
		{
			case State.Normal: SetSprite(mNormalSprite); break;
			case State.Hover: SetSprite(hoverSprite); break;
			case State.Pressed: SetSprite(pressedSprite); break;
			case State.Disabled: SetSprite(disabledSprite); break;
		}
	}

	/// <summary>
	/// Convenience function that changes the sprite.
	/// </summary>

	protected void SetSprite (string sp)
	{
		if (mSprite != null && !string.IsNullOrEmpty(sp) && mSprite.spriteName != sp)
		{
			mSprite.spriteName = sp;
			if (pixelSnap) mSprite.MakePixelPerfect();
		}
	}
}

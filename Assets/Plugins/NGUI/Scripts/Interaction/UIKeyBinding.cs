//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;

/// <summary>
/// This class makes it possible to activate or select something by pressing a key (such as space bar for example).
/// </summary>

[AddComponentMenu("NGUI/Interaction/Key Binding")]
public class UIKeyBinding : MonoBehaviour
{
	public enum Action
	{
		PressAndClick,
		Select,
	}

	public enum Modifier
	{
		None,
		Shift,
		Control,
		Alt,
	}

	/// <summary>
	/// Key that will trigger the binding.
	/// </summary>

	public KeyCode keyCode = KeyCode.None;

	/// <summary>
	/// Modifier key that must be active in order for the binding to trigger.
	/// </summary>

	public Modifier modifier = Modifier.None;

	/// <summary>
	/// Action to take with the specified key.
	/// </summary>

	public Action action = Action.PressAndClick;

	bool mIgnoreUp = false;
	bool mIsInput = false;

	/// <summary>
	/// If we're bound to an input field, subscribe to its Submit notification.
	/// </summary>

	void Start ()
	{
		UIInput input = GetComponent<UIInput>();
		mIsInput = (input != null);
		if (input != null) EventDelegate.Add(input.onSubmit, OnSubmit);
	}

	/// <summary>
	/// Ignore the KeyUp message if the input field "ate" it.
	/// </summary>

	void OnSubmit () { if (UICamera.currentKey == keyCode && IsModifierActive()) mIgnoreUp = true; }

	/// <summary>
	/// Convenience function that checks whether the required modifier key is active.
	/// </summary>

	bool IsModifierActive ()
	{
		if (modifier == Modifier.None) return true;

		if (modifier == Modifier.Alt)
		{
			if (Input.GetKey(KeyCode.LeftAlt) ||
				Input.GetKey(KeyCode.RightAlt)) return true;
		}
		else if (modifier == Modifier.Control)
		{
			if (Input.GetKey(KeyCode.LeftControl) ||
				Input.GetKey(KeyCode.RightControl)) return true;
		}
		else if (modifier == Modifier.Shift)
		{
			if (Input.GetKey(KeyCode.LeftShift) ||
				Input.GetKey(KeyCode.RightShift)) return true;
		}
		return false;
	}

	/// <summary>
	/// Process the key binding.
	/// </summary>

	void Update ()
	{
		if (keyCode == KeyCode.None || !IsModifierActive()) return;

		if (action == Action.PressAndClick)
		{
			if (UICamera.inputHasFocus) return;

			UICamera.currentTouch = UICamera.controller;
			UICamera.currentScheme = UICamera.ControlScheme.Mouse;
			UICamera.currentTouch.current = gameObject;

			if (Input.GetKeyDown(keyCode))
			{
				UICamera.Notify(gameObject, "OnPress", true);
			}

			if (Input.GetKeyUp(keyCode))
			{
				UICamera.Notify(gameObject, "OnPress", false);
				UICamera.Notify(gameObject, "OnClick", null);
			}
			UICamera.currentTouch.current = null;
		}
		else if (action == Action.Select)
		{
			if (Input.GetKeyUp(keyCode))
			{
				if (mIsInput)
				{
					if (!mIgnoreUp && !UICamera.inputHasFocus)
					{
						UICamera.selectedObject = gameObject;
					}
					mIgnoreUp = false;
				}
				else
				{
					UICamera.selectedObject = gameObject;
				}
			}
		}
	}
}

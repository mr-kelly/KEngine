//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;

/// <summary>
/// Attach this script to a popup list, the parent of a group of toggles, or to a toggle itself to save its state.
/// </summary>

[AddComponentMenu("NGUI/Interaction/Saved Option")]
public class UISavedOption : MonoBehaviour
{
	/// <summary>
	/// PlayerPrefs-stored key for this option.
	/// </summary>

	public string keyName;

	string key { get { return (string.IsNullOrEmpty(keyName)) ? "NGUI State: " + name : keyName; } }

	UIPopupList mList;
	UIToggle mCheck;

	/// <summary>
	/// Cache the components and register a listener callback.
	/// </summary>

	void Awake ()
	{
		mList = GetComponent<UIPopupList>();
		mCheck = GetComponent<UIToggle>();	
	}

	/// <summary>
	/// Load and set the state of the toggles.
	/// </summary>

	void OnEnable ()
	{
		if (mList != null) EventDelegate.Add(mList.onChange, SaveSelection);
		if (mCheck != null) EventDelegate.Add(mCheck.onChange, SaveState);

		if (mList != null)
		{
			string s = PlayerPrefs.GetString(key);
			if (!string.IsNullOrEmpty(s)) mList.value = s;
			return;
		}

		if (mCheck != null)
		{
			mCheck.value = (PlayerPrefs.GetInt(key, 1) != 0);
		}
		else
		{
			string s = PlayerPrefs.GetString(key);
			UIToggle[] toggles = GetComponentsInChildren<UIToggle>(true);

			for (int i = 0, imax = toggles.Length; i < imax; ++i)
			{
				UIToggle ch = toggles[i];
				ch.value = (ch.name == s);
			}
		}
	}

	/// <summary>
	/// Save the state on destroy.
	/// </summary>

	void OnDisable ()
	{
		if (mCheck != null) EventDelegate.Remove(mCheck.onChange, SaveState);
		if (mList != null) EventDelegate.Remove(mList.onChange, SaveSelection);

		if (mCheck == null && mList == null)
		{
			UIToggle[] toggles = GetComponentsInChildren<UIToggle>(true);

			for (int i = 0, imax = toggles.Length; i < imax; ++i)
			{
				UIToggle ch = toggles[i];

				if (ch.value)
				{
					PlayerPrefs.SetString(key, ch.name);
					break;
				}
			}
		}
	}

	/// <summary>
	/// Save the selection.
	/// </summary>

	public void SaveSelection () { PlayerPrefs.SetString(key, UIPopupList.current.value); }

	/// <summary>
	/// Save the state.
	/// </summary>

	public void SaveState () { PlayerPrefs.SetInt(key, UIToggle.current.value ? 1 : 0); }
}

//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;

/// <summary>
/// Very simple script that can be attached to a slider and will control the volume of all sounds played via NGUITools.PlaySound,
/// which includes all of UI's sounds.
/// </summary>

[RequireComponent(typeof(UISlider))]
[AddComponentMenu("NGUI/Interaction/Sound Volume")]
public class UISoundVolume : MonoBehaviour
{
	UISlider mSlider;

	void Awake ()
	{
		mSlider = GetComponent<UISlider>();
		mSlider.value = NGUITools.soundVolume;
		EventDelegate.Add(mSlider.onChange, OnChange);
	}

	void OnChange ()
	{
		NGUITools.soundVolume = UISlider.current.value;
	}
}

//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;

/// <summary>
/// Makes it possible to animate alpha of the widget or a panel.
/// </summary>

[ExecuteInEditMode]
public class AnimatedAlpha : MonoBehaviour
{
#if !UNITY_3_5
	[Range(0f, 1f)]
#endif
	public float alpha = 1f;

	UIWidget mWidget;
	UIPanel mPanel;

	void OnEnable ()
	{
		mWidget = GetComponent<UIWidget>();
		mPanel = GetComponent<UIPanel>();
		LateUpdate();
	}

	void LateUpdate ()
	{
		if (mWidget != null) mWidget.alpha = alpha;
		if (mPanel != null) mPanel.alpha = alpha;
	}
}

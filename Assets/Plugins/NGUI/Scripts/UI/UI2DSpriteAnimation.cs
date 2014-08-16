//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

#if !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
using UnityEngine;

/// <summary>
/// Small script that makes it easy to create looping 2D sprite animations.
/// </summary>

public class UI2DSpriteAnimation : MonoBehaviour
{
	public int framerate = 20;
	public bool ignoreTimeScale = true;
	public UnityEngine.Sprite[] frames;

	UnityEngine.SpriteRenderer mUnitySprite;
	UI2DSprite mNguiSprite;
	int mIndex = 0;
	float mUpdate = 0f;

	void Start ()
	{
		mUnitySprite = GetComponent<UnityEngine.SpriteRenderer>();
		mNguiSprite = GetComponent<UI2DSprite>();
		if (framerate > 0) mUpdate = (ignoreTimeScale ? RealTime.time : Time.time) + 1f / framerate;
	}

	void Update ()
	{
		if (framerate != 0 && frames != null && frames.Length > 0)
		{
			float time = ignoreTimeScale ? RealTime.time : Time.time;

			if (mUpdate < time)
			{
				mUpdate = time;
				mIndex = NGUIMath.RepeatIndex(framerate > 0 ? mIndex + 1 : mIndex - 1, frames.Length);
				mUpdate = time + Mathf.Abs(1f / framerate);

				if (mUnitySprite != null)
				{
					mUnitySprite.sprite = frames[mIndex];
				}
				else if (mNguiSprite != null)
				{
					mNguiSprite.nextSprite = frames[mIndex];
				}
			}
		}
	}
}
#endif

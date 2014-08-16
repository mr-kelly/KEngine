//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Popup list can be used to display pop-up menus and drop-down lists.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/Interaction/Popup List")]
public class UIPopupList : UIWidgetContainer
{
	/// <summary>
	/// Current popup list. Only available during the OnSelectionChange event callback.
	/// </summary>

	static public UIPopupList current;

	const float animSpeed = 0.15f;

	public enum Position
	{
		Auto,
		Above,
		Below,
	}

	/// <summary>
	/// Atlas used by the sprites.
	/// </summary>

	public UIAtlas atlas;

	/// <summary>
	/// Font used by the labels.
	/// </summary>

	public UIFont bitmapFont;

	/// <summary>
	/// True type font used by the labels. Alternative to specifying a bitmap font ('font').
	/// </summary>

	public Font trueTypeFont;

	/// <summary>
	/// Font used by the popup list. Conveniently wraps both dynamic and bitmap fonts into one property.
	/// </summary>

	public Object ambigiousFont
	{
		get
		{
			if (trueTypeFont != null) return trueTypeFont;
			if (bitmapFont != null) return bitmapFont;
			return font;
		}
		set
		{
			if (value is Font)
			{
				trueTypeFont = value as Font;
				bitmapFont = null;
				font = null;
			}
			else if (value is UIFont)
			{
				bitmapFont = value as UIFont;
				trueTypeFont = null;
				font = null;
			}
		}
	}

	/// <summary>
	/// Size of the font to use for the popup list's labels.
	/// </summary>

	public int fontSize = 16;

	/// <summary>
	/// Font style used by the dynamic font.
	/// </summary>

	public FontStyle fontStyle = FontStyle.Normal;

	/// <summary>
	/// Name of the sprite used to create the popup's background.
	/// </summary>

	public string backgroundSprite;

	/// <summary>
	/// Name of the sprite used to highlight items.
	/// </summary>

	public string highlightSprite;

	/// <summary>
	/// Popup list's display style.
	/// </summary>

	public Position position = Position.Auto;

	/// <summary>
	/// New line-delimited list of items.
	/// </summary>

	public List<string> items = new List<string>();

	/// <summary>
	/// Amount of padding added to labels.
	/// </summary>

	public Vector2 padding = new Vector3(4f, 4f);

	/// <summary>
	/// Color tint applied to labels inside the list.
	/// </summary>

	public Color textColor = Color.white;

	/// <summary>
	/// Color tint applied to the background.
	/// </summary>

	public Color backgroundColor = Color.white;

	/// <summary>
	/// Color tint applied to the highlighter.
	/// </summary>

	public Color highlightColor = new Color(225f / 255f, 200f / 255f, 150f / 255f, 1f);

	/// <summary>
	/// Whether the popup list is animated or not. Disable for better performance.
	/// </summary>

	public bool isAnimated = true;

	/// <summary>
	/// Whether the popup list's values will be localized.
	/// </summary>

	public bool isLocalized = false;

	/// <summary>
	/// Callbacks triggered when the popup list gets a new item selection.
	/// </summary>

	public List<EventDelegate> onChange = new List<EventDelegate>();

	// Currently selected item
	[HideInInspector][SerializeField] string mSelectedItem;

	UIPanel mPanel;
	GameObject mChild;
	UISprite mBackground;
	UISprite mHighlight;
	UILabel mHighlightedLabel = null;
	List<UILabel> mLabelList = new List<UILabel>();
	float mBgBorder = 0f;

	// Deprecated functionality
	[HideInInspector][SerializeField] GameObject eventReceiver;
	[HideInInspector][SerializeField] string functionName = "OnSelectionChange";
	[HideInInspector][SerializeField] float textScale = 0f;
	[HideInInspector][SerializeField] UIFont font; // Use 'bitmapFont' instead

	// This functionality is no longer needed as the same can be achieved by choosing a
	// OnValueChange notification targeting a label's SetCurrentSelection function.
	// If your code was list.textLabel = myLabel, change it to:
	// EventDelegate.Add(list.onChange, lbl.SetCurrentSelection);
	[HideInInspector][SerializeField] UILabel textLabel;

	public delegate void LegacyEvent (string val);
	LegacyEvent mLegacyEvent;

	[System.Obsolete("Use EventDelegate.Add(popup.onChange, YourCallback) instead, and UIPopupList.current.value to determine the state")]
	public LegacyEvent onSelectionChange { get { return mLegacyEvent; } set { mLegacyEvent = value; } }

	/// <summary>
	/// Whether the popup list is currently open.
	/// </summary>

	public bool isOpen { get { return mChild != null; } }

	/// <summary>
	/// Current selection.
	/// </summary>

	public string value
	{
		get
		{
			return mSelectedItem;
		}
		set
		{
			mSelectedItem = value;
			if (mSelectedItem == null) return;
#if UNITY_EDITOR
			if (!Application.isPlaying) return;
#endif
			if (mSelectedItem != null)
				TriggerCallbacks();
		}
	}

	[System.Obsolete("Use 'value' instead")]
	public string selection { get { return value; } set { this.value = value; } }

	/// <summary>
	/// Whether the popup list will be handling keyboard, joystick and controller events.
	/// </summary>

	bool handleEvents
	{
		get
		{
			UIKeyNavigation keys = GetComponent<UIKeyNavigation>();
			return (keys == null || !keys.enabled);
		}
		set
		{
			UIKeyNavigation keys = GetComponent<UIKeyNavigation>();
			if (keys != null) keys.enabled = !value;
		}
	}

	/// <summary>
	/// Whether the popup list is actually usable.
	/// </summary>

	bool isValid { get { return bitmapFont != null || trueTypeFont != null; } }

	/// <summary>
	/// Active font size.
	/// </summary>

	int activeFontSize { get { return (trueTypeFont != null || bitmapFont == null) ? fontSize : bitmapFont.defaultSize; } }

	/// <summary>
	/// Font scale applied to the popup list's text.
	/// </summary>

	float activeFontScale { get { return (trueTypeFont != null || bitmapFont == null) ? 1f : (float)fontSize / bitmapFont.defaultSize; } }

	/// <summary>
	/// Trigger all event notification callbacks.
	/// </summary>

	protected void TriggerCallbacks ()
	{
		if (current != this)
		{
			UIPopupList old = current;
			current = this;

			// Legacy functionality
			if (mLegacyEvent != null) mLegacyEvent(mSelectedItem);

			if (EventDelegate.IsValid(onChange))
			{
				EventDelegate.Execute(onChange);
			}
			else if (eventReceiver != null && !string.IsNullOrEmpty(functionName))
			{
				// Legacy functionality support (for backwards compatibility)
				eventReceiver.SendMessage(functionName, mSelectedItem, SendMessageOptions.DontRequireReceiver);
			}
			current = old;
		}
	}

	/// <summary>
	/// Remove legacy functionality.
	/// </summary>

	void OnEnable ()
	{
		if (EventDelegate.IsValid(onChange))
		{
			eventReceiver = null;
			functionName = null;
		}

		// 'font' is no longer used
		if (font != null)
		{
			if (font.isDynamic)
			{
				trueTypeFont = font.dynamicFont;
				fontStyle = font.dynamicFontStyle;
				mUseDynamicFont = true;
			}
			else if (bitmapFont == null)
			{
				bitmapFont = font;
				mUseDynamicFont = false;
			}
			font = null;
		}

		// 'textScale' is no longer used
		if (textScale != 0f)
		{
			fontSize = (bitmapFont != null) ? Mathf.RoundToInt(bitmapFont.defaultSize * textScale) : 16;
			textScale = 0f;
		}

		// Auto-upgrade to the true type font
		if (trueTypeFont == null && bitmapFont != null && bitmapFont.isDynamic)
		{
			trueTypeFont = bitmapFont.dynamicFont;
			bitmapFont = null;
		}
	}

	bool mUseDynamicFont = false;

	void OnValidate ()
	{
		Font ttf = trueTypeFont;
		UIFont fnt = bitmapFont;

		bitmapFont = null;
		trueTypeFont = null;

		if (ttf != null && (fnt == null || !mUseDynamicFont))
		{
			bitmapFont = null;
			trueTypeFont = ttf;
			mUseDynamicFont = true;
		}
		else if (fnt != null)
		{
			// Auto-upgrade from 3.0.2 and earlier
			if (fnt.isDynamic)
			{
				trueTypeFont = fnt.dynamicFont;
				fontStyle = fnt.dynamicFontStyle;
				fontSize = fnt.defaultSize;
				mUseDynamicFont = true;
			}
			else
			{
				bitmapFont = fnt;
				mUseDynamicFont = false;
			}
		}
		else
		{
			trueTypeFont = ttf;
			mUseDynamicFont = true;
		}
	}

	/// <summary>
	/// Send out the selection message on start.
	/// </summary>

	void Start ()
	{
		// Auto-upgrade legacy functionality
		if (textLabel != null)
		{
			EventDelegate.Add(onChange, textLabel.SetCurrentSelection);
			textLabel = null;
#if UNITY_EDITOR
			NGUITools.SetDirty(this);
#endif
		}

		if (Application.isPlaying)
		{
			// Automatically choose the first item
			if (string.IsNullOrEmpty(mSelectedItem))
			{
				if (items.Count > 0) value = items[0];
			}
			else
			{
				string s = mSelectedItem;
				mSelectedItem = null;
				value = s;
			}
		}
	}

	/// <summary>
	/// Localize the text label.
	/// </summary>

	void OnLocalize () { if (isLocalized) TriggerCallbacks(); }

	/// <summary>
	/// Visibly highlight the specified transform by moving the highlight sprite to be over it.
	/// </summary>

	void Highlight (UILabel lbl, bool instant)
	{
		if (mHighlight != null)
		{
			mHighlightedLabel = lbl;

			UISpriteData sp = mHighlight.GetAtlasSprite();
			if (sp == null) return;

			Vector3 pos = GetHighlightPosition();

			if (instant || !isAnimated)
			{
				mHighlight.cachedTransform.localPosition = pos;
			}
			else
			{
				TweenPosition.Begin(mHighlight.gameObject, 0.1f, pos).method = UITweener.Method.EaseOut;

				if (!mTweening)
				{
					mTweening = true;
					StartCoroutine(UpdateTweenPosition());
				}
			}
		}
	}

	/// <summary>
	/// Helper function that calculates where the tweened position should be.
	/// </summary>

	Vector3 GetHighlightPosition ()
	{
		if (mHighlightedLabel == null) return Vector3.zero;
		UISpriteData sp = mHighlight.GetAtlasSprite();
		if (sp == null) return Vector3.zero;

		float scaleFactor = atlas.pixelSize;
		float offsetX = sp.borderLeft * scaleFactor;
		float offsetY = sp.borderTop * scaleFactor;

		return mHighlightedLabel.cachedTransform.localPosition + new Vector3(-offsetX, offsetY, 1f);
	}

	bool mTweening = false;

	/// <summary>
	/// Periodically update the tweened target position.
	/// It's needed because the popup list animates into view, and the target position changes.
	/// </summary>

	IEnumerator UpdateTweenPosition ()
	{
		if (mHighlight != null && mHighlightedLabel != null)
		{
			TweenPosition tp = mHighlight.GetComponent<TweenPosition>();
			
			while (tp != null && tp.enabled)
			{
				tp.to = GetHighlightPosition();
				yield return null;
			}
		}
		mTweening = false;
	}

	/// <summary>
	/// Event function triggered when the mouse hovers over an item.
	/// </summary>

	void OnItemHover (GameObject go, bool isOver)
	{
		if (isOver)
		{
			UILabel lbl = go.GetComponent<UILabel>();
			Highlight(lbl, false);
		}
	}

	/// <summary>
	/// Select the specified label.
	/// </summary>

	void Select (UILabel lbl, bool instant)
	{
		Highlight(lbl, instant);
		
		UIEventListener listener = lbl.gameObject.GetComponent<UIEventListener>();
		value = listener.parameter as string;

		UIPlaySound[] sounds = GetComponents<UIPlaySound>();

		for (int i = 0, imax = sounds.Length; i < imax; ++i)
		{
			UIPlaySound snd = sounds[i];

			if (snd.trigger == UIPlaySound.Trigger.OnClick)
			{
				NGUITools.PlaySound(snd.audioClip, snd.volume, 1f);
			}
		}
	}

	/// <summary>
	/// Event function triggered when the drop-down list item gets clicked on.
	/// </summary>

	void OnItemPress (GameObject go, bool isPressed) { if (isPressed) Select(go.GetComponent<UILabel>(), true); }

	/// <summary>
	/// Close the popup list on click.
	/// </summary>

	void OnItemClick (GameObject go) { Close(); }

	/// <summary>
	/// React to key-based input.
	/// </summary>

	void OnKey (KeyCode key)
	{
		if (enabled && NGUITools.GetActive(gameObject) && handleEvents)
		{
			int index = mLabelList.IndexOf(mHighlightedLabel);
			if (index == -1) index = 0;

			if (key == KeyCode.UpArrow)
			{
				if (index > 0)
				{
					Select(mLabelList[--index], false);
				}
			}
			else if (key == KeyCode.DownArrow)
			{
				if (index + 1 < mLabelList.Count)
				{
					Select(mLabelList[++index], false);
				}
			}
			else if (key == KeyCode.Escape)
			{
				OnSelect(false);
			}
		}
	}

	/// <summary>
	/// Get rid of the popup dialog when the selection gets lost.
	/// </summary>

	void OnSelect (bool isSelected) { if (!isSelected) Close(); }

	/// <summary>
	/// Manually close the popup list.
	/// </summary>

	public void Close ()
	{
		if (mChild != null)
		{
			mLabelList.Clear();
			handleEvents = false;

			if (isAnimated)
			{
				UIWidget[] widgets = mChild.GetComponentsInChildren<UIWidget>();

				for (int i = 0, imax = widgets.Length; i < imax; ++i)
				{
					UIWidget w = widgets[i];
					Color c = w.color;
					c.a = 0f;
					TweenColor.Begin(w.gameObject, animSpeed, c).method = UITweener.Method.EaseOut;
				}

				Collider[] cols = mChild.GetComponentsInChildren<Collider>();
				for (int i = 0, imax = cols.Length; i < imax; ++i) cols[i].enabled = false;
				Destroy(mChild, animSpeed);
			}
			else Destroy(mChild);

			mBackground = null;
			mHighlight = null;
			mChild = null;
		}
	}

	/// <summary>
	/// Helper function that causes the widget to smoothly fade in.
	/// </summary>

	void AnimateColor (UIWidget widget)
	{
		Color c = widget.color;
		widget.color = new Color(c.r, c.g, c.b, 0f);
		TweenColor.Begin(widget.gameObject, animSpeed, c).method = UITweener.Method.EaseOut;
	}

	/// <summary>
	/// Helper function that causes the widget to smoothly move into position.
	/// </summary>

	void AnimatePosition (UIWidget widget, bool placeAbove, float bottom)
	{
		Vector3 target = widget.cachedTransform.localPosition;
		Vector3 start = placeAbove ? new Vector3(target.x, bottom, target.z) : new Vector3(target.x, 0f, target.z);

		widget.cachedTransform.localPosition = start;

		GameObject go = widget.gameObject;
		TweenPosition.Begin(go, animSpeed, target).method = UITweener.Method.EaseOut;
	}

	/// <summary>
	/// Helper function that causes the widget to smoothly grow until it reaches its original size.
	/// </summary>

	void AnimateScale (UIWidget widget, bool placeAbove, float bottom)
	{
		GameObject go = widget.gameObject;
		Transform t = widget.cachedTransform;

		float minHeight = activeFontSize * activeFontScale + mBgBorder * 2f;
		t.localScale = new Vector3(1f, minHeight / widget.height, 1f);
		TweenScale.Begin(go, animSpeed, Vector3.one).method = UITweener.Method.EaseOut;

		if (placeAbove)
		{
			Vector3 pos = t.localPosition;
			t.localPosition = new Vector3(pos.x, pos.y - widget.height + minHeight, pos.z);
			TweenPosition.Begin(go, animSpeed, pos).method = UITweener.Method.EaseOut;
		}
	}

	/// <summary>
	/// Helper function used to animate widgets.
	/// </summary>

	void Animate (UIWidget widget, bool placeAbove, float bottom)
	{
		AnimateColor(widget);
		AnimatePosition(widget, placeAbove, bottom);
	}

	/// <summary>
	/// Display the drop-down list when the game object gets clicked on.
	/// </summary>

	void OnClick()
	{
		if (enabled && NGUITools.GetActive(gameObject) && mChild == null && atlas != null && isValid && items.Count > 0)
		{
			mLabelList.Clear();

			// Automatically locate the panel responsible for this object
			if (mPanel == null)
			{
				mPanel = UIPanel.Find(transform);
				if (mPanel == null) return;
			}

			// Disable the navigation script
			handleEvents = true;

			// Calculate the dimensions of the object triggering the popup list so we can position it below it
			Transform myTrans = transform;
			Bounds bounds = NGUIMath.CalculateRelativeWidgetBounds(myTrans.parent, myTrans);

			// Create the root object for the list
			mChild = new GameObject("Drop-down List");
			mChild.layer = gameObject.layer;

			Transform t = mChild.transform;
			t.parent = myTrans.parent;
			t.localPosition = bounds.min;
			t.localRotation = Quaternion.identity;
			t.localScale = Vector3.one;

			// Add a sprite for the background
			mBackground = NGUITools.AddSprite(mChild, atlas, backgroundSprite);
			mBackground.pivot = UIWidget.Pivot.TopLeft;
			mBackground.depth = NGUITools.CalculateNextDepth(mPanel.gameObject);
			mBackground.color = backgroundColor;

			// We need to know the size of the background sprite for padding purposes
			Vector4 bgPadding = mBackground.border;
			mBgBorder = bgPadding.y;
			mBackground.cachedTransform.localPosition = new Vector3(0f, bgPadding.y, 0f);

			// Add a sprite used for the selection
			mHighlight = NGUITools.AddSprite(mChild, atlas, highlightSprite);
			mHighlight.pivot = UIWidget.Pivot.TopLeft;
			mHighlight.color = highlightColor;

			UISpriteData hlsp = mHighlight.GetAtlasSprite();
			if (hlsp == null) return;

			float hlspHeight = hlsp.borderTop;
			float fontHeight = activeFontSize;
			float dynScale = activeFontScale;
			float labelHeight = fontHeight * dynScale;
			float x = 0f, y = -padding.y;
			int labelFontSize = (bitmapFont != null) ? bitmapFont.defaultSize : fontSize;
			List<UILabel> labels = new List<UILabel>();

			// Run through all items and create labels for each one
			for (int i = 0, imax = items.Count; i < imax; ++i)
			{
				string s = items[i];

				UILabel lbl = NGUITools.AddWidget<UILabel>(mChild);
				lbl.name = i.ToString();
				lbl.pivot = UIWidget.Pivot.TopLeft;
				lbl.bitmapFont = bitmapFont;
				lbl.trueTypeFont = trueTypeFont;
				lbl.fontSize = labelFontSize;
				lbl.fontStyle = fontStyle;
				lbl.text = isLocalized ? Localization.Get(s) : s;
				lbl.color = textColor;
				lbl.cachedTransform.localPosition = new Vector3(bgPadding.x + padding.x, y, -1f);
				lbl.overflowMethod = UILabel.Overflow.ResizeFreely;
				lbl.MakePixelPerfect();
				if (dynScale != 1f) lbl.cachedTransform.localScale = Vector3.one * dynScale;
				labels.Add(lbl);

				y -= labelHeight;
				y -= padding.y;
				x = Mathf.Max(x, lbl.printedSize.x);

				// Add an event listener
				UIEventListener listener = UIEventListener.Get(lbl.gameObject);
				listener.onHover = OnItemHover;
				listener.onPress = OnItemPress;
				listener.onClick = OnItemClick;
				listener.parameter = s;

				// Move the selection here if this is the right label
				if (mSelectedItem == s || (i == 0 && string.IsNullOrEmpty(mSelectedItem)))
					Highlight(lbl, true);

				// Add this label to the list
				mLabelList.Add(lbl);
			}

			// The triggering widget's width should be the minimum allowed width
			x = Mathf.Max(x, bounds.size.x * dynScale - (bgPadding.x + padding.x) * 2f);

			float cx = x / dynScale;
			Vector3 bcCenter = new Vector3(cx * 0.5f, -fontHeight * 0.5f, 0f);
			Vector3 bcSize = new Vector3(cx, (labelHeight + padding.y) / dynScale, 1f);

			// Run through all labels and add colliders
			for (int i = 0, imax = labels.Count; i < imax; ++i)
			{
				UILabel lbl = labels[i];
				NGUITools.AddWidgetCollider(lbl.gameObject);
				BoxCollider bc = lbl.GetComponent<BoxCollider>();

				if (bc != null)
				{
					bcCenter.z = bc.center.z;
					bc.center = bcCenter;
					bc.size = bcSize;
				}
#if !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
				else
				{
					BoxCollider2D b2d = lbl.GetComponent<BoxCollider2D>();
					b2d.center = bcCenter;
					b2d.size = bcSize;
				}
#endif
			}

			x += (bgPadding.x + padding.x) * 2f;
			y -= bgPadding.y;

			// Scale the background sprite to envelop the entire set of items
			mBackground.width = Mathf.RoundToInt(x);
			mBackground.height = Mathf.RoundToInt(-y + bgPadding.y);

			// Scale the highlight sprite to envelop a single item
			float scaleFactor = 2f * atlas.pixelSize;
			float w = x - (bgPadding.x + padding.x) * 2f + hlsp.borderLeft * scaleFactor;
			float h = labelHeight + hlspHeight * scaleFactor;
			mHighlight.width = Mathf.RoundToInt(w);
			mHighlight.height = Mathf.RoundToInt(h);

			bool placeAbove = (position == Position.Above);

			if (position == Position.Auto)
			{
				UICamera cam = UICamera.FindCameraForLayer(gameObject.layer);

				if (cam != null)
				{
					Vector3 viewPos = cam.cachedCamera.WorldToViewportPoint(myTrans.position);
					placeAbove = (viewPos.y < 0.5f);
				}
			}

			// If the list should be animated, let's animate it by expanding it
			if (isAnimated)
			{
				float bottom = y + labelHeight;
				Animate(mHighlight, placeAbove, bottom);
				for (int i = 0, imax = labels.Count; i < imax; ++i) Animate(labels[i], placeAbove, bottom);
				AnimateColor(mBackground);
				AnimateScale(mBackground, placeAbove, bottom);
			}

			// If we need to place the popup list above the item, we need to reposition everything by the size of the list
			if (placeAbove)
			{
				t.localPosition = new Vector3(bounds.min.x, bounds.max.y - y - bgPadding.y, bounds.min.z);
			}
		}
		else OnSelect(false);
	}
}

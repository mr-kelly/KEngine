//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This script should be attached to each camera that's used to draw the objects with
/// UI components on them. This may mean only one camera (main camera or your UI camera),
/// or multiple cameras if you happen to have multiple viewports. Failing to attach this
/// script simply means that objects drawn by this camera won't receive UI notifications:
/// 
/// * OnHover (isOver) is sent when the mouse hovers over a collider or moves away.
/// * OnPress (isDown) is sent when a mouse button gets pressed on the collider.
/// * OnSelect (selected) is sent when a mouse button is released on the same object as it was pressed on.
/// * OnClick () is sent with the same conditions as OnSelect, with the added check to see if the mouse has not moved much.
///   UICamera.currentTouchID tells you which button was clicked.
/// * OnDoubleClick () is sent when the click happens twice within a fourth of a second.
///   UICamera.currentTouchID tells you which button was clicked.
/// 
/// * OnDragStart () is sent to a game object under the touch just before the OnDrag() notifications begin.
/// * OnDrag (delta) is sent to an object that's being dragged.
/// * OnDragOver (draggedObject) is sent to a game object when another object is dragged over its area.
/// * OnDragOut (draggedObject) is sent to a game object when another object is dragged out of its area.
/// * OnDragEnd () is sent to a dragged object when the drag event finishes.
/// 
/// * OnTooltip (show) is sent when the mouse hovers over a collider for some time without moving.
/// * OnScroll (float delta) is sent out when the mouse scroll wheel is moved.
/// * OnKey (KeyCode key) is sent when keyboard or controller input is used.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/UI/NGUI Event System (UICamera)")]
[RequireComponent(typeof(Camera))]
public class UICamera : MonoBehaviour
{
	public enum ControlScheme
	{
		Mouse,
		Touch,
		Controller,
	}

	/// <summary>
	/// Whether the touch event will be sending out the OnClick notification at the end.
	/// </summary>

	public enum ClickNotification
	{
		None,
		Always,
		BasedOnDelta,
	}

	/// <summary>
	/// Ambiguous mouse, touch, or controller event.
	/// </summary>

	public class MouseOrTouch
	{
		public Vector2 pos;				// Current position of the mouse or touch event
		public Vector2 lastPos;			// Previous position of the mouse or touch event
		public Vector2 delta;			// Delta since last update
		public Vector2 totalDelta;		// Delta since the event started being tracked

		public Camera pressedCam;		// Camera that the OnPress(true) was fired with

		public GameObject last;			// Last object under the touch or mouse
		public GameObject current;		// Current game object under the touch or mouse
		public GameObject pressed;		// Last game object to receive OnPress
		public GameObject dragged;		// Game object that's being dragged

		public float clickTime = 0f;	// The last time a click event was sent out

		public ClickNotification clickNotification = ClickNotification.Always;
		public bool touchBegan = true;
		public bool pressStarted = false;
		public bool dragStarted = false;
	}

	/// <summary>
	/// Camera type controls how raycasts are handled by the UICamera.
	/// </summary>

	public enum EventType : int
	{
		World_3D,	// Perform a Physics.Raycast and sort by distance to the point that was hit.
		UI_3D,		// Perform a Physics.Raycast and sort by widget depth.
#if !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
		World_2D,	// Perform a Physics2D.OverlapPoint
		UI_2D,		// Physics2D.OverlapPoint then sort by widget depth
#endif
	}

	/// <summary>
	/// List of all active cameras in the scene.
	/// </summary>

	static public BetterList<UICamera> list = new BetterList<UICamera>();

	public delegate void OnScreenResize ();

	/// <summary>
	/// Delegate triggered when the screen size changes for any reason.
	/// Subscribe to it if you don't want to compare Screen.width and Screen.height each frame.
	/// </summary>

	static public OnScreenResize onScreenResize;

	/// <summary>
	/// Event type -- use "UI" for your user interfaces, and "World" for your game camera.
	/// This setting changes how raycasts are handled. Raycasts have to be more complicated for UI cameras.
	/// </summary>

	public EventType eventType = EventType.UI_3D;

	/// <summary>
	/// Which layers will receive events.
	/// </summary>

	public LayerMask eventReceiverMask = -1;

	/// <summary>
	/// If 'true', currently hovered object will be shown in the top left corner.
	/// </summary>

	public bool debug = false;

	/// <summary>
	/// Whether the mouse input is used.
	/// </summary>

	public bool useMouse = true;

	/// <summary>
	/// Whether the touch-based input is used.
	/// </summary>

	public bool useTouch = true;

	/// <summary>
	/// Whether multi-touch is allowed.
	/// </summary>

	public bool allowMultiTouch = true;

	/// <summary>
	/// Whether the keyboard events will be processed.
	/// </summary>

	public bool useKeyboard = true;

	/// <summary>
	/// Whether the joystick and controller events will be processed.
	/// </summary>

	public bool useController = true;

	[System.Obsolete("Use new OnDragStart / OnDragOver / OnDragOut / OnDragEnd events instead")]
	public bool stickyPress { get { return true; } }

	/// <summary>
	/// Whether the tooltip will disappear as soon as the mouse moves (false) or only if the mouse moves outside of the widget's area (true).
	/// </summary>

	public bool stickyTooltip = true;

	/// <summary>
	/// How long of a delay to expect before showing the tooltip.
	/// </summary>

	public float tooltipDelay = 1f;

	/// <summary>
	/// How much the mouse has to be moved after pressing a button before it starts to send out drag events.
	/// </summary>

	public float mouseDragThreshold = 4f;

	/// <summary>
	/// How far the mouse is allowed to move in pixels before it's no longer considered for click events, if the click notification is based on delta.
	/// </summary>

	public float mouseClickThreshold = 10f;

	/// <summary>
	/// How much the mouse has to be moved after pressing a button before it starts to send out drag events.
	/// </summary>

	public float touchDragThreshold = 40f;

	/// <summary>
	/// How far the touch is allowed to move in pixels before it's no longer considered for click events, if the click notification is based on delta.
	/// </summary>

	public float touchClickThreshold = 40f;

	/// <summary>
	/// Raycast range distance. By default it's as far as the camera can see.
	/// </summary>

	public float rangeDistance = -1f;

	/// <summary>
	/// Name of the axis used for scrolling.
	/// </summary>

	public string scrollAxisName = "Mouse ScrollWheel";

	/// <summary>
	/// Name of the axis used to send up and down key events.
	/// </summary>

	public string verticalAxisName = "Vertical";

	/// <summary>
	/// Name of the axis used to send left and right key events.
	/// </summary>

	public string horizontalAxisName = "Horizontal";

	/// <summary>
	/// Various keys used by the camera.
	/// </summary>

	public KeyCode submitKey0 = KeyCode.Return;
	public KeyCode submitKey1 = KeyCode.JoystickButton0;
	public KeyCode cancelKey0 = KeyCode.Escape;
	public KeyCode cancelKey1 = KeyCode.JoystickButton1;

	public delegate void OnCustomInput ();

	/// <summary>
	/// Custom input processing logic, if desired. For example: WP7 touches.
	/// Use UICamera.current to get the current camera.
	/// </summary>

	static public OnCustomInput onCustomInput;

	/// <summary>
	/// Whether tooltips will be shown or not.
	/// </summary>

	static public bool showTooltips = true;

	/// <summary>
	/// Position of the last touch (or mouse) event.
	/// </summary>

	static public Vector2 lastTouchPosition = Vector2.zero;

	/// <summary>
	/// Position of the last touch (or mouse) event in the world.
	/// </summary>

	static public Vector3 lastWorldPosition = Vector3.zero;

	/// <summary>
	/// Last raycast hit prior to sending out the event. This is useful if you want detailed information
	/// about what was actually hit in your OnClick, OnHover, and other event functions.
	/// Note that this is not going to be valid if you're using 2D colliders.
	/// </summary>

	static public RaycastHit lastHit;

	/// <summary>
	/// UICamera that sent out the event.
	/// </summary>

	static public UICamera current = null;

	/// <summary>
	/// Last camera active prior to sending out the event. This will always be the camera that actually sent out the event.
	/// </summary>

	static public Camera currentCamera = null;

	/// <summary>
	/// Current control scheme. Set automatically when events arrive.
	/// </summary>

	static public ControlScheme currentScheme = ControlScheme.Mouse;

	/// <summary>
	/// ID of the touch or mouse operation prior to sending out the event. Mouse ID is '-1' for left, '-2' for right mouse button, '-3' for middle.
	/// </summary>

	static public int currentTouchID = -1;

	/// <summary>
	/// Key that triggered the event, if any.
	/// </summary>

	static public KeyCode currentKey = KeyCode.None;

	/// <summary>
	/// Ray projected into the screen underneath the current touch.
	/// </summary>

	static public Ray currentRay
	{
		get
		{
			return (currentCamera != null && currentTouch != null) ?
				currentCamera.ScreenPointToRay(currentTouch.pos) : new Ray();
		}
	}

	/// <summary>
	/// Current touch, set before any event function gets called.
	/// </summary>

	static public MouseOrTouch currentTouch = null;

	/// <summary>
	/// Whether an input field currently has focus.
	/// </summary>

	static public bool inputHasFocus = false;

	/// <summary>
	/// If set, this game object will receive all events regardless of whether they were handled or not.
	/// </summary>

	static public GameObject genericEventHandler;

	/// <summary>
	/// If events don't get handled, they will be forwarded to this game object.
	/// </summary>

	static public GameObject fallThrough;

	// Selected widget (for input)
	static GameObject mCurrentSelection = null;
	static GameObject mNextSelection = null;
	static ControlScheme mNextScheme = ControlScheme.Controller;

	// Mouse events
	static MouseOrTouch[] mMouse = new MouseOrTouch[] { new MouseOrTouch(), new MouseOrTouch(), new MouseOrTouch() };

	// The last object to receive OnHover
	static GameObject mHover;

	// Joystick/controller/keyboard event
	static public MouseOrTouch controller = new MouseOrTouch();

	// Used to ensure that joystick-based controls don't trigger that often
	static float mNextEvent = 0f;

	// List of currently active touches
	static Dictionary<int, MouseOrTouch> mTouches = new Dictionary<int, MouseOrTouch>();

	// Used to detect screen dimension changes
	static int mWidth = 0;
	static int mHeight = 0;

	// Tooltip widget (mouse only)
	GameObject mTooltip = null;

	// Mouse input is turned off on iOS
	Camera mCam = null;
	float mTooltipTime = 0f;
	float mNextRaycast = 0f;

	/// <summary>
	/// Helper function that determines if this script should be handling the events.
	/// </summary>

	bool handlesEvents { get { return eventHandler == this; } }

	/// <summary>
	/// Caching is always preferable for performance.
	/// </summary>

	public Camera cachedCamera { get { if (mCam == null) mCam = camera; return mCam; } }

	/// <summary>
	/// Set to 'true' just before OnDrag-related events are sent. No longer needed, but kept for backwards compatibility.
	/// </summary>

	static public bool isDragging = false;

	/// <summary>
	/// The object hit by the last Raycast that was the result of a mouse or touch event.
	/// </summary>

	static public GameObject hoveredObject;

	/// <summary>
	/// Option to manually set the selected game object.
	/// </summary>

	static public GameObject selectedObject
	{
		get
		{
			return mCurrentSelection;
		}
		set
		{
			SetSelection(value, UICamera.currentScheme);
		}
	}

	/// <summary>
	/// Returns 'true' if any of the active touch, mouse or controller is currently holding the specified object.
	/// </summary>

	static public bool IsPressed (GameObject go)
	{
		for (int i = 0; i < 3; ++i) if (mMouse[i].pressed == go) return true;
		foreach (KeyValuePair<int, MouseOrTouch> touch in mTouches) if (touch.Value.pressed == go) return true;
		if (controller.pressed == go) return true;
		return false;
	}

	/// <summary>
	/// Change the selection.
	/// </summary>

	static protected void SetSelection (GameObject go, ControlScheme scheme)
	{
		if (mNextSelection != null)
		{
			mNextSelection = go;
		}
		else if (mCurrentSelection != go)
		{
			mNextSelection = go;
			mNextScheme = scheme;

			if (UICamera.list.size > 0)
			{
				UICamera cam = (mNextSelection != null) ? FindCameraForLayer(mNextSelection.layer) : UICamera.list[0];
				if (cam != null) cam.StartCoroutine(cam.ChangeSelection());
			}
		}
	}

	/// <summary>
	/// Selection change is delayed on purpose. This way selection changes during event processing won't cause
	/// the newly selected widget to continue processing when it is it's turn. Example: pressing 'tab' on one
	/// button selects the next button, and then it also processes its 'tab' in turn, selecting the next one.
	/// </summary>

	System.Collections.IEnumerator ChangeSelection ()
	{
		yield return new WaitForEndOfFrame();
		Notify(mCurrentSelection, "OnSelect", false);
		mCurrentSelection = mNextSelection;
		mNextSelection = null;

		if (mCurrentSelection != null)
		{
			current = this;
			currentCamera = mCam;
			UICamera.currentScheme = mNextScheme;
			inputHasFocus = (mCurrentSelection.GetComponent<UIInput>() != null);
			Notify(mCurrentSelection, "OnSelect", true);
			current = null;
		}
		else inputHasFocus = false;
	}

	/// <summary>
	/// Number of active touches from all sources.
	/// </summary>

	static public int touchCount
	{
		get
		{
			int count = 0;

			foreach (KeyValuePair<int, MouseOrTouch> touch in mTouches)
				if (touch.Value.pressed != null)
					++count;

			for (int i = 0; i < mMouse.Length; ++i)
				if (mMouse[i].pressed != null)
					++count;

			if (controller.pressed != null)
				++count;

			return count;
		}
	}

	/// <summary>
	/// Number of active drag events from all sources.
	/// </summary>

	static public int dragCount
	{
		get
		{
			int count = 0;

			foreach (KeyValuePair<int, MouseOrTouch> touch in mTouches)
				if (touch.Value.dragged != null)
					++count;

			for (int i = 0; i < mMouse.Length; ++i)
				if (mMouse[i].dragged != null)
					++count;

			if (controller.dragged != null)
				++count;

			return count;
		}
	}

	/// <summary>
	/// Convenience function that returns the main HUD camera.
	/// </summary>

	static public Camera mainCamera
	{
		get
		{
			UICamera mouse = eventHandler;
			return (mouse != null) ? mouse.cachedCamera : null;
		}
	}

	/// <summary>
	/// Event handler for all types of events.
	/// </summary>

	static public UICamera eventHandler
	{
		get
		{
			for (int i = 0; i < list.size; ++i)
			{
				// Invalid or inactive entry -- keep going
				UICamera cam = list.buffer[i];
				if (cam == null || !cam.enabled || !NGUITools.GetActive(cam.gameObject)) continue;
				return cam;
			}
			return null;
		}
	}

	/// <summary>
	/// Static comparison function used for sorting.
	/// </summary>

	static int CompareFunc (UICamera a, UICamera b)
	{
		if (a.cachedCamera.depth < b.cachedCamera.depth) return 1;
		if (a.cachedCamera.depth > b.cachedCamera.depth) return -1;
		return 0;
	}

	struct DepthEntry
	{
		public int depth;
		public RaycastHit hit;
		public GameObject go;
	}

	static DepthEntry mHit = new DepthEntry();
	static BetterList<DepthEntry> mHits = new BetterList<DepthEntry>();

	/// <summary>
	/// Returns the object under the specified position.
	/// </summary>

	static public bool Raycast (Vector3 inPos)
	{
		for (int i = 0; i < list.size; ++i)
		{
			UICamera cam = list.buffer[i];
			
			// Skip inactive scripts
			if (!cam.enabled || !NGUITools.GetActive(cam.gameObject)) continue;

			// Convert to view space
			currentCamera = cam.cachedCamera;
			Vector3 pos = currentCamera.ScreenToViewportPoint(inPos);
			if (float.IsNaN(pos.x) || float.IsNaN(pos.y)) continue;

			// If it's outside the camera's viewport, do nothing
			if (pos.x < 0f || pos.x > 1f || pos.y < 0f || pos.y > 1f) continue;

			// Cast a ray into the screen
			Ray ray = currentCamera.ScreenPointToRay(inPos);

			// Raycast into the screen
			int mask = currentCamera.cullingMask & (int)cam.eventReceiverMask;
			float dist = (cam.rangeDistance > 0f) ? cam.rangeDistance : currentCamera.farClipPlane - currentCamera.nearClipPlane;

			if (cam.eventType == EventType.World_3D)
			{
				if (Physics.Raycast(ray, out lastHit, dist, mask))
				{
					hoveredObject = lastHit.collider.gameObject;
					return true;
				}
				continue;
			}
			else if (cam.eventType == EventType.UI_3D)
			{
				RaycastHit[] hits = Physics.RaycastAll(ray, dist, mask);

				if (hits.Length > 1)
				{
					for (int b = 0; b < hits.Length; ++b)
					{
						GameObject go = hits[b].collider.gameObject;
						UIWidget w = go.GetComponent<UIWidget>();

						if (w != null)
						{
							if (!w.isVisible) continue;
							if (w.hitCheck != null && !w.hitCheck(hits[b].point)) continue;
						}
						else
						{
							UIRect rect = NGUITools.FindInParents<UIRect>(go);
							if (rect != null && rect.finalAlpha < 0.001f) continue;
						}

						mHit.depth = NGUITools.CalculateRaycastDepth(go);

						if (mHit.depth != int.MaxValue)
						{
							mHit.hit = hits[b];
							mHit.go = hits[b].collider.gameObject;
							mHits.Add(mHit);
						}
					}

					mHits.Sort(delegate(DepthEntry r1, DepthEntry r2) { return r2.depth.CompareTo(r1.depth); });

					for (int b = 0; b < mHits.size; ++b)
					{
#if UNITY_FLASH
						if (IsVisible(mHits.buffer[b]))
#else
						if (IsVisible(ref mHits.buffer[b]))
#endif
						{
							lastHit = mHits[b].hit;
							hoveredObject = mHits[b].go;
							lastWorldPosition = hits[b].point;
							mHits.Clear();
							return true;
						}
					}
					mHits.Clear();
				}
				else if (hits.Length == 1)
				{
					GameObject go = hits[0].collider.gameObject;
					UIWidget w = go.GetComponent<UIWidget>();

					if (w != null)
					{
						if (!w.isVisible) continue;
						if (w.hitCheck != null && !w.hitCheck(hits[0].point)) continue;
					}
					else
					{
						UIRect rect = NGUITools.FindInParents<UIRect>(go);
						if (rect != null && rect.finalAlpha < 0.001f) continue;
					}

					if (IsVisible(hits[0].point, hits[0].collider.gameObject))
					{
						lastHit = hits[0];
						lastWorldPosition = hits[0].point;
						hoveredObject = lastHit.collider.gameObject;
						return true;
					}
				}
				continue;
			}
#if !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
			else if (cam.eventType == EventType.World_2D)
			{
				if (m2DPlane.Raycast(ray, out dist))
				{
					Vector3 point = ray.GetPoint(dist);
					Collider2D c2d = Physics2D.OverlapPoint(point, mask);

					if (c2d)
					{
						lastWorldPosition = point;
						hoveredObject = c2d.gameObject;
						return true;
					}
				}
				continue;
			}
			else if (cam.eventType == EventType.UI_2D)
			{
				if (m2DPlane.Raycast(ray, out dist))
				{
					lastWorldPosition = ray.GetPoint(dist);
					Collider2D[] hits = Physics2D.OverlapPointAll(lastWorldPosition, mask);

					if (hits.Length > 1)
					{
						for (int b = 0; b < hits.Length; ++b)
						{
							GameObject go = hits[b].gameObject;
							UIWidget w = go.GetComponent<UIWidget>();

							if (w != null)
							{
								if (!w.isVisible) continue;
								if (w.hitCheck != null && !w.hitCheck(lastWorldPosition)) continue;
							}
							else
							{
								UIRect rect = NGUITools.FindInParents<UIRect>(go);
								if (rect != null && rect.finalAlpha < 0.001f) continue;
							}

							mHit.depth = NGUITools.CalculateRaycastDepth(go);

							if (mHit.depth != int.MaxValue)
							{
								mHit.go = go;
								mHits.Add(mHit);
							}
						}

						mHits.Sort(delegate(DepthEntry r1, DepthEntry r2) { return r2.depth.CompareTo(r1.depth); });

						for (int b = 0; b < mHits.size; ++b)
						{
#if UNITY_FLASH
							if (IsVisible(mHits.buffer[b]))
#else
							if (IsVisible(ref mHits.buffer[b]))
#endif
							{
								hoveredObject = mHits[b].go;
								mHits.Clear();
								return true;
							}
						}
						mHits.Clear();
					}
					else if (hits.Length == 1)
					{
						GameObject go = hits[0].gameObject;
						UIWidget w = go.GetComponent<UIWidget>();

						if (w != null)
						{
							if (!w.isVisible) continue;
							if (w.hitCheck != null && !w.hitCheck(lastWorldPosition)) continue;
						}
						else
						{
							UIRect rect = NGUITools.FindInParents<UIRect>(go);
							if (rect != null && rect.finalAlpha < 0.001f) continue;
						}

						if (IsVisible(lastWorldPosition, go))
						{
							hoveredObject = go;
							return true;
						}
					}
				}
				continue;
			}
#endif
		}
		return false;
	}

#if !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
	static Plane m2DPlane = new Plane(Vector3.back, 0f);
#endif

	/// <summary>
	/// Helper function to check if the specified hit is visible by the panel.
	/// </summary>

	static bool IsVisible (Vector3 worldPoint, GameObject go)
	{
		UIPanel panel = NGUITools.FindInParents<UIPanel>(go);

		while (panel != null)
		{
			if (!panel.IsVisible(worldPoint)) return false;
			panel = panel.parentPanel;
		}
		return true;
	}

	/// <summary>
	/// Helper function to check if the specified hit is visible by the panel.
	/// </summary>

#if UNITY_FLASH
	static bool IsVisible (DepthEntry de)
#else
	static bool IsVisible (ref DepthEntry de)
#endif
	{
		UIPanel panel = NGUITools.FindInParents<UIPanel>(de.go);

		while (panel != null)
		{
			if (!panel.IsVisible(de.hit.point)) return false;
			panel = panel.parentPanel;
		}
		return true;
	}

	/// <summary>
	/// Whether the specified object should be highlighted.
	/// </summary>

	static public bool IsHighlighted (GameObject go)
	{
		if (UICamera.currentScheme == UICamera.ControlScheme.Mouse)
			return (UICamera.hoveredObject == go);

		if (UICamera.currentScheme == UICamera.ControlScheme.Controller)
			return (UICamera.selectedObject == go);

		return false;
	}

	/// <summary>
	/// Find the camera responsible for handling events on objects of the specified layer.
	/// </summary>

	static public UICamera FindCameraForLayer (int layer)
	{
		int layerMask = 1 << layer;

		for (int i = 0; i < list.size; ++i)
		{
			UICamera cam = list.buffer[i];
			Camera uc = cam.cachedCamera;
			if ((uc != null) && (uc.cullingMask & layerMask) != 0) return cam;
		}
		return null;
	}

	/// <summary>
	/// Using the keyboard will result in 1 or -1, depending on whether up or down keys have been pressed.
	/// </summary>

	static int GetDirection (KeyCode up, KeyCode down)
	{
		if (Input.GetKeyDown(up)) return 1;
		if (Input.GetKeyDown(down)) return -1;
		return 0;
	}

	/// <summary>
	/// Using the keyboard will result in 1 or -1, depending on whether up or down keys have been pressed.
	/// </summary>

	static int GetDirection (KeyCode up0, KeyCode up1, KeyCode down0, KeyCode down1)
	{
		if (Input.GetKeyDown(up0) || Input.GetKeyDown(up1)) return 1;
		if (Input.GetKeyDown(down0) || Input.GetKeyDown(down1)) return -1;
		return 0;
	}

	/// <summary>
	/// Using the joystick to move the UI results in 1 or -1 if the threshold has been passed, mimicking up/down keys.
	/// </summary>

	static int GetDirection (string axis)
	{
		float time = RealTime.time;

		if (mNextEvent < time && !string.IsNullOrEmpty(axis))
		{
			float val = Input.GetAxis(axis);

			if (val > 0.75f)
			{
				mNextEvent = time + 0.25f;
				return 1;
			}

			if (val < -0.75f)
			{
				mNextEvent = time + 0.25f;
				return -1;
			}
		}
		return 0;
	}

	static bool mNotifying = false;

	/// <summary>
	/// Generic notification function. Used in place of SendMessage to shorten the code and allow for more than one receiver.
	/// </summary>

	static public void Notify (GameObject go, string funcName, object obj)
	{
		if (mNotifying) return;
		mNotifying = true;

		if (NGUITools.GetActive(go))
		{
			go.SendMessage(funcName, obj, SendMessageOptions.DontRequireReceiver);

			if (genericEventHandler != null && genericEventHandler != go)
			{
				genericEventHandler.SendMessage(funcName, obj, SendMessageOptions.DontRequireReceiver);
			}
		}
		mNotifying = false;
	}

	/// <summary>
	/// Get the details of the specified mouse button.
	/// </summary>

	static public MouseOrTouch GetMouse (int button) { return mMouse[button]; }

	/// <summary>
	/// Get or create a touch event.
	/// </summary>

	static public MouseOrTouch GetTouch (int id)
	{
		MouseOrTouch touch = null;

		if (id < 0) return GetMouse(-id - 1);

		if (!mTouches.TryGetValue(id, out touch))
		{
			touch = new MouseOrTouch();
			touch.touchBegan = true;
			mTouches.Add(id, touch);
		}
		return touch;
	}

	/// <summary>
	/// Remove a touch event from the list.
	/// </summary>

	static public void RemoveTouch (int id) { mTouches.Remove(id); }

	/// <summary>
	/// Add this camera to the list.
	/// </summary>

	void Awake ()
	{
		mWidth = Screen.width;
		mHeight = Screen.height;

		if (Application.platform == RuntimePlatform.Android ||
			Application.platform == RuntimePlatform.IPhonePlayer
#if !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1
			|| Application.platform == RuntimePlatform.WP8Player
#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3
			|| Application.platform == RuntimePlatform.BB10Player
#else
			|| Application.platform == RuntimePlatform.BlackBerryPlayer
#endif
#endif
			)
		{
			useMouse = false;
			useTouch = true;

			if (Application.platform == RuntimePlatform.IPhonePlayer)
			{
				useKeyboard = false;
				useController = false;
			}
		}
		else if (Application.platform == RuntimePlatform.PS3 ||
				 Application.platform == RuntimePlatform.XBOX360)
		{
			useMouse = false;
			useTouch = false;
			useKeyboard = false;
			useController = true;
		}

		// Save the starting mouse position
		mMouse[0].pos.x = Input.mousePosition.x;
		mMouse[0].pos.y = Input.mousePosition.y;

		for (int i = 1; i < 3; ++i)
		{
			mMouse[i].pos = mMouse[0].pos;
			mMouse[i].lastPos = mMouse[0].pos;
		}
		lastTouchPosition = mMouse[0].pos;
	}

	/// <summary>
	/// Sort the list when enabled.
	/// </summary>

	void OnEnable () { list.Add(this); list.Sort(CompareFunc); }

	/// <summary>
	/// Remove this camera from the list.
	/// </summary>

	void OnDisable () { list.Remove(this); }

#if !UNITY_3_5 && !UNITY_4_0
	/// <summary>
	/// We don't want the camera to send out any kind of mouse events.
	/// </summary>
	
	void Start ()
	{
		if (eventType != EventType.World_3D && cachedCamera.transparencySortMode != TransparencySortMode.Orthographic)
			cachedCamera.transparencySortMode = TransparencySortMode.Orthographic;

		if (Application.isPlaying) cachedCamera.eventMask = 0;
		if (handlesEvents) NGUIDebug.debugRaycast = debug;
	}
#else
	void Start () { if (handlesEvents) NGUIDebug.debugRaycast = debug; }
#endif

#if UNITY_EDITOR
	void OnValidate () { Start(); }
#endif

	/// <summary>
	/// Check the input and send out appropriate events.
	/// </summary>

	void Update ()
	{
		// Only the first UI layer should be processing events
#if UNITY_EDITOR
		if (!Application.isPlaying || !handlesEvents) return;
#else
		if (!handlesEvents) return;
#endif
		current = this;

		// Process touch events first
		if (useTouch) ProcessTouches ();
		else if (useMouse) ProcessMouse();

		// Custom input processing
		if (onCustomInput != null) onCustomInput();

		// Clear the selection on the cancel key, but only if mouse input is allowed
		if (useMouse && mCurrentSelection != null)
		{
			if (cancelKey0 != KeyCode.None && Input.GetKeyDown(cancelKey0))
			{
				currentScheme = ControlScheme.Controller;
				currentKey = cancelKey0;
				selectedObject = null;
			}
			else if (cancelKey1 != KeyCode.None && Input.GetKeyDown(cancelKey1))
			{
				currentScheme = ControlScheme.Controller;
				currentKey = cancelKey1;
				selectedObject = null;
			}
		}

		// If nothing is selected, input focus is lost
		if (mCurrentSelection == null) inputHasFocus = false;

		// Update the keyboard and joystick events
		if (mCurrentSelection != null) ProcessOthers();

		// If it's time to show a tooltip, inform the object we're hovering over
		if (useMouse && mHover != null)
		{
			float scroll = !string.IsNullOrEmpty(scrollAxisName) ? Input.GetAxis(scrollAxisName) : 0f;
			if (scroll != 0f) Notify(mHover, "OnScroll", scroll);

			if (showTooltips && mTooltipTime != 0f && (mTooltipTime < RealTime.time ||
				Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
			{
				mTooltip = mHover;
				ShowTooltip(true);
			}
		}
		current = null;
	}

	/// <summary>
	/// Keep an eye on screen size changes.
	/// </summary>

	void LateUpdate ()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying || !handlesEvents) return;
#else
		if (!handlesEvents) return;
#endif
		int w = Screen.width;
		int h = Screen.height;

		if (w != mWidth || h != mHeight)
		{
			mWidth = w;
			mHeight = h;

			UIRoot.Broadcast("UpdateAnchors");

			if (onScreenResize != null)
				onScreenResize();
		}
	}

	/// <summary>
	/// Update mouse input.
	/// </summary>

	public void ProcessMouse ()
	{
		// Update the position and delta
		lastTouchPosition = Input.mousePosition;
		mMouse[0].delta = lastTouchPosition - mMouse[0].pos;
		mMouse[0].pos = lastTouchPosition;
		bool posChanged = mMouse[0].delta.sqrMagnitude > 0.001f;

		// Propagate the updates to the other mouse buttons
		for (int i = 1; i < 3; ++i)
		{
			mMouse[i].pos = mMouse[0].pos;
			mMouse[i].delta = mMouse[0].delta;
		}

		// Is any button currently pressed?
		bool isPressed = false;
		bool justPressed = false;

		for (int i = 0; i < 3; ++i)
		{
			if (Input.GetMouseButtonDown(i))
			{
				currentScheme = ControlScheme.Mouse;
				justPressed = true;
				isPressed = true;
			}
			else if (Input.GetMouseButton(i))
			{
				currentScheme = ControlScheme.Mouse;
				isPressed = true;
			}
		}

		// No need to perform raycasts every frame
		if (isPressed || posChanged || mNextRaycast < RealTime.time)
		{
			mNextRaycast = RealTime.time + 0.02f;
			if (!Raycast(Input.mousePosition)) hoveredObject = fallThrough;
			if (hoveredObject == null) hoveredObject = genericEventHandler;
			for (int i = 0; i < 3; ++i) mMouse[i].current = hoveredObject;
		}

		bool highlightChanged = (mMouse[0].last != mMouse[0].current);
		if (highlightChanged) currentScheme = ControlScheme.Mouse;

		if (isPressed)
		{
			// A button was pressed -- cancel the tooltip
			mTooltipTime = 0f;
		}
		else if (posChanged && (!stickyTooltip || highlightChanged))
		{
			if (mTooltipTime != 0f)
			{
				// Delay the tooltip
				mTooltipTime = RealTime.time + tooltipDelay;
			}
			else if (mTooltip != null)
			{
				// Hide the tooltip
				ShowTooltip(false);
			}
		}

		// The button was released over a different object -- remove the highlight from the previous
		if ((justPressed || !isPressed) && mHover != null && highlightChanged)
		{
			currentScheme = ControlScheme.Mouse;
			if (mTooltip != null) ShowTooltip(false);
			Notify(mHover, "OnHover", false);
			mHover = null;
		}

		// Process all 3 mouse buttons as individual touches
		for (int i = 0; i < 3; ++i)
		{
			bool pressed = Input.GetMouseButtonDown(i);
			bool unpressed = Input.GetMouseButtonUp(i);

			if (pressed || unpressed) currentScheme = ControlScheme.Mouse;

			currentTouch = mMouse[i];
			currentTouchID = -1 - i;
			currentKey = KeyCode.Mouse0 + i;
	
			// We don't want to update the last camera while there is a touch happening
			if (pressed) currentTouch.pressedCam = currentCamera;
			else if (currentTouch.pressed != null) currentCamera = currentTouch.pressedCam;
	
			// Process the mouse events
			ProcessTouch(pressed, unpressed);
			currentKey = KeyCode.None;
		}
		currentTouch = null;

		// If nothing is pressed and there is an object under the touch, highlight it
		if (!isPressed && highlightChanged)
		{
			currentScheme = ControlScheme.Mouse;
			mTooltipTime = RealTime.time + tooltipDelay;
			mHover = mMouse[0].current;
			Notify(mHover, "OnHover", true);
		}

		// Update the last value
		mMouse[0].last = mMouse[0].current;
		for (int i = 1; i < 3; ++i) mMouse[i].last = mMouse[0].last;
	}

	/// <summary>
	/// Update touch-based events.
	/// </summary>

	public void ProcessTouches ()
	{
		currentScheme = ControlScheme.Touch;

		for (int i = 0; i < Input.touchCount; ++i)
		{
			Touch touch = Input.GetTouch(i);

			currentTouchID = allowMultiTouch ? touch.fingerId : 1;
			currentTouch = GetTouch(currentTouchID);

			bool pressed = (touch.phase == TouchPhase.Began) || currentTouch.touchBegan;
			bool unpressed = (touch.phase == TouchPhase.Canceled) || (touch.phase == TouchPhase.Ended);
			currentTouch.touchBegan = false;

			// Although input.deltaPosition can be used, calculating it manually is safer (just in case)
			currentTouch.delta = pressed ? Vector2.zero : touch.position - currentTouch.pos;
			currentTouch.pos = touch.position;

			// Raycast into the screen
			if (!Raycast(currentTouch.pos)) hoveredObject = fallThrough;
			if (hoveredObject == null) hoveredObject = genericEventHandler;
			currentTouch.last = currentTouch.current;
			currentTouch.current = hoveredObject;
			lastTouchPosition = currentTouch.pos;

			// We don't want to update the last camera while there is a touch happening
			if (pressed) currentTouch.pressedCam = currentCamera;
			else if (currentTouch.pressed != null) currentCamera = currentTouch.pressedCam;

			// Double-tap support
			if (touch.tapCount > 1) currentTouch.clickTime = RealTime.time;

			// Process the events from this touch
			ProcessTouch(pressed, unpressed);

			// If the touch has ended, remove it from the list
			if (unpressed) RemoveTouch(currentTouchID);
			currentTouch.last = null;
			currentTouch = null;

			// Don't consider other touches
			if (!allowMultiTouch) break;
		}

		if (Input.touchCount == 0)
		{
			if (useMouse) ProcessMouse();
#if UNITY_EDITOR
			else ProcessFakeTouches();
#endif
		}
	}

	/// <summary>
	/// Process fake touch events where the mouse acts as a touch device.
	/// Useful for testing mobile functionality in the editor.
	/// </summary>

	void ProcessFakeTouches ()
	{
		bool pressed = Input.GetMouseButtonDown(0);
		bool unpressed = Input.GetMouseButtonUp(0);
		bool held = Input.GetMouseButton(0);

		if (pressed || unpressed || held)
		{
			currentTouchID = 1;
			currentTouch = mMouse[0];
			currentTouch.touchBegan = pressed;

			Vector2 pos = Input.mousePosition;
			currentTouch.delta = pressed ? Vector2.zero : pos - currentTouch.pos;
			currentTouch.pos = pos;

			// Raycast into the screen
			if (!Raycast(currentTouch.pos)) hoveredObject = fallThrough;
			if (hoveredObject == null) hoveredObject = genericEventHandler;
			currentTouch.last = currentTouch.current;
			currentTouch.current = hoveredObject;
			lastTouchPosition = currentTouch.pos;

			// We don't want to update the last camera while there is a touch happening
			if (pressed) currentTouch.pressedCam = currentCamera;
			else if (currentTouch.pressed != null) currentCamera = currentTouch.pressedCam;

			// Process the events from this touch
			ProcessTouch(pressed, unpressed);

			// If the touch has ended, remove it from the list
			if (unpressed) RemoveTouch(currentTouchID);
			currentTouch.last = null;
			currentTouch = null;
		}
	}

	/// <summary>
	/// Process keyboard and joystick events.
	/// </summary>

	public void ProcessOthers ()
	{
		currentTouchID = -100;
		currentTouch = controller;

		bool submitKeyDown = false;
		bool submitKeyUp = false;

		if (submitKey0 != KeyCode.None && Input.GetKeyDown(submitKey0))
		{
			currentKey = submitKey0;
			submitKeyDown = true;
		}

		if (submitKey1 != KeyCode.None && Input.GetKeyDown(submitKey1))
		{
			currentKey = submitKey1;
			submitKeyDown = true;
		}

		if (submitKey0 != KeyCode.None && Input.GetKeyUp(submitKey0))
		{
			currentKey = submitKey0;
			submitKeyUp = true;
		}

		if (submitKey1 != KeyCode.None && Input.GetKeyUp(submitKey1))
		{
			currentKey = submitKey1;
			submitKeyUp = true;
		}

		if (submitKeyDown || submitKeyUp)
		{
			currentScheme = ControlScheme.Controller;
			currentTouch.last = currentTouch.current;
			currentTouch.current = mCurrentSelection;
			ProcessTouch(submitKeyDown, submitKeyUp);
			currentTouch.last = null;
		}

		int vertical = 0;
		int horizontal = 0;

		if (useKeyboard)
		{
			if (inputHasFocus)
			{
				vertical += GetDirection(KeyCode.UpArrow, KeyCode.DownArrow);
				horizontal += GetDirection(KeyCode.RightArrow, KeyCode.LeftArrow);
			}
			else
			{
				vertical += GetDirection(KeyCode.W, KeyCode.UpArrow, KeyCode.S, KeyCode.DownArrow);
				horizontal += GetDirection(KeyCode.D, KeyCode.RightArrow, KeyCode.A, KeyCode.LeftArrow);
			}
		}

		if (useController)
		{
			if (!string.IsNullOrEmpty(verticalAxisName)) vertical += GetDirection(verticalAxisName);
			if (!string.IsNullOrEmpty(horizontalAxisName)) horizontal += GetDirection(horizontalAxisName);
		}

		// Send out key notifications
		if (vertical != 0)
		{
			currentScheme = ControlScheme.Controller;
			Notify(mCurrentSelection, "OnKey", vertical > 0 ? KeyCode.UpArrow : KeyCode.DownArrow);
		}
		
		if (horizontal != 0)
		{
			currentScheme = ControlScheme.Controller;
			Notify(mCurrentSelection, "OnKey", horizontal > 0 ? KeyCode.RightArrow : KeyCode.LeftArrow);
		}
		
		if (useKeyboard && Input.GetKeyDown(KeyCode.Tab))
		{
			currentKey = KeyCode.Tab;
			currentScheme = ControlScheme.Controller;
			Notify(mCurrentSelection, "OnKey", KeyCode.Tab);
		}

		// Send out the cancel key notification
		if (cancelKey0 != KeyCode.None && Input.GetKeyDown(cancelKey0))
		{
			currentKey = cancelKey0;
			currentScheme = ControlScheme.Controller;
			Notify(mCurrentSelection, "OnKey", KeyCode.Escape);
		}

		if (cancelKey1 != KeyCode.None && Input.GetKeyDown(cancelKey1))
		{
			currentKey = cancelKey1;
			currentScheme = ControlScheme.Controller;
			Notify(mCurrentSelection, "OnKey", KeyCode.Escape);
		}

		currentTouch = null;
		currentKey = KeyCode.None;
	}

	/// <summary>
	/// Process the events of the specified touch.
	/// </summary>

	public void ProcessTouch (bool pressed, bool unpressed)
	{
		// Whether we're using the mouse
		bool isMouse = (currentScheme == ControlScheme.Mouse);
		float drag   = isMouse ? mouseDragThreshold : touchDragThreshold;
		float click  = isMouse ? mouseClickThreshold : touchClickThreshold;

		// So we can use sqrMagnitude below
		drag *= drag;
		click *= click;

		// Send out the press message
		if (pressed)
		{
			if (mTooltip != null) ShowTooltip(false);

			currentTouch.pressStarted = true;
			Notify(currentTouch.pressed, "OnPress", false);
			currentTouch.pressed = currentTouch.current;
			currentTouch.dragged = currentTouch.current;
			currentTouch.clickNotification = ClickNotification.BasedOnDelta;
			currentTouch.totalDelta = Vector2.zero;
			currentTouch.dragStarted = false;
			Notify(currentTouch.pressed, "OnPress", true);

			// Update the selection
			if (currentTouch.pressed != mCurrentSelection)
			{
				if (mTooltip != null) ShowTooltip(false);
				currentScheme = ControlScheme.Touch;
				selectedObject = currentTouch.pressed;
			}
		}
		else if (currentTouch.pressed != null && (currentTouch.delta.sqrMagnitude != 0f || currentTouch.current != currentTouch.last))
		{
			// Keep track of the total movement
			currentTouch.totalDelta += currentTouch.delta;
			float mag = currentTouch.totalDelta.sqrMagnitude;
			bool justStarted = false;

			// If the drag process hasn't started yet but we've already moved off the object, start it immediately
			if (!currentTouch.dragStarted && currentTouch.last != currentTouch.current)
			{
				currentTouch.dragStarted = true;
				currentTouch.delta = currentTouch.totalDelta;

				// OnDragOver is sent for consistency, so that OnDragOut is always preceded by OnDragOver
				isDragging = true;
				Notify(currentTouch.dragged, "OnDragStart", null);
				Notify(currentTouch.last, "OnDragOver", currentTouch.dragged);
				isDragging = false;
			}
			else if (!currentTouch.dragStarted && drag < mag)
			{
				// If the drag event has not yet started, see if we've dragged the touch far enough to start it
				justStarted = true;
				currentTouch.dragStarted = true;
				currentTouch.delta = currentTouch.totalDelta;
			}

			// If we're dragging the touch, send out drag events
			if (currentTouch.dragStarted)
			{
				if (mTooltip != null) ShowTooltip(false);

				isDragging = true;
				bool isDisabled = (currentTouch.clickNotification == ClickNotification.None);

				if (justStarted)
				{
					Notify(currentTouch.dragged, "OnDragStart", null);
					Notify(currentTouch.current, "OnDragOver", currentTouch.dragged);
				}
				else if (currentTouch.last != currentTouch.current)
				{
					Notify(currentTouch.last, "OnDragOut", currentTouch.dragged);
					Notify(currentTouch.current, "OnDragOver", currentTouch.dragged);
				}

				Notify(currentTouch.dragged, "OnDrag", currentTouch.delta);

				currentTouch.last = currentTouch.current;
				isDragging = false;

				if (isDisabled)
				{
					// If the notification status has already been disabled, keep it as such
					currentTouch.clickNotification = ClickNotification.None;
				}
				else if (currentTouch.clickNotification == ClickNotification.BasedOnDelta && click < mag)
				{
					// We've dragged far enough to cancel the click
					currentTouch.clickNotification = ClickNotification.None;
				}
			}
		}

		// Send out the unpress message
		if (unpressed)
		{
			currentTouch.pressStarted = false;
			if (mTooltip != null) ShowTooltip(false);

			if (currentTouch.pressed != null)
			{
				// If there was a drag event in progress, make sure OnDragOut gets sent
				if (currentTouch.dragStarted)
				{
					Notify(currentTouch.last, "OnDragOut", currentTouch.dragged);
					Notify(currentTouch.dragged, "OnDragEnd", null);
				}

				// Send the notification of a touch ending
				Notify(currentTouch.pressed, "OnPress", false);

				// Send a hover message to the object
				if (isMouse) Notify(currentTouch.current, "OnHover", true);
				mHover = currentTouch.current;

				// If the button/touch was released on the same object, consider it a click and select it
				if (currentTouch.dragged == currentTouch.current ||
					(currentScheme != ControlScheme.Controller &&
					currentTouch.clickNotification != ClickNotification.None &&
					currentTouch.totalDelta.sqrMagnitude < drag))
				{
					if (currentTouch.pressed != mCurrentSelection)
					{
						mNextSelection = null;
						mCurrentSelection = currentTouch.pressed;
						Notify(currentTouch.pressed, "OnSelect", true);
					}
					else
					{
						mNextSelection = null;
						mCurrentSelection = currentTouch.pressed;
					}

					// If the touch should consider clicks, send out an OnClick notification
					if (currentTouch.clickNotification != ClickNotification.None && currentTouch.pressed == currentTouch.current)
					{
						float time = RealTime.time;

						Notify(currentTouch.pressed, "OnClick", null);

						if (currentTouch.clickTime + 0.35f > time)
						{
							Notify(currentTouch.pressed, "OnDoubleClick", null);
						}
						currentTouch.clickTime = time;
					}
				}
				else if (currentTouch.dragStarted) // The button/touch was released on a different object
				{
					// Send a drop notification (for drag & drop)
					Notify(currentTouch.current, "OnDrop", currentTouch.dragged);
				}
			}
			currentTouch.dragStarted = false;
			currentTouch.pressed = null;
			currentTouch.dragged = null;
		}
	}

	/// <summary>
	/// Show or hide the tooltip.
	/// </summary>

	public void ShowTooltip (bool val)
	{
		mTooltipTime = 0f;
		Notify(mTooltip, "OnTooltip", val);
		if (!val) mTooltip = null;
	}

#if !UNITY_EDITOR
	/// <summary>
	/// Clear all active press states when the application gets paused.
	/// </summary>

	void OnApplicationPause ()
	{
		MouseOrTouch prev = currentTouch;

		if (useTouch)
		{
			BetterList<int> ids = new BetterList<int>();

			foreach (KeyValuePair<int, MouseOrTouch> pair in mTouches)
			{
				if (pair.Value != null && pair.Value.pressed)
				{
					currentTouch = pair.Value;
					currentTouchID = pair.Key;
					currentScheme = ControlScheme.Touch;
					currentTouch.clickNotification = ClickNotification.None;
					ProcessTouch(false, true);
					ids.Add(currentTouchID);
				}
			}

			for (int i = 0; i < ids.size; ++i)
				RemoveTouch(ids[i]);
		}

		if (useMouse)
		{
			for (int i = 0; i < 3; ++i)
			{
				if (mMouse[i].pressed)
				{
					currentTouch = mMouse[i];
					currentTouchID = -1 - i;
					currentKey = KeyCode.Mouse0 + i;
					currentScheme = ControlScheme.Mouse;
					currentTouch.clickNotification = ClickNotification.None;
					ProcessTouch(false, true);
				}
			}
		}

		if (useController)
		{
			if (controller.pressed)
			{
				currentTouch = controller;
				currentTouchID = -100;
				currentScheme = ControlScheme.Controller;
				currentTouch.last = currentTouch.current;
				currentTouch.current = mCurrentSelection;
				currentTouch.clickNotification = ClickNotification.None;
				ProcessTouch(false, true);
				currentTouch.last = null;
			}
		}
		currentTouch = prev;
	}
#endif
}

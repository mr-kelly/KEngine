//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

/// <summary>
/// Helper class containing generic functions used throughout the UI library.
/// </summary>

static public class NGUITools
{
	static AudioListener mListener;

	static bool mLoaded = false;
	static float mGlobalVolume = 1f;

	/// <summary>
	/// Globally accessible volume affecting all sounds played via NGUITools.PlaySound().
	/// </summary>

	static public float soundVolume
	{
		get
		{
			if (!mLoaded)
			{
				mLoaded = true;
				mGlobalVolume = PlayerPrefs.GetFloat("Sound", 1f);
			}
			return mGlobalVolume;
		}
		set
		{
			if (mGlobalVolume != value)
			{
				mLoaded = true;
				mGlobalVolume = value;
				PlayerPrefs.SetFloat("Sound", value);
			}
		}
	}

	/// <summary>
	/// Helper function -- whether the disk access is allowed.
	/// </summary>

	static public bool fileAccess
	{
		get
		{
			return Application.platform != RuntimePlatform.WindowsWebPlayer &&
				Application.platform != RuntimePlatform.OSXWebPlayer;
		}
	}

	/// <summary>
	/// Play the specified audio clip.
	/// </summary>

	static public AudioSource PlaySound (AudioClip clip) { return PlaySound(clip, 1f, 1f); }

	/// <summary>
	/// Play the specified audio clip with the specified volume.
	/// </summary>

	static public AudioSource PlaySound (AudioClip clip, float volume) { return PlaySound(clip, volume, 1f); }

	/// <summary>
	/// Play the specified audio clip with the specified volume and pitch.
	/// </summary>

	static public AudioSource PlaySound (AudioClip clip, float volume, float pitch)
	{
		volume *= soundVolume;

		if (clip != null && volume > 0.01f)
		{
			if (mListener == null || !NGUITools.GetActive(mListener))
			{
				AudioListener[] listeners = GameObject.FindObjectsOfType(typeof(AudioListener)) as AudioListener[];

				if (listeners != null)
				{
					for (int i = 0; i < listeners.Length; ++i)
					{
						if (NGUITools.GetActive(listeners[i]))
						{
							mListener = listeners[i];
							break;
						}
					}
				}

				if (mListener == null)
				{
					Camera cam = Camera.main;
					if (cam == null) cam = GameObject.FindObjectOfType(typeof(Camera)) as Camera;
					if (cam != null) mListener = cam.gameObject.AddComponent<AudioListener>();
				}
			}

			if (mListener != null && mListener.enabled && NGUITools.GetActive(mListener.gameObject))
			{
				AudioSource source = mListener.audio;
				if (source == null) source = mListener.gameObject.AddComponent<AudioSource>();
				source.pitch = pitch;
				source.PlayOneShot(clip, volume);
				return source;
			}
		}
		return null;
	}

	/// <summary>
	/// New WWW call can fail if the crossdomain policy doesn't check out. Exceptions suck. It's much more elegant to check for null instead.
	/// </summary>

	static public WWW OpenURL (string url)
	{
#if UNITY_FLASH
		Debug.LogError("WWW is not yet implemented in Flash");
		return null;
#else
		WWW www = null;
		try { www = new WWW(url); }
		catch (System.Exception ex) { Debug.LogError(ex.Message); }
		return www;
#endif
	}

	/// <summary>
	/// New WWW call can fail if the crossdomain policy doesn't check out. Exceptions suck. It's much more elegant to check for null instead.
	/// </summary>

	static public WWW OpenURL (string url, WWWForm form)
	{
		if (form == null) return OpenURL(url);
#if UNITY_FLASH
		Debug.LogError("WWW is not yet implemented in Flash");
		return null;
#else
		WWW www = null;
		try { www = new WWW(url, form); }
		catch (System.Exception ex) { Debug.LogError(ex != null ? ex.Message : "<null>"); }
		return www;
#endif
	}

	/// <summary>
	/// Same as Random.Range, but the returned value is between min and max, inclusive.
	/// Unity's Random.Range is less than max instead, unless min == max.
	/// This means Range(0,1) produces 0 instead of 0 or 1. That's unacceptable.
	/// </summary>

	static public int RandomRange (int min, int max)
	{
		if (min == max) return min;
		return UnityEngine.Random.Range(min, max + 1);
	}

	/// <summary>
	/// Returns the hierarchy of the object in a human-readable format.
	/// </summary>

	static public string GetHierarchy (GameObject obj)
	{
		if (obj == null) return "";
		string path = obj.name;

		while (obj.transform.parent != null)
		{
			obj = obj.transform.parent.gameObject;
			path = obj.name + "\\" + path;
		}
		return path;
	}

	/// <summary>
	/// Find all active objects of specified type.
	/// </summary>

	static public T[] FindActive<T> () where T : Component
	{
//#if UNITY_3_5 || UNITY_4_0
//        return GameObject.FindSceneObjectsOfType(typeof(T)) as T[];
//#else
		return GameObject.FindObjectsOfType(typeof(T)) as T[];
//#endif
	}

	/// <summary>
	/// Find the camera responsible for drawing the objects on the specified layer.
	/// </summary>

	static public Camera FindCameraForLayer (int layer)
	{
		int layerMask = 1 << layer;

		Camera cam;

		for (int i = 0; i < UICamera.list.size; ++i)
		{
			cam = UICamera.list.buffer[i].cachedCamera;
			if (cam && (cam.cullingMask & layerMask) != 0)
				return cam;
		}

		cam = Camera.main;
		if (cam && (cam.cullingMask & layerMask) != 0) return cam;

		Camera[] cameras = NGUITools.FindActive<Camera>();

		for (int i = 0, imax = cameras.Length; i < imax; ++i)
		{
			cam = cameras[i];
			if (cam && (cam.cullingMask & layerMask) != 0)
				return cam;
		}
		return null;
	}

	/// <summary>
	/// Add a collider to the game object containing one or more widgets.
	/// </summary>

	static public void AddWidgetCollider (GameObject go) { AddWidgetCollider(go, false); }

	/// <summary>
	/// Add a collider to the game object containing one or more widgets.
	/// </summary>

	static public void AddWidgetCollider (GameObject go, bool considerInactive)
	{
		if (go != null)
		{
			// 3D collider
			Collider col = go.GetComponent<Collider>();
			BoxCollider box = col as BoxCollider;

			if (box != null)
			{
				UpdateWidgetCollider(box, considerInactive);
				return;
			}

			// Is there already another collider present? If so, do nothing.
			if (col != null) return;

#if !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
			// 2D collider
			BoxCollider2D box2 = go.GetComponent<BoxCollider2D>();

			if (box2 != null)
			{
				UpdateWidgetCollider(box2, considerInactive);
				return;
			}

			UICamera ui = UICamera.FindCameraForLayer(go.layer);

			if (ui != null && (ui.eventType == UICamera.EventType.World_2D || ui.eventType == UICamera.EventType.UI_2D))
			{
				box2 = go.AddComponent<BoxCollider2D>();
				box2.isTrigger = true;
#if UNITY_EDITOR
				UnityEditor.Undo.RegisterCreatedObjectUndo(box2, "Add Collider");
#endif
				UIWidget widget = go.GetComponent<UIWidget>();
				if (widget != null) widget.autoResizeBoxCollider = true;
				UpdateWidgetCollider(box2, considerInactive);
				return;
			}
			else
#endif
			{
				box = go.AddComponent<BoxCollider>();
#if !UNITY_3_5 && UNITY_EDITOR
				UnityEditor.Undo.RegisterCreatedObjectUndo(box, "Add Collider");
#endif
				box.isTrigger = true;

				UIWidget widget = go.GetComponent<UIWidget>();
				if (widget != null) widget.autoResizeBoxCollider = true;
				UpdateWidgetCollider(box, considerInactive);
			}
		}
		return;
	}

	/// <summary>
	/// Adjust the widget's collider based on the depth of the widgets, as well as the widget's dimensions.
	/// </summary>

	static public void UpdateWidgetCollider (GameObject go)
	{
		UpdateWidgetCollider(go, false);
	}

	/// <summary>
	/// Adjust the widget's collider based on the depth of the widgets, as well as the widget's dimensions.
	/// </summary>

	static public void UpdateWidgetCollider (GameObject go, bool considerInactive)
	{
		if (go != null)
		{
			BoxCollider bc = go.GetComponent<BoxCollider>();

			if (bc != null)
			{
				UpdateWidgetCollider(bc, considerInactive);
				return;
			}
#if !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
			BoxCollider2D box2 = go.GetComponent<BoxCollider2D>();
			if (box2 != null) UpdateWidgetCollider(box2, considerInactive);
#endif
		}
	}

	/// <summary>
	/// Adjust the widget's collider based on the depth of the widgets, as well as the widget's dimensions.
	/// </summary>

	static public void UpdateWidgetCollider (BoxCollider box, bool considerInactive)
	{
		if (box != null)
		{
			GameObject go = box.gameObject;
			UIWidget w = go.GetComponent<UIWidget>();

			if (w != null)
			{
				Vector3[] corners = w.localCorners;
				box.center = Vector3.Lerp(corners[0], corners[2], 0.5f);
				box.size = corners[2] - corners[0];
			}
			else
			{
				Bounds b = NGUIMath.CalculateRelativeWidgetBounds(go.transform, considerInactive);
				box.center = b.center;
				box.size = new Vector3(b.size.x, b.size.y, 0f);
			}
#if UNITY_EDITOR
			NGUITools.SetDirty(box);
#endif
		}
	}

#if !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
	/// <summary>
	/// Adjust the widget's collider based on the depth of the widgets, as well as the widget's dimensions.
	/// </summary>

	static public void UpdateWidgetCollider (BoxCollider2D box, bool considerInactive)
	{
		if (box != null)
		{
			GameObject go = box.gameObject;
			UIWidget w = go.GetComponent<UIWidget>();

			if (w != null)
			{
				Vector3[] corners = w.localCorners;
				box.center = Vector3.Lerp(corners[0], corners[2], 0.5f);
				box.size = corners[2] - corners[0];
			}
			else
			{
				Bounds b = NGUIMath.CalculateRelativeWidgetBounds(go.transform, considerInactive);
				box.center = b.center;
				box.size = new Vector2(b.size.x, b.size.y);
			}
#if UNITY_EDITOR
			NGUITools.SetDirty(box);
#endif
		}
	}
#endif

	/// <summary>
	/// Helper function that returns the string name of the type.
	/// </summary>

	static public string GetTypeName<T> ()
	{
		string s = typeof(T).ToString();
		if (s.StartsWith("UI")) s = s.Substring(2);
		else if (s.StartsWith("UnityEngine.")) s = s.Substring(12);
		return s;
	}

	/// <summary>
	/// Helper function that returns the string name of the type.
	/// </summary>

	static public string GetTypeName (UnityEngine.Object obj)
	{
		if (obj == null) return "Null";
		string s = obj.GetType().ToString();
		if (s.StartsWith("UI")) s = s.Substring(2);
		else if (s.StartsWith("UnityEngine.")) s = s.Substring(12);
		return s;
	}

	/// <summary>
	/// Convenience method that works without warnings in both Unity 3 and 4.
	/// </summary>

	static public void RegisterUndo (UnityEngine.Object obj, string name)
	{
#if UNITY_EDITOR
 #if UNITY_3_5 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2
		UnityEditor.Undo.RegisterUndo(obj, name);
 #else
		UnityEditor.Undo.RecordObject(obj, name);
 #endif
		NGUITools.SetDirty(obj);
#endif
	}

	/// <summary>
	/// Convenience function that marks the specified object as dirty in the Unity Editor.
	/// </summary>

	static public void SetDirty (UnityEngine.Object obj)
	{
#if UNITY_EDITOR
		if (obj)
		{
			//if (obj is Component) Debug.Log(NGUITools.GetHierarchy((obj as Component).gameObject), obj);
			//else if (obj is GameObject) Debug.Log(NGUITools.GetHierarchy(obj as GameObject), obj);
			//else Debug.Log("Hmm... " + obj.GetType(), obj);
			UnityEditor.EditorUtility.SetDirty(obj);
		}
#endif
	}

	/// <summary>
	/// Add a new child game object.
	/// </summary>

	static public GameObject AddChild (GameObject parent) { return AddChild(parent, true); }

	/// <summary>
	/// Add a new child game object.
	/// </summary>

	static public GameObject AddChild (GameObject parent, bool undo)
	{
		GameObject go = new GameObject();
#if UNITY_EDITOR
		if (undo) UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create Object");
#endif
		if (parent != null)
		{
			Transform t = go.transform;
			t.parent = parent.transform;
			t.localPosition = Vector3.zero;
			t.localRotation = Quaternion.identity;
			t.localScale = Vector3.one;
			go.layer = parent.layer;
		}
		return go;
	}

	/// <summary>
	/// Instantiate an object and add it to the specified parent.
	/// </summary>

	static public GameObject AddChild (GameObject parent, GameObject prefab)
	{
		GameObject go = GameObject.Instantiate(prefab) as GameObject;

#if UNITY_EDITOR && !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2
		UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create Object");
#endif

		if (go != null && parent != null)
		{
			Transform t = go.transform;
			t.parent = parent.transform;
			t.localPosition = Vector3.zero;
			t.localRotation = Quaternion.identity;
			t.localScale = Vector3.one;
			go.layer = parent.layer;
		}
		return go;
	}

	/// <summary>
	/// Calculate the game object's depth based on the widgets within, and also taking panel depth into consideration.
	/// </summary>

	static public int CalculateRaycastDepth (GameObject go)
	{
		UIWidget w = go.GetComponent<UIWidget>();
		if (w != null) return w.raycastDepth;

		UIWidget[] widgets = go.GetComponentsInChildren<UIWidget>();
		if (widgets.Length == 0) return 0;

		int depth = int.MaxValue;
		
		for (int i = 0, imax = widgets.Length; i < imax; ++i)
		{
			if (widgets[i].enabled)
				depth = Mathf.Min(depth, widgets[i].raycastDepth);
		}
		return depth;
	}

	/// <summary>
	/// Gathers all widgets and calculates the depth for the next widget.
	/// </summary>

	static public int CalculateNextDepth (GameObject go)
	{
		int depth = -1;
		UIWidget[] widgets = go.GetComponentsInChildren<UIWidget>();
		for (int i = 0, imax = widgets.Length; i < imax; ++i)
			depth = Mathf.Max(depth, widgets[i].depth);
		return depth + 1;
	}

	/// <summary>
	/// Gathers all widgets and calculates the depth for the next widget.
	/// </summary>

	static public int CalculateNextDepth (GameObject go, bool ignoreChildrenWithColliders)
	{
		if (ignoreChildrenWithColliders)
		{
			int depth = -1;
			UIWidget[] widgets = go.GetComponentsInChildren<UIWidget>();

			for (int i = 0, imax = widgets.Length; i < imax; ++i)
			{
				UIWidget w = widgets[i];
				if (w.cachedGameObject != go && w.collider != null) continue;
				depth = Mathf.Max(depth, w.depth);
			}
			return depth + 1;
		}
		return CalculateNextDepth(go);
	}

	/// <summary>
	/// Adjust the widgets' depth by the specified value.
	/// Returns '0' if nothing was adjusted, '1' if panels were adjusted, and '2' if widgets were adjusted.
	/// </summary>

	static public int AdjustDepth (GameObject go, int adjustment)
	{
		if (go != null)
		{
			UIPanel panel = go.GetComponent<UIPanel>();

			if (panel != null)
			{
				UIPanel[] panels = go.GetComponentsInChildren<UIPanel>(true);
				
				for (int i = 0; i < panels.Length; ++i)
				{
					UIPanel p = panels[i];
#if UNITY_EDITOR
					RegisterUndo(p, "Depth Change");
#endif
					p.depth = p.depth + adjustment;
				}
				return 1;
			}
			else
			{
				UIWidget[] widgets = go.GetComponentsInChildren<UIWidget>(true);

				for (int i = 0, imax = widgets.Length; i < imax; ++i)
				{
					UIWidget w = widgets[i];
#if UNITY_EDITOR
					RegisterUndo(w, "Depth Change");
#endif
					w.depth = w.depth + adjustment;
				}
				return 2;
			}
		}
		return 0;
	}

	/// <summary>
	/// Bring all of the widgets on the specified object forward.
	/// </summary>

	static public void BringForward (GameObject go)
	{
		int val = AdjustDepth(go, 1000);
		if (val == 1) NormalizePanelDepths();
		else if (val == 2) NormalizeWidgetDepths();
	}

	/// <summary>
	/// Push all of the widgets on the specified object back, making them appear behind everything else.
	/// </summary>

	static public void PushBack (GameObject go)
	{
		int val = AdjustDepth(go, -1000);
		if (val == 1) NormalizePanelDepths();
		else if (val == 2) NormalizeWidgetDepths();
	}

	/// <summary>
	/// Normalize the depths of all the widgets and panels in the scene, making them start from 0 and remain in order.
	/// </summary>

	static public void NormalizeDepths ()
	{
		NormalizeWidgetDepths();
		NormalizePanelDepths();
	}

	/// <summary>
	/// Normalize the depths of all the widgets in the scene, making them start from 0 and remain in order.
	/// </summary>

	static public void NormalizeWidgetDepths ()
	{
		UIWidget[] list = FindActive<UIWidget>();
		int size = list.Length;

		if (size > 0)
		{
			Array.Sort(list, UIWidget.FullCompareFunc);

			int start = 0;
			int current = list[0].depth;

			for (int i = 0; i < size; ++i)
			{
				UIWidget w = list[i];

				if (w.depth == current)
				{
					w.depth = start;
				}
				else
				{
					current = w.depth;
					w.depth = ++start;
				}
			}
		}
	}

	/// <summary>
	/// Normalize the depths of all the panels in the scene, making them start from 0 and remain in order.
	/// </summary>

	static public void NormalizePanelDepths ()
	{
		UIPanel[] list = FindActive<UIPanel>();
		int size = list.Length;

		if (size > 0)
		{
			Array.Sort(list, UIPanel.CompareFunc);

			int start = 0;
			int current = list[0].depth;

			for (int i = 0; i < size; ++i)
			{
				UIPanel p = list[i];

				if (p.depth == current)
				{
					p.depth = start;
				}
				else
				{
					current = p.depth;
					p.depth = ++start;
				}
			}
		}
	}

	/// <summary>
	/// Create a new UI.
	/// </summary>

	static public UIPanel CreateUI (bool advanced3D) { return CreateUI(null, advanced3D, -1); }

	/// <summary>
	/// Create a new UI.
	/// </summary>

	static public UIPanel CreateUI (bool advanced3D, int layer) { return CreateUI(null, advanced3D, layer); }

	/// <summary>
	/// Create a new UI.
	/// </summary>

	static public UIPanel CreateUI (Transform trans, bool advanced3D, int layer)
	{
		// Find the existing UI Root
		UIRoot root = (trans != null) ? NGUITools.FindInParents<UIRoot>(trans.gameObject) : null;
		if (root == null && UIRoot.list.Count > 0)
			root = UIRoot.list[0];

		// If no root found, create one
		if (root == null)
		{
			GameObject go = NGUITools.AddChild(null, false);
			root = go.AddComponent<UIRoot>();

			// Automatically find the layers if none were specified
			if (layer == -1) layer = LayerMask.NameToLayer("UI");
			if (layer == -1) layer = LayerMask.NameToLayer("2D UI");
			go.layer = layer;

			if (advanced3D)
			{
				go.name = "UI Root (3D)";
				root.scalingStyle = UIRoot.Scaling.FixedSize;
			}
			else
			{
				go.name = "UI Root";
				root.scalingStyle = UIRoot.Scaling.PixelPerfect;
			}
		}

		// Find the first panel
		UIPanel panel = root.GetComponentInChildren<UIPanel>();

		if (panel == null)
		{
			// Find other active cameras in the scene
			Camera[] cameras = NGUITools.FindActive<Camera>();

			float depth = -1f;
			bool colorCleared = false;
			int mask = (1 << root.gameObject.layer);

			for (int i = 0; i < cameras.Length; ++i)
			{
				Camera c = cameras[i];

				// If the color is being cleared, we won't need to
				if (c.clearFlags == CameraClearFlags.Color ||
					c.clearFlags == CameraClearFlags.Skybox)
					colorCleared = true;

				// Choose the maximum depth
				depth = Mathf.Max(depth, c.depth);

				// Make sure this camera can't see the UI
				c.cullingMask = (c.cullingMask & (~mask));
			}

			// Create a camera that will draw the UI
			Camera cam = NGUITools.AddChild<Camera>(root.gameObject, false);
			cam.gameObject.AddComponent<UICamera>();
			cam.clearFlags = colorCleared ? CameraClearFlags.Depth : CameraClearFlags.Color;
			cam.backgroundColor = Color.grey;
			cam.cullingMask = mask;
			cam.depth = depth + 1f;

			if (advanced3D)
			{
				cam.nearClipPlane = 0.1f;
				cam.farClipPlane = 4f;
				cam.transform.localPosition = new Vector3(0f, 0f, -700f);
			}
			else
			{
				cam.orthographic = true;
				cam.orthographicSize = 1;
				cam.nearClipPlane = -10;
				cam.farClipPlane = 10;
			}

			// Make sure there is an audio listener present
			AudioListener[] listeners = NGUITools.FindActive<AudioListener>();
			if (listeners == null || listeners.Length == 0)
				cam.gameObject.AddComponent<AudioListener>();

			// Add a panel to the root
			panel = root.gameObject.AddComponent<UIPanel>();
#if UNITY_EDITOR
			UnityEditor.Selection.activeGameObject = panel.gameObject;
#endif
		}

		if (trans != null)
		{
			// Find the root object
			while (trans.parent != null) trans = trans.parent;

			if (NGUITools.IsChild(trans, panel.transform))
			{
				// Odd hierarchy -- can't reparent
				panel = trans.gameObject.AddComponent<UIPanel>();
			}
			else
			{
				// Reparent this root object to be a child of the panel
				trans.parent = panel.transform;
				trans.localScale = Vector3.one;
				trans.localPosition = Vector3.zero;
				SetChildLayer(panel.cachedTransform, panel.cachedGameObject.layer);
			}
		}
		return panel;
	}

	/// <summary>
	/// Helper function that recursively sets all children with widgets' game objects layers to the specified value.
	/// </summary>

	static public void SetChildLayer (Transform t, int layer)
	{
		for (int i = 0; i < t.childCount; ++i)
		{
			Transform child = t.GetChild(i);
			child.gameObject.layer = layer;
			SetChildLayer(child, layer);
		}
	}

	/// <summary>
	/// Add a child object to the specified parent and attaches the specified script to it.
	/// </summary>

	static public T AddChild<T> (GameObject parent) where T : Component
	{
		GameObject go = AddChild(parent);
		go.name = GetTypeName<T>();
		return go.AddComponent<T>();
	}

	/// <summary>
	/// Add a child object to the specified parent and attaches the specified script to it.
	/// </summary>

	static public T AddChild<T> (GameObject parent, bool undo) where T : Component
	{
		GameObject go = AddChild(parent, undo);
		go.name = GetTypeName<T>();
		return go.AddComponent<T>();
	}

	/// <summary>
	/// Add a new widget of specified type.
	/// </summary>

	static public T AddWidget<T> (GameObject go) where T : UIWidget
	{
		int depth = CalculateNextDepth(go);

		// Create the widget and place it above other widgets
		T widget = AddChild<T>(go);
		widget.width = 100;
		widget.height = 100;
		widget.depth = depth;
		widget.gameObject.layer = go.layer;
		return widget;
	}

	/// <summary>
	/// Add a sprite appropriate for the specified atlas sprite.
	/// It will be sliced if the sprite has an inner rect, and a regular sprite otherwise.
	/// </summary>

	static public UISprite AddSprite (GameObject go, UIAtlas atlas, string spriteName)
	{
		UISpriteData sp = (atlas != null) ? atlas.GetSprite(spriteName) : null;
		UISprite sprite = AddWidget<UISprite>(go);
		sprite.type = (sp == null || !sp.hasBorder) ? UISprite.Type.Simple : UISprite.Type.Sliced;
		sprite.atlas = atlas;
		sprite.spriteName = spriteName;
		return sprite;
	}

	/// <summary>
	/// Get the rootmost object of the specified game object.
	/// </summary>

	static public GameObject GetRoot (GameObject go)
	{
		Transform t = go.transform;

		for (; ; )
		{
			Transform parent = t.parent;
			if (parent == null) break;
			t = parent;
		}
		return t.gameObject;
	}

	/// <summary>
	/// Finds the specified component on the game object or one of its parents.
	/// </summary>

	static public T FindInParents<T> (GameObject go) where T : Component
	{
		if (go == null) return null;
#if UNITY_FLASH
		object comp = go.GetComponent<T>();
#else
		T comp = go.GetComponent<T>();
#endif
		if (comp == null)
		{
			Transform t = go.transform.parent;

			while (t != null && comp == null)
			{
				comp = t.gameObject.GetComponent<T>();
				t = t.parent;
			}
		}
#if UNITY_FLASH
		return (T)comp;
#else
		return comp;
#endif
	}

	/// <summary>
	/// Finds the specified component on the game object or one of its parents.
	/// </summary>

	static public T FindInParents<T> (Transform trans) where T : Component
	{
		if (trans == null) return null;
#if UNITY_FLASH
		object comp = trans.GetComponent<T>();
#else
		T comp = trans.GetComponent<T>();
#endif
		if (comp == null)
		{
			Transform t = trans.transform.parent;

			while (t != null && comp == null)
			{
				comp = t.gameObject.GetComponent<T>();
				t = t.parent;
			}
		}
#if UNITY_FLASH
		return (T)comp;
#else
		return comp;
#endif
	}

	/// <summary>
	/// Destroy the specified object, immediately if in edit mode.
	/// </summary>

	static public void Destroy (UnityEngine.Object obj)
	{
		if (obj != null)
		{
			if (Application.isPlaying)
			{
				if (obj is GameObject)
				{
					GameObject go = obj as GameObject;
					go.transform.parent = null;
				}

				UnityEngine.Object.Destroy(obj);
			}
			else UnityEngine.Object.DestroyImmediate(obj);
		}
	}

	/// <summary>
	/// Destroy the specified object immediately, unless not in the editor, in which case the regular Destroy is used instead.
	/// </summary>

	static public void DestroyImmediate (UnityEngine.Object obj)
	{
		if (obj != null)
		{
			if (Application.isEditor) UnityEngine.Object.DestroyImmediate(obj);
			else UnityEngine.Object.Destroy(obj);
		}
	}

	/// <summary>
	/// Call the specified function on all objects in the scene.
	/// </summary>

	static public void Broadcast (string funcName)
	{
		GameObject[] gos = GameObject.FindObjectsOfType(typeof(GameObject)) as GameObject[];
		for (int i = 0, imax = gos.Length; i < imax; ++i) gos[i].SendMessage(funcName, SendMessageOptions.DontRequireReceiver);
	}

	/// <summary>
	/// Call the specified function on all objects in the scene.
	/// </summary>

	static public void Broadcast (string funcName, object param)
	{
		GameObject[] gos = GameObject.FindObjectsOfType(typeof(GameObject)) as GameObject[];
		for (int i = 0, imax = gos.Length; i < imax; ++i) gos[i].SendMessage(funcName, param, SendMessageOptions.DontRequireReceiver);
	}

	/// <summary>
	/// Determines whether the 'parent' contains a 'child' in its hierarchy.
	/// </summary>

	static public bool IsChild (Transform parent, Transform child)
	{
		if (parent == null || child == null) return false;

		while (child != null)
		{
			if (child == parent) return true;
			child = child.parent;
		}
		return false;
	}

	/// <summary>
	/// Activate the specified object and all of its children.
	/// </summary>

	static void Activate (Transform t) { Activate(t, true); }

	/// <summary>
	/// Activate the specified object and all of its children.
	/// </summary>

	static void Activate (Transform t, bool compatibilityMode)
	{
		SetActiveSelf(t.gameObject, true);

		// Prior to Unity 4, active state was not nested. It was possible to have an enabled child of a disabled object.
		// Unity 4 onwards made it so that the state is nested, and a disabled parent results in a disabled child.
#if UNITY_3_5
		for (int i = 0, imax = t.GetChildCount(); i < imax; ++i)
		{
			Transform child = t.GetChild(i);
			Activate(child);
		}
#else
		if (compatibilityMode)
		{
			// If there is even a single enabled child, then we're using a Unity 4.0-based nested active state scheme.
			for (int i = 0, imax = t.childCount; i < imax; ++i)
			{
				Transform child = t.GetChild(i);
				if (child.gameObject.activeSelf) return;
			}

			// If this point is reached, then all the children are disabled, so we must be using a Unity 3.5-based active state scheme.
			for (int i = 0, imax = t.childCount; i < imax; ++i)
			{
				Transform child = t.GetChild(i);
				Activate(child, true);
			}
		}
#endif
	}

	/// <summary>
	/// Deactivate the specified object and all of its children.
	/// </summary>

	static void Deactivate (Transform t)
	{
#if UNITY_3_5
		for (int i = 0, imax = t.GetChildCount(); i < imax; ++i)
		{
			Transform child = t.GetChild(i);
			Deactivate(child);
		}
#endif
		SetActiveSelf(t.gameObject, false);
	}

	/// <summary>
	/// SetActiveRecursively enables children before parents. This is a problem when a widget gets re-enabled
	/// and it tries to find a panel on its parent.
	/// </summary>

	static public void SetActive (GameObject go, bool state) { SetActive(go, state, true); }

	/// <summary>
	/// SetActiveRecursively enables children before parents. This is a problem when a widget gets re-enabled
	/// and it tries to find a panel on its parent.
	/// </summary>

	static public void SetActive (GameObject go, bool state, bool compatibilityMode)
	{
		if (go)
		{
			if (state)
			{
				Activate(go.transform, compatibilityMode);
#if UNITY_EDITOR
				if (Application.isPlaying)
#endif
					CallCreatePanel(go.transform);
			}
			else Deactivate(go.transform);
		}
	}

	/// <summary>
	/// Ensure that all widgets have had their panels created, forcing the update right away rather than on the following frame.
	/// </summary>

	[System.Diagnostics.DebuggerHidden]
	[System.Diagnostics.DebuggerStepThrough]
	static void CallCreatePanel (Transform t)
	{
		UIWidget w = t.GetComponent<UIWidget>();
		if (w != null) w.CreatePanel();
		for (int i = 0, imax = t.childCount; i < imax; ++i)
			CallCreatePanel(t.GetChild(i));
	}

	/// <summary>
	/// Activate or deactivate children of the specified game object without changing the active state of the object itself.
	/// </summary>

	static public void SetActiveChildren (GameObject go, bool state)
	{
		Transform t = go.transform;

		if (state)
		{
			for (int i = 0, imax = t.childCount; i < imax; ++i)
			{
				Transform child = t.GetChild(i);
				Activate(child);
			}
		}
		else
		{
			for (int i = 0, imax = t.childCount; i < imax; ++i)
			{
				Transform child = t.GetChild(i);
				Deactivate(child);
			}
		}
	}

	/// <summary>
	/// Helper function that returns whether the specified MonoBehaviour is active.
	/// </summary>

	[System.Obsolete("Use NGUITools.GetActive instead")]
	static public bool IsActive (Behaviour mb)
	{
#if UNITY_3_5
		return mb != null && mb.enabled && mb.gameObject.active;
#else
		return mb != null && mb.enabled && mb.gameObject.activeInHierarchy;
#endif
	}

	/// <summary>
	/// Helper function that returns whether the specified MonoBehaviour is active.
	/// </summary>

	[System.Diagnostics.DebuggerHidden]
	[System.Diagnostics.DebuggerStepThrough]
	static public bool GetActive (Behaviour mb)
	{
#if UNITY_3_5
		return mb && mb.enabled && mb.gameObject.active;
#else
		return mb && mb.enabled && mb.gameObject.activeInHierarchy;
#endif
	}

	/// <summary>
	/// Unity4 has changed GameObject.active to GameObject.activeself.
	/// </summary>

	[System.Diagnostics.DebuggerHidden]
	[System.Diagnostics.DebuggerStepThrough]
	static public bool GetActive (GameObject go)
	{
#if UNITY_3_5
		return go && go.active;
#else
		return go && go.activeInHierarchy;
#endif
	}

	/// <summary>
	/// Unity4 has changed GameObject.active to GameObject.SetActive.
	/// </summary>

	[System.Diagnostics.DebuggerHidden]
	[System.Diagnostics.DebuggerStepThrough]
	static public void SetActiveSelf (GameObject go, bool state)
	{
#if UNITY_3_5
		go.active = state;
#else
		go.SetActive(state);
#endif
	}

	/// <summary>
	/// Recursively set the game object's layer.
	/// </summary>

	static public void SetLayer (GameObject go, int layer)
	{
		go.layer = layer;

		Transform t = go.transform;
		
		for (int i = 0, imax = t.childCount; i < imax; ++i)
		{
			Transform child = t.GetChild(i);
			SetLayer(child.gameObject, layer);
		}
	}

	/// <summary>
	/// Helper function used to make the vector use integer numbers.
	/// </summary>

	static public Vector3 Round (Vector3 v)
	{
		v.x = Mathf.Round(v.x);
		v.y = Mathf.Round(v.y);
		v.z = Mathf.Round(v.z);
		return v;
	}

	/// <summary>
	/// Make the specified selection pixel-perfect.
	/// </summary>

	static public void MakePixelPerfect (Transform t)
	{
		UIWidget w = t.GetComponent<UIWidget>();
		if (w != null) w.MakePixelPerfect();

		if (t.GetComponent<UIAnchor>() == null && t.GetComponent<UIRoot>() == null)
		{
#if UNITY_EDITOR
			RegisterUndo(t, "Make Pixel-Perfect");
#endif
			t.localPosition = Round(t.localPosition);
			t.localScale = Round(t.localScale);
		}

		// Recurse into children
		for (int i = 0, imax = t.childCount; i < imax; ++i)
			MakePixelPerfect(t.GetChild(i));
	}

	/// <summary>
	/// Save the specified binary data into the specified file.
	/// </summary>

	static public bool Save (string fileName, byte[] bytes)
	{
#if UNITY_WEBPLAYER || UNITY_FLASH || UNITY_METRO || UNITY_WP8
		return false;
#else
		if (!NGUITools.fileAccess) return false;

		string path = Application.persistentDataPath + "/" + fileName;

		if (bytes == null)
		{
			if (File.Exists(path)) File.Delete(path);
			return true;
		}

		FileStream file = null;

		try
		{
			file = File.Create(path);
		}
		catch (System.Exception ex)
		{
			Debug.LogError(ex.Message);
			return false;
		}

		file.Write(bytes, 0, bytes.Length);
		file.Close();
		return true;
#endif
	}

	/// <summary>
	/// Load all binary data from the specified file.
	/// </summary>

	static public byte[] Load (string fileName)
	{
#if UNITY_WEBPLAYER || UNITY_FLASH || UNITY_METRO || UNITY_WP8
		return null;
#else
		if (!NGUITools.fileAccess) return null;

		string path = Application.persistentDataPath + "/" + fileName;

		if (File.Exists(path))
		{
			return File.ReadAllBytes(path);
		}
		return null;
#endif
	}

	/// <summary>
	/// Pre-multiply shaders result in a black outline if this operation is done in the shader. It's better to do it outside.
	/// </summary>

	static public Color ApplyPMA (Color c)
	{
		if (c.a != 1f)
		{
			c.r *= c.a;
			c.g *= c.a;
			c.b *= c.a;
		}
		return c;
	}

	/// <summary>
	/// Inform all widgets underneath the specified object that the parent has changed.
	/// </summary>

	static public void MarkParentAsChanged (GameObject go)
	{
		UIRect[] rects = go.GetComponentsInChildren<UIRect>();
		for (int i = 0, imax = rects.Length; i < imax; ++i)
			rects[i].ParentHasChanged();
	}

	/// <summary>
	/// Access to the clipboard via undocumented APIs.
	/// </summary>

	static public string clipboard
	{
		get
		{
			TextEditor te = new TextEditor();
			te.Paste();
			return te.content.text;
		}
		set
		{
			TextEditor te = new TextEditor();
			te.content = new GUIContent(value);
			te.OnFocus();
			te.Copy();
		}
	}

	[System.Obsolete("Use NGUIText.EncodeColor instead")]
	static public string EncodeColor (Color c) { return NGUIText.EncodeColor24(c); }

	[System.Obsolete("Use NGUIText.ParseColor instead")]
	static public Color ParseColor (string text, int offset) { return NGUIText.ParseColor24(text, offset); }

	[System.Obsolete("Use NGUIText.StripSymbols instead")]
	static public string StripSymbols (string text) { return NGUIText.StripSymbols(text); }

	/// <summary>
	/// Extension for the game object that checks to see if the component already exists before adding a new one.
	/// If the component is already present it will be returned instead.
	/// </summary>

	static public T AddMissingComponent<T> (this GameObject go) where T : Component
	{
		T comp = go.GetComponent<T>();
		if (comp == null)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
				RegisterUndo(go, "Add " + typeof(T));
#endif
			comp = go.AddComponent<T>();
		}
		return comp;
	}

	// Temporary variable to avoid GC allocation
	static Vector3[] mSides = new Vector3[4];

	/// <summary>
	/// Get sides relative to the specified camera. The order is left, top, right, bottom.
	/// </summary>

	static public Vector3[] GetSides (this Camera cam)
	{
		return cam.GetSides(Mathf.Lerp(cam.nearClipPlane, cam.farClipPlane, 0.5f), null);
	}

	/// <summary>
	/// Get sides relative to the specified camera. The order is left, top, right, bottom.
	/// </summary>

	static public Vector3[] GetSides (this Camera cam, float depth)
	{
		return cam.GetSides(depth, null);
	}

	/// <summary>
	/// Get sides relative to the specified camera. The order is left, top, right, bottom.
	/// </summary>

	static public Vector3[] GetSides (this Camera cam, Transform relativeTo)
	{
		return cam.GetSides(Mathf.Lerp(cam.nearClipPlane, cam.farClipPlane, 0.5f), relativeTo);
	}

	/// <summary>
	/// Get sides relative to the specified camera. The order is left, top, right, bottom.
	/// </summary>

	static public Vector3[] GetSides (this Camera cam, float depth, Transform relativeTo)
	{
		mSides[0] = cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, depth));
		mSides[1] = cam.ViewportToWorldPoint(new Vector3(0.5f, 1f, depth));
		mSides[2] = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, depth));
		mSides[3] = cam.ViewportToWorldPoint(new Vector3(0.5f, 0f, depth));

		if (relativeTo != null)
		{
			for (int i = 0; i < 4; ++i)
				mSides[i] = relativeTo.InverseTransformPoint(mSides[i]);
		}
		return mSides;
	}

	/// <summary>
	/// Get the camera's world-space corners. The order is bottom-left, top-left, top-right, bottom-right.
	/// </summary>

	static public Vector3[] GetWorldCorners (this Camera cam)
	{
		return cam.GetWorldCorners(Mathf.Lerp(cam.nearClipPlane, cam.farClipPlane, 0.5f), null);
	}

	/// <summary>
	/// Get the camera's world-space corners. The order is bottom-left, top-left, top-right, bottom-right.
	/// </summary>

	static public Vector3[] GetWorldCorners (this Camera cam, float depth)
	{
		return cam.GetWorldCorners(depth, null);
	}

	/// <summary>
	/// Get the camera's world-space corners. The order is bottom-left, top-left, top-right, bottom-right.
	/// </summary>

	static public Vector3[] GetWorldCorners (this Camera cam, Transform relativeTo)
	{
		return cam.GetWorldCorners(Mathf.Lerp(cam.nearClipPlane, cam.farClipPlane, 0.5f), relativeTo);
	}

	/// <summary>
	/// Get the camera's world-space corners. The order is bottom-left, top-left, top-right, bottom-right.
	/// </summary>

	static public Vector3[] GetWorldCorners (this Camera cam, float depth, Transform relativeTo)
	{
		mSides[0] = cam.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
		mSides[1] = cam.ViewportToWorldPoint(new Vector3(0f, 1f, depth));
		mSides[2] = cam.ViewportToWorldPoint(new Vector3(1f, 1f, depth));
		mSides[3] = cam.ViewportToWorldPoint(new Vector3(1f, 0f, depth));

		if (relativeTo != null)
		{
			for (int i = 0; i < 4; ++i)
				mSides[i] = relativeTo.InverseTransformPoint(mSides[i]);
		}
		return mSides;
	}

	/// <summary>
	/// Convenience function that converts Class + Function combo into Class.Function representation.
	/// </summary>

	static public string GetFuncName (object obj, string method)
	{
		if (obj == null) return "<null>";
		string type = obj.GetType().ToString();
		int period = type.LastIndexOf('.');
		if (period > 0) type = type.Substring(period + 1);
		return string.IsNullOrEmpty(method) ? type : type + "." + method;
	}
}

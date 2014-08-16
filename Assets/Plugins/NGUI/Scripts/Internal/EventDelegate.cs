//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

#if UNITY_EDITOR || !UNITY_FLASH
#define REFLECTION_SUPPORT
#endif

#if REFLECTION_SUPPORT
using System.Reflection;
#endif

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Delegate callback that Unity can serialize and set via Inspector.
/// </summary>

[System.Serializable]
public class EventDelegate
{
	/// <summary>
	/// Delegates can have parameters, and this class makes it possible to save references to properties
	/// that can then be passed as function arguments, such as transform.position or widget.color.
	/// </summary>

	[System.Serializable]
	public class Parameter
	{
		public Object obj;
		public string field;

		[System.NonSerialized]
		public System.Type expectedType = typeof(void);

		public Parameter () { }
		public Parameter (Object obj, string field) { this.obj = obj; this.field = field; }

#if REFLECTION_SUPPORT
		// Cached values
		[System.NonSerialized] public bool cached = false;
		[System.NonSerialized] public PropertyInfo propInfo;
		[System.NonSerialized] public FieldInfo fieldInfo;

		/// <summary>
		/// Return the property's current value.
		/// </summary>

		public object value
		{
			get
			{
				if (!cached)
				{
					cached = true;
					fieldInfo = null;
					propInfo = null;

					if (obj != null && !string.IsNullOrEmpty(field))
					{
						System.Type type = obj.GetType();
#if NETFX_CORE
						propInfo = type.GetRuntimeProperty(field);
						if (propInfo == null) fieldInfo = type.GetRuntimeField(field);
#else
						propInfo = type.GetProperty(field);
						if (propInfo == null) fieldInfo = type.GetField(field);
#endif
					}
				}
				if (propInfo != null) return propInfo.GetValue(obj, null);
				if (fieldInfo != null) return fieldInfo.GetValue(obj);
				return obj;
			}
		}

		/// <summary>
		/// Parameter type -- a convenience function.
		/// </summary>

		public System.Type type
		{
			get
			{
				if (obj == null) return typeof(void);
				return obj.GetType();
			}
		}
#else
		public object value { get { return obj; } }
		public System.Type type { get { return typeof(void); } }
#endif
	}

	[SerializeField] MonoBehaviour mTarget;
	[SerializeField] string mMethodName;
	[SerializeField] Parameter[] mParameters;

	/// <summary>
	/// Whether the event delegate will be removed after execution.
	/// </summary>

	public bool oneShot = false;

	// Private variables
	public delegate void Callback();
	[System.NonSerialized] Callback mCachedCallback;
	[System.NonSerialized] bool mRawDelegate = false;
	[System.NonSerialized] bool mCached = false;
#if REFLECTION_SUPPORT
	[System.NonSerialized] MethodInfo mMethod;
	[System.NonSerialized] object[] mArgs;
#endif

	/// <summary>
	/// Event delegate's target object.
	/// </summary>

	public MonoBehaviour target
	{
		get
		{
			return mTarget;
		}
		set
		{
			mTarget = value;
			mCachedCallback = null;
			mRawDelegate = false;
			mCached = false;
#if REFLECTION_SUPPORT
			mMethod = null;
#endif
			mParameters = null;
		}
	}

	/// <summary>
	/// Event delegate's method name.
	/// </summary>

	public string methodName
	{
		get
		{
			return mMethodName;
		}
		set
		{
			mMethodName = value;
			mCachedCallback = null;
			mRawDelegate = false;
			mCached = false;
#if REFLECTION_SUPPORT
			mMethod = null;
#endif
			mParameters = null;
		}
	}

	/// <summary>
	/// Optional parameters if the method requires them.
	/// </summary>

	public Parameter[] parameters
	{
		get
		{
#if UNITY_EDITOR
			if (!mCached || !Application.isPlaying) Cache();
#else
			if (!mCached) Cache();
#endif
			return mParameters;
		}
	}

	/// <summary>
	/// Whether this delegate's values have been set.
	/// </summary>

	public bool isValid
	{
		get
		{
#if UNITY_EDITOR
			if (!mCached || !Application.isPlaying) Cache();
#else
			if (!mCached) Cache();
#endif
			return (mRawDelegate && mCachedCallback != null) || (mTarget != null && !string.IsNullOrEmpty(mMethodName));
		}
	}

	/// <summary>
	/// Whether the target script is actually enabled.
	/// </summary>

	public bool isEnabled
	{
		get
		{
#if UNITY_EDITOR
			if (!mCached || !Application.isPlaying) Cache();
#else
			if (!mCached) Cache();
#endif
			if (mRawDelegate && mCachedCallback != null) return true;
			if (mTarget == null) return false;
			MonoBehaviour mb = (mTarget as MonoBehaviour);
			return (mb == null || mb.enabled);
		}
	}

	public EventDelegate () { }
	public EventDelegate (Callback call) { Set(call); }
	public EventDelegate (MonoBehaviour target, string methodName) { Set(target, methodName); }

	/// <summary>
	/// GetMethodName is not supported on some platforms.
	/// </summary>

#if REFLECTION_SUPPORT
 #if !UNITY_EDITOR && NETFX_CORE
	static string GetMethodName (Callback callback)
	{
		System.Delegate d = callback as System.Delegate;
		return d.GetMethodInfo().Name;
	}

	static bool IsValid (Callback callback)
	{
		System.Delegate d = callback as System.Delegate;
		return d != null && d.GetMethodInfo() != null;
	}
 #else
	static string GetMethodName (Callback callback) { return callback.Method.Name; }
	static bool IsValid (Callback callback) { return callback != null && callback.Method != null; }
 #endif
#else
	static bool IsValid (Callback callback) { return callback != null; }
#endif

	/// <summary>
	/// Equality operator.
	/// </summary>

	public override bool Equals (object obj)
	{
		if (obj == null) return !isValid;

		if (obj is Callback)
		{
			Callback callback = obj as Callback;
#if REFLECTION_SUPPORT
			if (callback.Equals(mCachedCallback)) return true;
			MonoBehaviour mb = callback.Target as MonoBehaviour;
			return (mTarget == mb && string.Equals(mMethodName, GetMethodName(callback)));
#elif UNITY_FLASH
			return (callback == mCachedCallback);
#else
			return callback.Equals(mCachedCallback);
#endif
		}
		
		if (obj is EventDelegate)
		{
			EventDelegate del = obj as EventDelegate;
			return (mTarget == del.mTarget && string.Equals(mMethodName, del.mMethodName));
		}
		return false;
	}

	static int s_Hash = "EventDelegate".GetHashCode();

	/// <summary>
	/// Used in equality operators.
	/// </summary>

	public override int GetHashCode () { return s_Hash; }

	/// <summary>
	/// Set the delegate callback directly.
	/// </summary>

	void Set (Callback call)
	{
		Clear();

		if (call != null && IsValid(call))
		{
#if REFLECTION_SUPPORT
			mTarget = call.Target as MonoBehaviour;

			if (mTarget == null)
			{
				mRawDelegate = true;
				mCachedCallback = call;
				mMethodName = null;
			}
			else
			{
				mMethodName = GetMethodName(call);
				mRawDelegate = false;
			}
#else
			mRawDelegate = true;
			mCachedCallback = call;
#endif
		}
	}

	/// <summary>
	/// Set the delegate callback using the target and method names.
	/// </summary>

	public void Set (MonoBehaviour target, string methodName)
	{
		Clear();
		mTarget = target;
		mMethodName = methodName;
	}

	/// <summary>
	/// Cache the callback and create the list of the necessary parameters.
	/// </summary>

	void Cache ()
	{
		mCached = true;
		if (mRawDelegate) return;

#if REFLECTION_SUPPORT
		if (mCachedCallback == null || (mCachedCallback.Target as MonoBehaviour) != mTarget || GetMethodName(mCachedCallback) != mMethodName)
		{
			if (mTarget != null && !string.IsNullOrEmpty(mMethodName))
			{
				System.Type type = mTarget.GetType();

				try
				{
#if NETFX_CORE
					IEnumerable<MethodInfo> methods = type.GetRuntimeMethods();

					foreach (MethodInfo mi in methods)
					{
						if (mi.Name == mMethodName)
						{
							mMethod = mi;
							break;
						}
					}
#else
					for (mMethod = null; ; )
					{
#if UNITY_WP8
						mMethod = type.GetMethod(mMethodName);
#else
						mMethod = type.GetMethod(mMethodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
#endif
						if (mMethod != null) break;
						type = type.BaseType;
						if (type == null) break;
					}
#endif
				}
				catch (System.Exception ex)
				{
					Debug.LogError("Failed to bind " + type + "." + mMethodName + "\n" +  ex.Message);
					return;
				}

				if (mMethod == null)
				{
					Debug.LogError("Could not find method '" + mMethodName + "' on " + mTarget.GetType(), mTarget);
					return;
				}
				
				if (mMethod.ReturnType != typeof(void))
				{
					Debug.LogError(mTarget.GetType() + "." + mMethodName + " must have a 'void' return type.", mTarget);
					return;
				}

				// Get the list of expected parameters
				ParameterInfo[] info = mMethod.GetParameters();

				if (info.Length == 0)
				{
					// No parameters means we can create a simple delegate for it, optimizing the call
#if NETFX_CORE
					mCachedCallback = (Callback)mMethod.CreateDelegate(typeof(Callback), mTarget);
#else
					mCachedCallback = (Callback)System.Delegate.CreateDelegate(typeof(Callback), mTarget, mMethodName);
#endif

					mArgs = null;
					mParameters = null;
					return;
				}
				else mCachedCallback = null;

				// Allocate the initial list of parameters
				if (mParameters == null || mParameters.Length != info.Length)
				{
					mParameters = new Parameter[info.Length];
					for (int i = 0, imax = mParameters.Length; i < imax; ++i)
						mParameters[i] = new Parameter();
				}

				// Save the parameter type
				for (int i = 0, imax = mParameters.Length; i < imax; ++i)
					mParameters[i].expectedType = info[i].ParameterType;
			}
		}
#endif
	}

	/// <summary>
	/// Execute the delegate, if possible.
	/// This will only be used when the application is playing in order to prevent unintentional state changes.
	/// </summary>

	public bool Execute ()
	{
#if !REFLECTION_SUPPORT
		if (isValid)
		{
			if (mRawDelegate) mCachedCallback();
			else mTarget.SendMessage(mMethodName, SendMessageOptions.DontRequireReceiver);
			return true;
		}
#else
#if UNITY_EDITOR
		if (!mCached || !Application.isPlaying) Cache();
#else
		if (!mCached) Cache();
#endif
		if (mCachedCallback != null)
		{
#if !UNITY_EDITOR
			mCachedCallback();
#else
			if (Application.isPlaying)
			{
				mCachedCallback();
			}
			else if (mCachedCallback.Target != null)
			{
				// There must be an [ExecuteInEditMode] flag on the script for us to call the function at edit time
				System.Type type = mCachedCallback.Target.GetType();
				object[] objs = type.GetCustomAttributes(typeof(ExecuteInEditMode), true);
				if (objs != null && objs.Length > 0) mCachedCallback();
			}
#endif
			return true;
		}

		if (mMethod != null)
		{
#if UNITY_EDITOR
			// There must be an [ExecuteInEditMode] flag on the script for us to call the function at edit time
			if (mTarget != null && !Application.isPlaying)
			{
				System.Type type = mTarget.GetType();
				object[] objs = type.GetCustomAttributes(typeof(ExecuteInEditMode), true);
				if (objs == null || objs.Length == 0) return true;
			}
#endif
			int len = (mParameters != null) ? mParameters.Length : 0;

			if (len == 0)
			{
				mMethod.Invoke(mTarget, null);
			}
			else
			{
				// Allocate the parameter array
				if (mArgs == null || mArgs.Length != mParameters.Length)
					mArgs = new object[mParameters.Length];

				// Set all the parameters
				for (int i = 0, imax = mParameters.Length; i < imax; ++i)
					mArgs[i] = mParameters[i].value;

				// Invoke the callback
				try
				{
					mMethod.Invoke(mTarget, mArgs);
				}
				catch (System.ArgumentException ex)
				{
					string msg = "Error calling ";

					if (mTarget == null) msg += mMethod.Name;
					else msg += mTarget.GetType() + "." + mMethod.Name;
					
					msg += ": " + ex.Message;
					msg += "\n  Expected: ";

					ParameterInfo[] pis = mMethod.GetParameters();

					if (pis.Length == 0)
					{
						msg += "no arguments";
					}
					else
					{
						msg += pis[0];
						for (int i = 1; i < pis.Length; ++i)
							msg += ", " + pis[i].ParameterType;
					}

					msg += "\n  Received: ";

					if (mParameters.Length == 0)
					{
						msg += "no arguments";
					}
					else
					{
						msg += mParameters[0].type;
						for (int i = 1; i < mParameters.Length; ++i)
							msg += ", " + mParameters[i].type;
					}
					msg += "\n";
					Debug.LogError(msg);
				}

				// Clear the parameters so that references are not kept
				for (int i = 0, imax = mArgs.Length; i < imax;  ++i) mArgs[i] = null;
			}
			return true;
		}
#endif
		return false;
	}

	/// <summary>
	/// Clear the event delegate.
	/// </summary>

	public void Clear ()
	{
		mTarget = null;
		mMethodName = null;
		mRawDelegate = false;
		mCachedCallback = null;
		mParameters = null;
		mCached = false;
#if REFLECTION_SUPPORT
		mMethod = null;
		mArgs = null;
#endif
	}

	/// <summary>
	/// Convert the delegate to its string representation.
	/// </summary>

	public override string ToString ()
	{
		if (mTarget != null)
		{
			string typeName = mTarget.GetType().ToString();
			int period = typeName.LastIndexOf('.');
			if (period > 0) typeName = typeName.Substring(period + 1);

			if (!string.IsNullOrEmpty(methodName)) return typeName + "." + methodName;
			else return typeName + ".[delegate]";
		}
		return mRawDelegate ? "[delegate]" : null;
	}

	/// <summary>
	/// Execute an entire list of delegates.
	/// </summary>

	static public void Execute (List<EventDelegate> list)
	{
		if (list != null)
		{
			for (int i = 0; i < list.Count; )
			{
				EventDelegate del = list[i];

				if (del != null)
				{
					del.Execute();

					if (i >= list.Count) break;
					if (list[i] != del) continue;

					if (del.oneShot)
					{
						list.RemoveAt(i);
						continue;
					}
				}
				++i;
			}
		}
	}

	/// <summary>
	/// Convenience function to check if the specified list of delegates can be executed.
	/// </summary>

	static public bool IsValid (List<EventDelegate> list)
	{
		if (list != null)
		{
			for (int i = 0, imax = list.Count; i < imax; ++i)
			{
				EventDelegate del = list[i];
				if (del != null && del.isValid)
					return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Assign a new event delegate.
	/// </summary>

	static public void Set (List<EventDelegate> list, Callback callback)
	{
		if (list != null)
		{
			list.Clear();
			list.Add(new EventDelegate(callback));
		}
	}

	/// <summary>
	/// Assign a new event delegate.
	/// </summary>

	static public void Set (List<EventDelegate> list, EventDelegate del)
	{
		if (list != null)
		{
			list.Clear();
			list.Add(del);
		}
	}

	/// <summary>
	/// Append a new event delegate to the list.
	/// </summary>

	static public void Add (List<EventDelegate> list, Callback callback) { Add(list, callback, false); }

	/// <summary>
	/// Append a new event delegate to the list.
	/// </summary>

	static public void Add (List<EventDelegate> list, Callback callback, bool oneShot)
	{
		if (list != null)
		{
			for (int i = 0, imax = list.Count; i < imax; ++i)
			{
				EventDelegate del = list[i];
				if (del != null && del.Equals(callback))
					return;
			}

			EventDelegate ed = new EventDelegate(callback);
			ed.oneShot = oneShot;
			list.Add(ed);
		}
		else
		{
			Debug.LogWarning("Attempting to add a callback to a list that's null");
		}
	}

	/// <summary>
	/// Append a new event delegate to the list.
	/// </summary>

	static public void Add (List<EventDelegate> list, EventDelegate ev) { Add(list, ev, ev.oneShot); }

	/// <summary>
	/// Append a new event delegate to the list.
	/// </summary>

	static public void Add (List<EventDelegate> list, EventDelegate ev, bool oneShot)
	{
		if (ev.mRawDelegate || ev.target == null || string.IsNullOrEmpty(ev.methodName))
		{
			Add(list, ev.mCachedCallback, oneShot);
		}
		else if (list != null)
		{
			for (int i = 0, imax = list.Count; i < imax; ++i)
			{
				EventDelegate del = list[i];
				if (del != null && del.Equals(ev))
					return;
			}
			
			EventDelegate copy = new EventDelegate(ev.target, ev.methodName);
			copy.oneShot = oneShot;

			if (ev.mParameters != null && ev.mParameters.Length > 0)
			{
				copy.mParameters = new Parameter[ev.mParameters.Length];
				for (int i = 0; i < ev.mParameters.Length; ++i)
					copy.mParameters[i] = ev.mParameters[i];
			}

			list.Add(copy);
		}
		else Debug.LogWarning("Attempting to add a callback to a list that's null");
	}

	/// <summary>
	/// Remove an existing event delegate from the list.
	/// </summary>

	static public bool Remove (List<EventDelegate> list, Callback callback)
	{
		if (list != null)
		{
			for (int i = 0, imax = list.Count; i < imax; ++i)
			{
				EventDelegate del = list[i];
				
				if (del != null && del.Equals(callback))
				{
					list.RemoveAt(i);
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>
	/// Remove an existing event delegate from the list.
	/// </summary>

	static public bool Remove (List<EventDelegate> list, EventDelegate ev)
	{
		if (list != null)
		{
			for (int i = 0, imax = list.Count; i < imax; ++i)
			{
				EventDelegate del = list[i];

				if (del != null && del.Equals(ev))
				{
					list.RemoveAt(i);
					return true;
				}
			}
		}
		return false;
	}
}

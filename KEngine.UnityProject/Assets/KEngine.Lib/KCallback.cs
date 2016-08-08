using UnityEngine;
using System.Collections;

/// <summary>
/// KEngine standard callback, like nodejs 's standard callback
/// </summary>
/// <param name="args"></param>
public delegate void KCallback(object[] data, string err = null);

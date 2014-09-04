//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
//
//                         version 0.8
//                     Copyright © 2011-2014
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------
using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// This class offers the ability to save the fields
/// of types that don't have the <c ref="System.SerializableAttribute">SerializableAttribute</c>.
/// </summary>

public class NonSerialiazableTypeSurrogateSelector : System.Runtime.Serialization.ISerializationSurrogate, System.Runtime.Serialization.ISurrogateSelector
{
	#region ISerializationSurrogate Members

	public void GetObjectData(object obj, System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
	{
		FieldInfo[] fieldInfos = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (var fi in fieldInfos)
		{
			if (IsKnownType(fi.FieldType)
					)
			{
				info.AddValue(fi.Name, fi.GetValue(obj));
			}
			/*
			else if(fi.FieldType == typeof(Texture2D))
			{
				Texture2D tex = fi.GetValue(obj) as Texture2D;
				Debug.Log(tex.name);
				info.AddValue(fi.Name, tex.name);
			}
			*/
			else
				if (fi.FieldType.IsClass)
				{
					info.AddValue(fi.Name, fi.GetValue(obj));
				}
		}

	}

	private bool IsKnownType(Type type)
	{
		return
			type == typeof(string)
			|| type.IsPrimitive
			|| type.IsSerializable
				;
	}

	public object SetObjectData(object obj, System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context, System.Runtime.Serialization.ISurrogateSelector selector)
	{
		FieldInfo[] fieldInfos = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (var fi in fieldInfos)
		{
			if (IsKnownType(fi.FieldType))
			{
				//var value = info.GetValue(fi.Name, fi.FieldType);

				if (IsNullableType(fi.FieldType))
				{
					// Nullable<argumentValue>
					Type argumentValueForTheNullableType = GetFirstArgumentOfGenericType(fi.FieldType);//fi.FieldType.GetGenericArguments()[0];
					fi.SetValue(obj, info.GetValue(fi.Name, argumentValueForTheNullableType));
				}
				else
				{
					fi.SetValue(obj, info.GetValue(fi.Name, fi.FieldType));
				}

			}
			/*
			else if(fi.FieldType == typeof(Texture2D))
			{
				string texname = info.GetValue(fi.Name, typeof(string)) as string;
				Texture2D tex = Resources.Load("Social/"+texname) as Texture2D;
				Debug.Log(tex.name);

				fi.SetValue(obj, tex);
			}
			*/
			else
				if (fi.FieldType.IsClass)
				{
					fi.SetValue(obj, info.GetValue(fi.Name, fi.FieldType));
				}
		}

		return obj;
	}
	private Type GetFirstArgumentOfGenericType(Type type)
	{
		return type.GetGenericArguments()[0];
	}
	private bool IsNullableType(Type type)
	{
		if (type.IsGenericType)
			return type.GetGenericTypeDefinition() == typeof(Nullable<>);
		return false;
	}

	#endregion

	#region ISurrogateSelector Members
	System.Runtime.Serialization.ISurrogateSelector _nextSelector;
	public void ChainSelector(System.Runtime.Serialization.ISurrogateSelector selector)
	{
		this._nextSelector = selector;
	}

	public System.Runtime.Serialization.ISurrogateSelector GetNextSelector()
	{
		return _nextSelector;
	}

	public System.Runtime.Serialization.ISerializationSurrogate GetSurrogate(Type type, System.Runtime.Serialization.StreamingContext context, out System.Runtime.Serialization.ISurrogateSelector selector)
	{
		if (IsKnownType(type))
		{
			selector = null;
			return null;
		}
		else if (type.IsClass || type.IsValueType)
		{
			selector = this;
			return this;
		}
		else
		{
			selector = null;
			return null;
		}
	}


	#endregion
}
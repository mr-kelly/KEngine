//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

#if !UNITY_3_5 && !UNITY_4_0 && !UNITY_4_1 && !UNITY_4_2

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Inspector class used to edit UITextures.
/// </summary>

[CanEditMultipleObjects]
[CustomEditor(typeof(UI2DSprite), true)]
public class UI2DSpriteEditor : UIBasicSpriteEditor
{
	UI2DSprite mSprite;

	protected override void OnEnable ()
	{
		base.OnEnable();
		mSprite = target as UI2DSprite;
	}

	/// <summary>
	/// Should we draw the widget's custom properties?
	/// </summary>

	protected override bool ShouldDrawProperties ()
	{
		SerializedProperty sp = NGUIEditorTools.DrawProperty("2D Sprite", serializedObject, "mSprite");

		NGUISettings.sprite2D = sp.objectReferenceValue as Sprite;

		NGUIEditorTools.DrawProperty("Material", serializedObject, "mMat");

		if (mSprite.material == null || serializedObject.isEditingMultipleObjects)
		{
			NGUIEditorTools.DrawProperty("Shader", serializedObject, "mShader");
		}
		return (sp.objectReferenceValue != null);
	}

	/// <summary>
	/// Allow the texture to be previewed.
	/// </summary>

	public override bool HasPreviewGUI ()
	{
		return (Selection.activeGameObject == null || Selection.gameObjects.Length == 1) &&
			(mSprite != null) && (mSprite.mainTexture as Texture2D != null);
	}

	/// <summary>
	/// Draw the sprite preview.
	/// </summary>

	public override void OnPreviewGUI (Rect rect, GUIStyle background)
	{
		if (mSprite != null && mSprite.sprite2D != null)
		{
			Texture2D tex = mSprite.mainTexture as Texture2D;
			if (tex != null) NGUIEditorTools.DrawSprite(tex, rect, mSprite.color, mSprite.sprite2D.textureRect, mSprite.border);
		}
	}
}
#endif

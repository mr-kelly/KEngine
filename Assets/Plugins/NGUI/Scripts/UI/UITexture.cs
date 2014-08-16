//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2014 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// If you don't have or don't wish to create an atlas, you can simply use this script to draw a texture.
/// Keep in mind though that this will create an extra draw call with each UITexture present, so it's
/// best to use it only for backgrounds or temporary visible widgets.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/UI/NGUI Texture")]
public class UITexture : UIBasicSprite
{
	[HideInInspector][SerializeField] Rect mRect = new Rect(0f, 0f, 1f, 1f);
	[HideInInspector][SerializeField] Texture mTexture;
	[HideInInspector][SerializeField] Material mMat;
	[HideInInspector][SerializeField] Shader mShader;
	[HideInInspector][SerializeField] Vector4 mBorder = Vector4.zero;

	[System.NonSerialized] int mPMA = -1;

	/// <summary>
	/// Texture used by the UITexture. You can set it directly, without the need to specify a material.
	/// </summary>

	public override Texture mainTexture
	{
		get
		{
			if (mTexture != null) return mTexture;
			if (mMat != null) return mMat.mainTexture;
			return null;
		}
		set
		{
			if (mTexture != value)
			{
				RemoveFromPanel();
				mTexture = value;
				if (value != null) mMat = null;
				mPMA = -1;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Material used by the widget.
	/// </summary>

	public override Material material
	{
		get
		{
			return mMat;
		}
		set
		{
			if (mMat != value)
			{
				RemoveFromPanel();
				mShader = null;
				mMat = value;
				mPMA = -1;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Shader used by the texture when creating a dynamic material (when the texture was specified, but the material was not).
	/// </summary>

	public override Shader shader
	{
		get
		{
			if (mMat != null) return mMat.shader;
			if (mShader == null) mShader = Shader.Find("Unlit/Transparent Colored");
			return mShader;
		}
		set
		{
			if (mShader != value)
			{
				RemoveFromPanel();
				mShader = value;
				mPMA = -1;
				mMat = null;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Whether the texture is using a premultiplied alpha material.
	/// </summary>

	public override bool premultipliedAlpha
	{
		get
		{
			if (mPMA == -1)
			{
				Material mat = material;
				mPMA = (mat != null && mat.shader != null && mat.shader.name.Contains("Premultiplied")) ? 1 : 0;
			}
			return (mPMA == 1);
		}
	}


	/// <summary>
	/// Sprite's border. X = left, Y = bottom, Z = right, W = top.
	/// </summary>

	public override Vector4 border
	{
		get
		{
			return mBorder;
		}
		set
		{
			if (mBorder != value)
			{
				mBorder = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// UV rectangle used by the texture.
	/// </summary>

	public Rect uvRect
	{
		get
		{
			return mRect;
		}
		set
		{
			if (mRect != value)
			{
				mRect = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Widget's dimensions used for drawing. X = left, Y = bottom, Z = right, W = top.
	/// This function automatically adds 1 pixel on the edge if the texture's dimensions are not even.
	/// It's used to achieve pixel-perfect sprites even when an odd dimension widget happens to be centered.
	/// </summary>

	public override Vector4 drawingDimensions
	{
		get
		{
			Vector2 offset = pivotOffset;

			float x0 = -offset.x * mWidth;
			float y0 = -offset.y * mHeight;
			float x1 = x0 + mWidth;
			float y1 = y0 + mHeight;

			if (mTexture != null && mType != UISprite.Type.Tiled)
			{
				int w = mTexture.width;
				int h = mTexture.height;
				int padRight = 0;
				int padTop = 0;

				float px = 1f;
				float py = 1f;

				if (w > 0 && h > 0 && (mType == UISprite.Type.Simple || mType == UISprite.Type.Filled))
				{
					if ((w & 1) != 0) ++padRight;
					if ((h & 1) != 0) ++padTop;

					px = (1f / w) * mWidth;
					py = (1f / h) * mHeight;
				}

				if (mFlip == UISprite.Flip.Horizontally || mFlip == UISprite.Flip.Both)
				{
					x0 += padRight * px;
				}
				else x1 -= padRight * px;

				if (mFlip == UISprite.Flip.Vertically || mFlip == UISprite.Flip.Both)
				{
					y0 += padTop * py;
				}
				else y1 -= padTop * py;
			}

			Vector4 br = border;

			float fw = br.x + br.z;
			float fh = br.y + br.w;
			float vx = Mathf.Lerp(x0, x1 - fw, mDrawRegion.x);
			float vy = Mathf.Lerp(y0, y1 - fh, mDrawRegion.y);
			float vz = Mathf.Lerp(x0 + fw, x1, mDrawRegion.z);
			float vw = Mathf.Lerp(y0 + fh, y1, mDrawRegion.w);

			return new Vector4(vx, vy, vz, vw);
		}
	}

	/// <summary>
	/// Adjust the scale of the widget to make it pixel-perfect.
	/// </summary>

	public override void MakePixelPerfect ()
	{
		base.MakePixelPerfect();
		if (mType == Type.Tiled) return;

		Texture tex = mainTexture;
		if (tex == null) return;

		if (mType == Type.Simple || mType == Type.Filled || !hasBorder)
		{
			if (tex != null)
			{
				int w = tex.width;
				int h = tex.height;

				if ((w & 1) == 1) ++w;
				if ((h & 1) == 1) ++h;

				width = w;
				height = h;
			}
		}
	}

	/// <summary>
	/// Virtual function called by the UIPanel that fills the buffers.
	/// </summary>

	public override void OnFill (BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> cols)
	{
		Texture tex = mainTexture;
		if (tex == null) return;

		Rect outer = new Rect(mRect.x * tex.width, mRect.y * tex.height, tex.width * mRect.width, tex.height * mRect.height);
		Rect inner = outer;
		Vector4 br = border;
		inner.xMin += br.x;
		inner.yMin += br.y;
		inner.xMax -= br.z;
		inner.yMax -= br.w;

		float w = 1f / tex.width;
		float h = 1f / tex.height;

		outer.xMin *= w;
		outer.xMax *= w;
		outer.yMin *= h;
		outer.yMax *= h;

		inner.xMin *= w;
		inner.xMax *= w;
		inner.yMin *= h;
		inner.yMax *= h;

		Fill(verts, uvs, cols, outer, inner);
	}
}

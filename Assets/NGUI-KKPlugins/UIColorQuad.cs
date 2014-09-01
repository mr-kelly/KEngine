#if NGUI
/// 
/// Author: KK
/// Email: 23110388@qq.com
/// 
using UnityEngine;
using System.Collections;

/// <summary>
/// 纯色正方形，使用两个三角形组成，仅4个顶点
/// </summary>
[ExecuteInEditMode]
[AddComponentMenu("NGUI/AC-Plugins/ColorQuad")]
public class UIColorQuad : UIWidget
{
    /// <summary>
    /// 用于纯色矩形渲染的材质, 独立，不共享
    /// </summary>
    private static Material m_UIColorQuadMaterial = null;  // 静态，唯一，共享

    public override Material material
    {
        get { return UIColorQuad.m_UIColorQuadMaterial; }
    }

    protected override void OnStart()
    {
        base.OnStart();
        mChanged = true;  // Start时让其重新渲染一次，否则在客户端会加载后没东西
    }

    /// <summary>
    /// 负责显示内容，它的工作是填写如何显示，显示什么。就是把需要显示的内容存储在UIWidget
    /// </summary>
    /// <param name="verts"></param>
    /// <param name="uvs">显示的多边形形状</param>
    /// <param name="cols">颜色调配</param>
    public override void OnFill(BetterList<Vector3> verts, BetterList<Vector2> uvs, BetterList<Color32> cols)
    {
        // 开始画网格, 顶点, 矩形
        Vector3[] arrVerts = localCorners;  // 直接由4个角组成矩形吧
        for (int i = 0; i < arrVerts.Length; i++)
        {
            verts.Add(arrVerts[i]);
        }

        // 贴图点
        for (int i = 0; i < arrVerts.Length; i++)
        {
            uvs.Add(new Vector2(0, 0));
        }

        // 顶点颜色
        Color pmaColor = NGUITools.ApplyPMA(this.color);  // NGUI PMA
        for (int i = 0; i < arrVerts.Length; i++)
        {
            cols.Add(pmaColor);
        }
    }

    // 创建材质
    void CheckQuadMaterial()
    {
        string szUseShaderName = "Unlit/Premultiplied Colored"; // NGUI的~

        if (UIColorQuad.m_UIColorQuadMaterial == null ||   // 下列情况下重新生成材质
            material == null ||
            material.shader == null ||
            material.shader.name != szUseShaderName
            )
        {
            GameObject.DestroyImmediate(UIColorQuad.m_UIColorQuadMaterial);

            UIColorQuad.m_UIColorQuadMaterial = new Material(Shader.Find(szUseShaderName));
            UIColorQuad.m_UIColorQuadMaterial.name = "UIColorQuadMaterial";

            // 生成一个1点的白色纹理
            Texture2D whiteTex = new Texture2D(1, 1);
            for (int y = 0; y < whiteTex.height; ++y)
            {
                for (int x = 0; x < whiteTex.width; ++x)
                {
                    whiteTex.SetPixel(x, y, new Color(1, 1, 1, 1));
                }
            }
            whiteTex.Apply();
            UIColorQuad.m_UIColorQuadMaterial.SetTexture("_MainTex", whiteTex);
        }
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        if (mChanged)
            CheckQuadMaterial(); // 確保Shader不為空，才能進入OnFill
    }
}
#endif
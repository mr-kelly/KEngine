#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KUIDemoHome.cs
// Date:     2015/12/03
// Author:  Kelly
// Email: 23110388@qq.com
// Github: https://github.com/mr-kelly/KEngine
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3.0 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library.

#endregion

using System.Collections;
using KEngine;
using KEngine.UI;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A script class that auto AddComponent to the UI AssetBundle(or Prefab)
/// </summary>
public class KUIDemoHome : UIController
{
    private Button Button1;
    private Text HomeLabel;
    public Text TipLabel;

    public override void OnInit()
    {
        base.OnInit();

        Button1 = GetControl<Button>("Button1"); // child
        Debuger.Assert(Button1);

        HomeLabel = GetControl<Text>("HomeText");
        TipLabel = GetControl<Text>("Tip");

        Button1.onClick.AddListener(() => Log.LogWarning("Click Home Button!"));
    }

    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);

        StartCoroutine(DemoUIAnimate());
    }

    private IEnumerator DemoUIAnimate()
    {
        yield return new WaitForSeconds(1f);
        HomeLabel.text = "Change UI Label...... 1";

        yield return new WaitForSeconds(1f);
        HomeLabel.text = "Change UI Label...... 2";

        yield return new WaitForSeconds(1f);
        HomeLabel.text = "Change UI Label...... 3";

        yield return new WaitForSeconds(1f);
        HomeLabel.text = "KEngine Demo!";
    }
}
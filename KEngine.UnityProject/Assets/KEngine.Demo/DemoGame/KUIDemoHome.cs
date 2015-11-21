//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
//
//                     Version 0.9.1 (20151010)
//                     Copyright © 2011-2015
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------
using UnityEngine;
using System.Collections;
using KEngine;
using UnityEngine.UI;

/// <summary>
/// A script class that auto AddComponent to the UI AssetBundle(or Prefab)
/// </summary>
public class KUIDemoHome : KUIController
{
    Button Button1;
    private Text HomeLabel;
    public override void OnInit()
    {
        base.OnInit();

        Button1 = GetControl<Button>("Button1"); // child
        Logger.Assert(Button1);

        HomeLabel = GetControl<Text>("HomeText");

        Button1.onClick.AddListener(() => Logger.LogWarning("Click Home Button!"));

    }

    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);

        StartCoroutine(DemoUIAnimate());
    }

    IEnumerator DemoUIAnimate()
    {
        yield return new WaitForSeconds(1f);
        HomeLabel.text = "Change UI Label...... 1";

        yield return new WaitForSeconds(1f);
        HomeLabel.text = "Change UI Label...... 2";

        yield return new WaitForSeconds(1f);
        HomeLabel.text = "Change UI Label...... 3";

        yield return new WaitForSeconds(1f);
        HomeLabel.text = "CosmosEngine Demo!";
    }
}

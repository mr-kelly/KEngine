//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
// 
//                     Version 0.8 (20140904)
//                     Copyright © 2011-2014
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------
using UnityEngine;
using System.Collections;

/// <summary>
/// A script class that auto AddComponent to the UI AssetBundle(or Prefab)
/// </summary>
public class CUIDemoHome : CUIController
{
    UIButton HomeButton;
    UILabel HomeLabel;

    public override void OnInit()
    {
        base.OnInit();

        HomeButton = GetControl<UIButton>("Button"); // child
        CDebug.Assert(HomeButton);

        HomeLabel = GetControl<UILabel>("Button/Label"); // uri....
        CDebug.Assert(HomeLabel);

        HomeButton = FindControl<UIButton>("Button"); // find by gameobject name
        CDebug.Assert(HomeButton);

        HomeLabel = FindControl<UILabel>("Label"); // child name
        CDebug.Assert(HomeLabel);

        HomeButton.onClick.Add(new EventDelegate(() => {
            CDebug.LogWarning("Click Home Button!");
        }));
        
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

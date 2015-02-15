using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using System.Collections.Generic;

/*
/// <summary>
/// 该类用于Ui排版时进行对齐, by KK
/// 
/// 根据x, y, z数字进行等距排列
/// </summary>
*/
public class AlignEditor : EditorWindow
{
    public string alignX = "0";
    public string alignY = "0";
    public string alignZ = "0";

    [MenuItem("Window/AlignEditor")]
    static void Init()
    {
        // Init Editor Window
        var alignEditor = EditorWindow.GetWindow(typeof(AlignEditor));
        alignEditor.Show();

    }
    void OnGUI()
    {
        GUILayout.Label("Distance Align Options");
        alignX = EditorGUILayout.TextField("X", alignX);
        alignY = EditorGUILayout.TextField("Y", alignY);
        alignZ = EditorGUILayout.TextField("Z", alignZ);
        /* 对齐选中对象按钮 */
        if (GUILayout.Button("Align Selection"))
        {
            GameObject[] gameObjects = this.getSortedGameObjects();

            /* 根据第一个对象所在位置，距离递增排列 */
            Vector3 firstObjectVec = Vector3.zero; /* 初始化 */
            for (int i = 0; i < gameObjects.Length; i++)
            {
                /* 循环第一个对象，赋变量 */
                if (i == 0)
                {
                    firstObjectVec = gameObjects[i].transform.localPosition;
                    continue;
                }
                
                /*循环其它对象*/
                gameObjects[i].transform.localPosition = new Vector3(
                    firstObjectVec.x + Convert.ToSingle(alignX) * i,
                    firstObjectVec.y + -Convert.ToSingle(alignY) * i,    /* 正交视觉，向下为y负数，向右为x正数， 忽略x */
                    firstObjectVec.z + Convert.ToSingle(alignZ) * i);
            }
        }

        GUILayout.Label("Other Align");
        
        GUILayout.BeginHorizontal("");

        if (GUILayout.Button("Left/Right Align"))
        {
            this.PositionSelectionObjects(AlignType.LeftAlign);
        }

        /* 顶对齐, 所有对象的y等于第一个对象 */
        if (GUILayout.Button("Top/Bottom Align"))
        {
            this.PositionSelectionObjects(AlignType.TopAlign);
        }
        GUILayout.EndHorizontal();
        
    }


    enum AlignType
    {
        TopAlign,
        LeftAlign,
        RightAlign,
        BottomAlign
    }


    /*
    /// <summary>
    /// 比较游戏对象名称委托方法
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    */
    private int CompareGameObjectsByName(GameObject a, GameObject b)
    {
        /* 使用系统的字符串比较 */

        return a.name.CompareTo(b.name);

        ///* 比较游戏对象名称最后一位 */
        //char aLast = a[a.Length-1];
        //char bLast = b[b.Length - 1];
        //if (a == b)
        //{
        //    return 0;  // equal
        //}
        //else
        //{

        //}
    }
    /*
    /// <summary>
    ///  获取根据名字重新排序的 已选游戏对象
    /// </summary>
    /// <returns></returns>
    */
    private GameObject[] getSortedGameObjects()
    {
        List<GameObject> gameObjects = new List<GameObject>(Selection.gameObjects);

        gameObjects.Sort(this.CompareGameObjectsByName);  /* 排序 委托*/

        return gameObjects.ToArray();
        
    }
    /*
    /// <summary>
    /// 统一定位所有选中的对象
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    */
    void PositionSelectionObjects(AlignType alignType)
    {
        GameObject[] gameObjects = this.getSortedGameObjects();
        

        /* 对齐开始 */
        if (gameObjects.Length > 0)
        {
            /* 获取第一个元素，其它元素根据它排位 */
            if (alignType == AlignType.TopAlign)
            {
                float firstY = gameObjects[0].transform.localPosition.y;  /*统一y, 顶对齐 */

                foreach (GameObject obj in gameObjects)
                {
                    float selfX = obj.transform.localPosition.x;
                    float selfZ = obj.transform.localPosition.z;

                    obj.transform.localPosition = new Vector3(selfX, firstY, selfZ);
                }
            }
            else if (alignType == AlignType.LeftAlign)  /*左对齐*/
            {
                float fisrtX = gameObjects[0].transform.localPosition.x;

                foreach (GameObject obj in gameObjects)
                {
                    float selfY = obj.transform.localPosition.y;
                    float selfZ = obj.transform.localPosition.z;

                    obj.transform.localPosition = new Vector3(fisrtX, selfY, selfZ);
                }
            }

        }
    }
}

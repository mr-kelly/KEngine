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
using UnityEditor;
using System;
using System.Collections.Generic;

/*
/// <summary>
/// ��������Ui�Ű�ʱ���ж���, by KK
/// 
/// ����x, y, z���ֽ��еȾ�����
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
        /* ����ѡ�ж���ť */
        if (GUILayout.Button("Align Selection"))
        {
            GameObject[] gameObjects = this.getSortedGameObjects();

            /* ���ݵ�һ����������λ�ã������������ */
            Vector3 firstObjectVec = Vector3.zero; /* ��ʼ�� */
            for (int i = 0; i < gameObjects.Length; i++)
            {
                /* ѭ����һ�����󣬸����� */
                if (i == 0)
                {
                    firstObjectVec = gameObjects[i].transform.localPosition;
                    continue;
                }
                
                /*ѭ����������*/
                gameObjects[i].transform.localPosition = new Vector3(
                    firstObjectVec.x + Convert.ToSingle(alignX) * i,
                    firstObjectVec.y + -Convert.ToSingle(alignY) * i,    /* �����Ӿ�������Ϊy����������Ϊx������ ����x */
                    firstObjectVec.z + Convert.ToSingle(alignZ) * i);
            }
        }

        GUILayout.Label("Other Align");
        
        GUILayout.BeginHorizontal("");

        if (GUILayout.Button("Left/Right Align"))
        {
            this.PositionSelectionObjects(AlignType.LeftAlign);
        }

        /* ������, ���ж����y���ڵ�һ������ */
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
    /// �Ƚ���Ϸ��������ί�з���
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    */
    private int CompareGameObjectsByName(GameObject a, GameObject b)
    {
        /* ʹ��ϵͳ���ַ����Ƚ� */

        return a.name.CompareTo(b.name);

        ///* �Ƚ���Ϸ�����������һλ */
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
    ///  ��ȡ����������������� ��ѡ��Ϸ����
    /// </summary>
    /// <returns></returns>
    */
    private GameObject[] getSortedGameObjects()
    {
        List<GameObject> gameObjects = new List<GameObject>(Selection.gameObjects);

        gameObjects.Sort(this.CompareGameObjectsByName);  /* ���� ί��*/

        return gameObjects.ToArray();
        
    }
    /*
    /// <summary>
    /// ͳһ��λ����ѡ�еĶ���
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    */
    void PositionSelectionObjects(AlignType alignType)
    {
        GameObject[] gameObjects = this.getSortedGameObjects();
        

        /* ���뿪ʼ */
        if (gameObjects.Length > 0)
        {
            /* ��ȡ��һ��Ԫ�أ�����Ԫ�ظ�������λ */
            if (alignType == AlignType.TopAlign)
            {
                float firstY = gameObjects[0].transform.localPosition.y;  /*ͳһy, ������ */

                foreach (GameObject obj in gameObjects)
                {
                    float selfX = obj.transform.localPosition.x;
                    float selfZ = obj.transform.localPosition.z;

                    obj.transform.localPosition = new Vector3(selfX, firstY, selfZ);
                }
            }
            else if (alignType == AlignType.LeftAlign)  /*�����*/
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

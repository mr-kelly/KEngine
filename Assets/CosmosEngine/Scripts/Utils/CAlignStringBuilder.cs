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
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// 一个基于StringBuilder的多行文字自动对齐构造器, 
/// 可以制作一个类似隐形表格的效果, 
/// 
/// 新的方式：UILabel自动子集，注意，刷新时会删掉所有子集的
/// 旧的方式：原理类似表格原理，对齐方式则使用空格自动补全!
/// 
/// 类似：
/// 
/// 列1：内容111         列2：内容2222
/// 行2：内容123123123   行22：内容abcdfefe
/// 
/// 传入CellStringLength，一个格子字符串长度
/// 
/// 行和列从数字1开始
/// </summary>
/// <author> kelly / 23110388@qq.com </author>
public class CAlignStringBuilder
{
    private int _autoRow; // 用于AutoAppend函数
    private int _autoColumn; // 用于AutoAppend函数

    private int _rowCount;
    private int _columnCount;
    private float _cellPixelLength;

    private IList<string> _cachedStrings;
    private UILabel _label;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="label"></param>
    /// <param name="rowCount"></param>
    /// <param name="columnCount"></param>
    /// <param name="cellPixelLength"></param>
    public CAlignStringBuilder(UILabel label, int rowCount, int columnCount, float cellPixelLength = -1)
    {
        _label = label;

        _cachedStrings = new string[rowCount * columnCount];
        _rowCount = rowCount;
        _columnCount = columnCount;
        _cellPixelLength = cellPixelLength.Equals(-1) ? (label.width / (float)columnCount) : cellPixelLength; // 可选参数

        _autoRow = 1;
        _autoColumn = 1;
    }

    /// <summary>
    /// 自动根据当前row和column设置string
    /// </summary>
    /// <param name="str"></param>
    public void AutoAppend(string str)
    {
        var index = CalcIndex(_autoRow, _autoColumn);
        _cachedStrings[index] = str;

        _autoColumn++;
        if (_autoColumn > _columnCount)
        {
            _autoColumn = 1;// 恢复到1行
            _autoRow++;
        }
    }

    /// <summary>
    /// 根据行列计算_cacheString偏移
    /// </summary>
    /// <param name="row"></param>
    /// <param name="column"></param>
    /// <returns></returns>
    private int CalcIndex(int row, int column)
    {
        return (row - 1) * _columnCount + (column - 1);
    }

    /// <summary>
    /// 使用StringBuilder进行字符拼接，由于等宽字体的问题，不用这个方法了
    /// </summary>
    /// <returns></returns>
    [Obsolete]
    public override string ToString()
    {
        var stringBuilder = new StringBuilder(); // init capacity
        stringBuilder.Length = 0;

        for (var row = 1; row <= _rowCount; row++)
        {
            for (var col = 1; col <= _columnCount; col++)
            {
                var index = CalcIndex(row, col);
                var appendOriginStr = _cachedStrings[index];
                if (!string.IsNullOrEmpty(appendOriginStr))
                {
                    var appendOriginStrLength = appendOriginStr.Length;
                    stringBuilder.Append(appendOriginStr);

                    // 字符串不够长，补充空格
                    var lengthDiff = _cellPixelLength - appendOriginStrLength;
                    if (lengthDiff > 0)
                    {
                        for (var d = 0; d < lengthDiff; d++)
                        {
                            stringBuilder.Append(" ");
                        }
                    }
                }
            }

            // 换行时换行符
            stringBuilder.Append("\n");
        }

        return stringBuilder.ToString();
    }

    /// <summary>
    /// 刷新
    /// </summary>
    public void Refresh()
    {
        _label.text = "";
        CTool.DestroyGameObjectChildren(_label.cachedGameObject);
        var originActive = _label.cachedGameObject.activeSelf;
        if (originActive)
        {
            _label.cachedGameObject.SetActive(false); // 预防copy时激发Awake，生成UICamera
        }

        // 格子数量,copy源Label对象
        var cellCount = _rowCount*_columnCount;
        UILabel[] copyLabels = new UILabel[cellCount];
        for (var i = 0; i < copyLabels.Length; i++)
        {
            var copyGameObj = GameObject.Instantiate(_label.cachedGameObject) as GameObject;
            CDebug.Assert(copyGameObj);
            copyLabels[i] = copyGameObj.GetComponent<UILabel>();
        }

        // 设置child
        for (var row = 1; row <= _rowCount; row++)
        {
            for (var col = 1; col <= _columnCount; col++)
            {
                var index = CalcIndex(row, col);
                var childLabel = copyLabels[index];
                CTool.SetChild(childLabel.cachedTransform, _label.cachedTransform);
                var localX = (col - 1) * _cellPixelLength;
                var localY = (row - 1)*_label.height;

                // 这里加上源Label的Y Spacing，用于行间距
                if (row > 1) localY += _label.spacingY * (row - 1);

                childLabel.cachedTransform.localPosition = new Vector3(localX, -localY, 0);

                childLabel.cachedGameObject.SetActive(true);
                childLabel.text = _cachedStrings[index];
            }
        }
        if (originActive)
        {
            _label.cachedGameObject.SetActive(true); // 预防copy时激发Awake，生成UICamera
        }
    }
}

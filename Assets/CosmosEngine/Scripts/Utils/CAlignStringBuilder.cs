using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// 一个基于StringBuilder的多行文字自动对齐构造器, 
/// 可以制作一个类似隐形表格的效果, 
/// 原理类似表格原理，对齐方式则使用空格自动补全!
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

    private StringBuilder _stringBuilder;

    private int _autoRow; // 用于AutoAppend函数
    private int _autoColumn; // 用于AutoAppend函数

    private int _rowCount;
    private int _columnCount;
    private int _cellStringLength;

    private IList<string> _cachedStrings;

    public CAlignStringBuilder(int rowCount, int columnCount, int cellStringLength)
    {
        _cachedStrings = new string[rowCount * columnCount];
        _stringBuilder = new StringBuilder(); // init capacity
        _rowCount = rowCount;
        _columnCount = columnCount;
        _cellStringLength = cellStringLength;

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
        return (row - 1) * column + (column - 1);
    }

    public override string ToString()
    {
        _stringBuilder.Length = 0;

        for (var row = 1; row <= _rowCount; row++)
        {
            for (var col = 1; col <= _columnCount; col++)
            {
                var index = CalcIndex(row, col);
                var appendOriginStr = _cachedStrings[index];
                if (!string.IsNullOrEmpty(appendOriginStr))
                {
                    var appendOriginStrLength = appendOriginStr.Length;
                    _stringBuilder.Append(appendOriginStr);

                    // 字符串不够长，补充空格
                    var lengthDiff = _cellStringLength - appendOriginStrLength;
                    if (lengthDiff > 0)
                    {
                        for (var d = 0; d < lengthDiff; d++)
                        {
                            _stringBuilder.Append(" ");
                        }
                    }
                }
            }

            // 换行时换行符
            _stringBuilder.Append("\n");
        }

        return _stringBuilder.ToString();
    }
}

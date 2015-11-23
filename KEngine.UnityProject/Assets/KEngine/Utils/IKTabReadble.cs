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

public interface IKTabReadble
{
    bool HasColumn(string columnName);
    int GetInteger(int row, string columnName);
    string GetString(int row, string columnName);
    string GetString(int row, int columnIndex);
    float GetFloat(int row, string columnName);
    bool GetBool(int row, string columnName);
    double GetDouble(int row, string columnName);
    uint GetUInteger(int row, string columnName);

    int GetHeight();
    int GetColumnCount();

    void Close();
}

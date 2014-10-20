using UnityEngine;
using System.Collections;

public interface ICTabReadble
{
    bool HasColumn(string columnName);
    int GetInteger(int row, string columnName);
    string GetString(int row, string columnName);
    float GetFloat(int row, string columnName);
    bool GetBool(int row, string columnName);
    double GetDouble(int row, string columnName);
    uint GetUInteger(int row, string columnName);

    int GetHeight();

    void Close();
}

#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: CFiber_Demo.cs
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
using UnityEngine;

public class CFiber_Demo : MonoBehaviour
{
    private void Start()
    {
        CFiber.Instance.PlayCoroutine(TestCo());
    }

    private IEnumerator TestCo()
    {
        yield return new WaitForSeconds(1f);
        Debug.Log("Success! Wait For seconds" + Time.time);

        yield return new CustomWaitForMileSeconds(3000);
        Debug.Log("Success! Wait For mileseconds" + Time.time);

        Debug.Log("Over TestCo");
    }
}


public class CustomWaitForMileSeconds : CFiberBase
{
    private int MileSeconds;

    private float StartTime;

    public CustomWaitForMileSeconds(int mileseconds)
    {
        MileSeconds = mileseconds;
        StartTime = Time.time;
    }

    public override IEnumerator Wait()
    {
        float endTime = StartTime + (float) MileSeconds/1000f;
        while (Time.time < endTime)
            yield return null;
    }
}
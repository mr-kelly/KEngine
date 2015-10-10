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
using System.IO;
using System.Text;

/// <summary>
/// 一个List，具有缓存在磁盘的功能！高性能写入！ 不重复的key, HashSet
/// 
/// 会自动将上次写入错误的信息抹掉
/// 
/// 不能做Delete的操作，只适合Add, 比CFileHashSet更好！后边替代它！
/// </summary>
public class CFileCacheList : IDisposable
{
    private StreamWriter _writer;
    private Stream _appendStream;
    private HashSet<string> _hashSet;
    
    private bool _isMD5 = true;

    public CFileCacheList(string ioPath, bool isMd5Mode = true)
    {
        var newHashSet = new HashSet<string>();
        Init(ioPath, ref newHashSet, isMd5Mode);
    }

    public CFileCacheList(string ioPath, ref HashSet<string> refHashSet, bool isMd5Mode = true)
    {
        Init(ioPath, ref refHashSet, isMd5Mode);
    }

    private void Init(string ioPath, ref HashSet<string> refHashSet, bool isMd5Mode)
    {
        _isMD5 = isMd5Mode;
        _hashSet = refHashSet;

        if (File.Exists(ioPath))
        {
            using (var readStream = new FileStream(ioPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                // Read
                var sb = new StringBuilder();
                var sLength = readStream.Length;
                var lastNewlinePos = 0L;
                while (readStream.Position < sLength)
                {
                    var c = (char)readStream.ReadByte();
                    if (c == '\n')
                    {
                        var getStr = sb.ToString().Trim();
                        _hashSet.Add(getStr); // 过滤换行符 '\r'等
                        sb.Length = 0; // clear
                        lastNewlinePos = readStream.Position;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }

                if (lastNewlinePos != readStream.Position)
                {
                    // 抹掉最后一行！
                    // 最后一行，故意忽略掉，为什么？
                    // 因为上次程序有可能写到一半中途退出！  造成最后一行写入错误！超坑的！
                    readStream.SetLength(lastNewlinePos);  // 拦截到换行处
                }
            }  
        }
        _appendStream = new FileStream(ioPath, FileMode.Append, FileAccess.Write, FileShare.Read);
        _writer = new StreamWriter(_appendStream);

        _writer.AutoFlush = true; // 自动刷新, 每一次都是一个写入，会降低系统性能，但大大增加可靠性
    }

    public bool Add(string str)
    {

        var setStr = str.Trim();
        if (_isMD5)
            setStr = CTool.MD5_16bit(str);
        if (_hashSet.Add(setStr))
        {
            _writer.WriteLine(setStr);
            return true;
        }

        return false;
    }

    public bool Contains(string str)
    {
        var findStr = str;
        if (_isMD5)
            findStr = CTool.MD5_16bit(str);

        return _hashSet.Contains(findStr);
    }
    public int Count
    {
        get { return _hashSet.Count; }
    }

    /// <summary>
    /// 主动刷新缓存区
    /// </summary>
    public void Flush()
    {
        _writer.Flush();
    }
    public void Dispose()
    {
        _writer.Close();
        _appendStream.Close();
    }
}

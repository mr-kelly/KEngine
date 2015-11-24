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
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;

public class CZipTool : MonoBehaviour
{

    public static void SetZipFile(string zipPath, string fileName, string content)
    {
        using (var zipFile = CreateReadZipFile(zipPath))
        {
            zipFile.BeginUpdate();
            zipFile.Add(new StringDataSource(content), fileName);
            zipFile.CommitUpdate();
        };
    }
    public static string GetFileContentFromZip(string zipPath, string fileName)
    {
        var bytes = GetFileBytesFromZip(zipPath, fileName);
        if (bytes == null) return null;

        return Encoding.UTF8.GetString(bytes);
    }

    public static byte[] GetFileBytesFromZip(string zipPath, string fileName)
    {
        using (var zipFile = CreateReadZipFile(zipPath))
        {
            var entry = zipFile.GetEntry(fileName);
            if (entry != null)
            {
                var stream = zipFile.GetInputStream(entry);
                var bytes = new byte[entry.Size];
                stream.Read(bytes, 0, bytes.Length);
                return bytes;
            }
            return null;
        };
    }
    static ZipFile CreateReadZipFile(string filePath)
    {
        ZipFile zip;
        if (File.Exists(filePath))
            zip = new ZipFile(filePath);
        else
        {
            zip = ZipFile.Create(filePath);
            zip.BeginUpdate();
            zip.Add(new StringDataSource("Copyright KEngine, created zip by Kelly's ZipTool"), ".KEngine"); // must have a file on init, or a Exception
            zip.CommitUpdate();
        }
        return zip;
    }

    class StringDataSource : IStaticDataSource
    {
        public string Str { get; set; }

        public StringDataSource(string str)
        {
            this.Str = str;
        }

        public Stream GetSource()
        {
            Stream s = new MemoryStream(Encoding.Default.GetBytes(Str));
            return s;
        }
    }
}

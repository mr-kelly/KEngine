#region Copyright (c) 2015 KEngine / Kelly <http://github.com/mr-kelly>, All rights reserved.

// KEngine - Toolset and framework for Unity3D
// ===================================
// 
// Filename: KZipTool.cs
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

using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;

namespace KEngine.Lib
{
    public class KZipTool
    {
        public static void SetZipFile(string zipPath, string fileName, string content)
        {
            using (var zipFile = CreateReadZipFile(zipPath))
            {
                zipFile.BeginUpdate();
                zipFile.Add(new StringDataSource(content), fileName);
                zipFile.CommitUpdate();
            }
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
            }
            ;
        }

        private static ZipFile CreateReadZipFile(string filePath)
        {
            ZipFile zip;
            if (File.Exists(filePath))
                zip = new ZipFile(filePath);
            else
            {
                zip = ZipFile.Create(filePath);
                zip.BeginUpdate();
                zip.Add(new StringDataSource("Copyright KEngine, created zip by Kelly's ZipTool"), ".KEngine");
                // must have a file on init, or a Exception
                zip.CommitUpdate();
            }
            return zip;
        }

        private class StringDataSource : IStaticDataSource
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
}

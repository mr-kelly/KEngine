using System;
using System.Collections;

namespace KEngine
{
    public enum AppVersionType
    {
        Development,
        Beta,
        Preview,
        Release,
    }

    /// <summary>
    /// For App Version
    /// 
    /// 1.2.3.10.4567.release.mi
    /// </summary>
    public class AppVersion
    {
        public Version Version { get; private set; }
        public AppVersionType VersionType { get; private set; }
        public string VersionDesc { get; private set; }

        public AppVersion(string versionStr)
        {
            var versionArr = versionStr.Split('.');
            if (versionArr.Length < 4)
                throw new Exception("Version String's length must larger than 4!");
            Version = new Version(versionStr);

            if (versionArr.Length >= 5)
            {
                VersionType = (AppVersionType)Enum.Parse(typeof(AppVersionType), versionArr[4]);
            }

            if (versionArr.Length >= 6)
            {
                VersionDesc = versionArr[6];
            }
        }
    }

}

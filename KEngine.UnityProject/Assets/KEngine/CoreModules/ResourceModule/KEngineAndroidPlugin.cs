using System;
using UnityEngine;
using System.Collections;

namespace KEngine
{
    /// <summary>
    /// KEngine's Android Plugins is a jar.
    /// 
    /// Load sync from android asset folder.
    /// Unity3D's WWW class can only load async from asset folder, disgusting.
    /// </summary>
    public class KEngineAndroidPlugin
    {
#if UNITY_ANDROID
        private static AndroidJavaClass _helper;

        /// <summary>
        /// Get AndroidHelper from Java jar
        /// </summary>
        private static AndroidJavaClass AndroidHelper
        {
            get
            {
                if (_helper != null) return _helper;

                _helper = new AndroidJavaClass("com.github.KEngine.AndroidHelper");

                if (_helper == null)
                    ErrorNotSupport();

                return _helper;
            }
        }
#endif

        private static void ErrorNotSupport()
        {
            throw new Exception("[KEngineAndroidPlugin.cs]Error on Android Plugin. Check if KEngine.Android.jar file exist in your Plugins/Android/libs?");
        }

        /// <summary>
        /// Check if path exist in asset folder?
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsAssetExists(string path)
        {
#if UNITY_ANDROID
            return AndroidHelper.CallStatic<bool>("isAssetExists", path);
#else
            ErrorNotSupport();
            return false;
#endif
        }

        /// <summary>
        /// Call from java get asset file bytes and convert to string
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetAssetString(string path)
        {
#if UNITY_ANDROID
            return AndroidHelper.CallStatic<string>("getAssetString", path);
#else
            ErrorNotSupport();
            return null;
#endif
        }

        /// <summary>
        /// Call from java get asset file bytes
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static byte[] GetAssetBytes(string path)
        {
#if UNITY_ANDROID
            return AndroidHelper.CallStatic<byte[]>("getAssetBytes", path);
#else
            ErrorNotSupport();
            return null;
#endif
        }
    }
}


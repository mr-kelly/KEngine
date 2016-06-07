package com.github.KEngine;
import android.app.Activity;
import android.content.res.AssetManager;
import com.unity3d.player.UnityPlayer;
import java.io.InputStream;
import java.util.Hashtable;

/**
 * Created by Sean on 2015/7/9.
 * Modified by chenpeilin1 on 2016/05/28.
 *
 * Synchronic read file from apk assets folder, which Unity's WWW cannot.
 *
 */
public class AndroidHelper {

    private static AssetManager sAssetManager;
    private static boolean isInit = false;
    private static Activity sActivity = null;
    private static Hashtable mFileTable = new Hashtable();

    static {
        // auto init with current Unity3D Activity!
        AndroidHelper.init(UnityPlayer.currentActivity);
    }

    static void init(Object activity) {
        if (!isInit) {
            sActivity = (Activity)activity;
            sAssetManager = sActivity.getAssets();
            isInit = true;
        }
    }
//    @SuppressWarnings("unchecked")
    public static boolean isAssetExists(String path)
    {
        boolean ret = false;
        if(mFileTable.containsKey(path))
            return  (boolean)mFileTable.get(path);
        if(sAssetManager != null)
        {
            InputStream input = null;
            try
            {
                input = sAssetManager.open(path);
                ret = true;
                mFileTable.put(path,true);
                input.close();
            }
            catch (Exception e)
            {
                mFileTable.put(path, false);
            }
        }
        return ret;
    }
//    @SuppressWarnings("unchecked")
    public static byte[] getAssetBytes(String path)
    {
        byte[] mBytes = null;
        if(sAssetManager != null)
        {
            InputStream input = null;
            try
            {
                input = sAssetManager.open(path);
                int length = input.available();
                mBytes = new byte[length];
                input.read(mBytes);
                input.close();
                if(!mFileTable.containsKey(path))
                {
                    mFileTable.put(path,true);
                }
            }
            catch (Exception e)
            {
                if(!mFileTable.containsKey(path))
                {
                    mFileTable.put(path,false);
                }
            }
        }
        return mBytes;
    }

    public static String getAssetString(String path)
    {
        byte[] mBytes = getAssetBytes(path);
        if(mBytes != null)
        {
            try
            {
                return new String(mBytes,"utf-8");
            }
            catch (Exception e)
            {

            }
            return "";
        }
        return "";
    }
}

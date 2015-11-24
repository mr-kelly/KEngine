using UnityEngine;
using System.Collections;

/// <summary>
/// 对XXXLoader的结果Asset进行Debug显示
/// </summary>
public class KResoourceLoadedAssetDebugger : MonoBehaviour
{
    public UnityEngine.Object TheObject;
    const string bigType = "LoadedAssetDebugger";
    public string Type;
    private bool IsRemoveFromParent = false;
    public static KResoourceLoadedAssetDebugger Create(string type, string url, UnityEngine.Object theObject)
    {
        var newHelpGameObject = new GameObject(string.Format("LoadedObject-{0}-{1}", type, url));
        KDebuggerObjectTool.SetParent(bigType, type, newHelpGameObject);

        var newHelp = newHelpGameObject.AddComponent<KResoourceLoadedAssetDebugger>();
        newHelp.Type = type;
        newHelp.TheObject = theObject;
        return newHelp;
    }

    void Update()
    {
        if (TheObject == null && !IsRemoveFromParent)
        {
            KDebuggerObjectTool.RemoveFromParent(bigType, Type, gameObject);
            IsRemoveFromParent = true;
        }
    }
    // 可供调试删资源
    void OnDestroy()
    {
        if (!IsRemoveFromParent)
        {
            KDebuggerObjectTool.RemoveFromParent(bigType, Type, gameObject);
            IsRemoveFromParent = true;
        }

    }
}
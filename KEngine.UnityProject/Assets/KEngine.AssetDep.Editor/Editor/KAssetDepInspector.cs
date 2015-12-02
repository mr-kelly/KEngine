using UnityEngine;
using System.Collections;

namespace KEngine.AssetDep.Editor
{
    [UnityEditor.CustomEditor(typeof(KAssetDep))]
    public class CBaseAssetDepInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            bool isFinish = ((KAssetDep)target).IsFinishDependency;
            if (isFinish)
                UnityEditor.EditorGUILayout.LabelField("依赖已经加载完毕！");
        }
    }
}
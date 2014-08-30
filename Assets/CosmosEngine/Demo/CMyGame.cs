using UnityEngine;
using System.Collections;

public class CMyGame : MonoBehaviour
{
    void Awake()
    {
        CCosmosEngine.New(
            gameObject,
            new ICModule[] { },
            null,
            null);

        CUIManager.Instance.OpenWindow<CUIDemoHome>();
    }
}

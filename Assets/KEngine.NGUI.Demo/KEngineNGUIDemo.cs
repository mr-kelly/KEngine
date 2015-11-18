using UnityEngine;
using System.Collections;

public class KEngineNGUIDemo : MonoBehaviour {

	// Use this for initialization
	void Start () {
        //CGameSettings.Instance.InitAction += OnGameSettingsInit;

        KEngine.AppEngine.New(
            gameObject,
            new ICModule[] {
                CGameSettings.Instance,
            },
            null,
            null);

        KUIModule.Instance.OpenWindow<KUITestWindow>();

        KUIModule.Instance.CallUI<KUITestWindow>(ui =>
        {

            // Do some UI stuff

        });
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}

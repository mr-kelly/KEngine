using UnityEngine;
using System.Collections;
using KEngine.CoreModules;

public class KEngineNGUIDemoMain : MonoBehaviour {

	// Use this for initialization
	void Start () {
        //KGameSettings.Instance.InitAction += OnGameSettingsInit;

        KEngine.AppEngine.New(
            gameObject,
            new ICModule[] {
                KGameSettings.Instance,
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

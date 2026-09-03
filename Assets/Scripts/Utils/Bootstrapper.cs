using TriInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class Bootstrapper : MonoBehaviour
{
    [SerializeField] private bool loadSceneOnStart = false;

    [ShowIf(nameof(loadSceneOnStart), true)]
    [SerializeField]
    private AssetReference sceneToLoad;

    void Start()
    {
        if (loadSceneOnStart && sceneToLoad != null)
        {
            ServiceLocator.TryOrWaitForService<SceneService>(LoadScene);
        }
    }

    private void LoadScene(IService sceneService)
    {
        if (sceneToLoad.RuntimeKeyIsValid())
        {
            ((SceneService)sceneService).TransitionToScene(sceneToLoad);
        }
        else
        {
            Debug.LogError($"Invalid scene reference: {sceneToLoad.AssetGUID}");
        }
    }

    #region LOAD BOOTSTRAPPER BEFORE SCENE LOAD
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadBootstrapper()
    {
        SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
    }
    #endregion
}
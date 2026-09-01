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
            ServiceLocator.TryGetOrWaitForService<SceneService>(out var sceneService, LoadScene);
        }
    }

    private void LoadScene(SceneService sceneService)
    {
        if (sceneToLoad.RuntimeKeyIsValid())
        {
            sceneService.TransitionToScene(sceneToLoad);
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
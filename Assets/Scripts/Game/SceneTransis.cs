using UnityEngine;
using UnityEngine.AddressableAssets;

public class SceneTransis : MonoBehaviour
{
    private SceneService _sceneService;
    [SerializeField] private AssetReference sceneToLoad;

    void Awake()
    {
        ServiceLocator.TryOrWaitForService<SceneService>(SetSceneService);
    }

    void OnDestroy()
    {
        ServiceLocator.RemoveServiceCallback<SceneService>(SetSceneService);
    }

    void SetSceneService(IService service)
    {
        if (service is not SceneService sceneService)
        {
            Debug.LogError($"ServiceLocator returned a service of type {service.GetType()} instead of SceneService.");
            return;
        }

        _sceneService = sceneService;
    }

    public void TransitionToScene()
    {
        if (_sceneService != null && sceneToLoad != null && sceneToLoad.RuntimeKeyIsValid())
        {
            _sceneService.TransitionToScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("SceneService is not available.");
        }
    }

    public void Test()
    {
        Debug.Log("Test method called in SceneTransis.");
    }
}

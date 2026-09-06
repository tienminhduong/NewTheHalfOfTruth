using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private CD.LoadSceneData _loadSceneData;

    protected virtual void HandleDoorInteract()
    {
        if (_loadSceneData.SceneToLoad == null)
        {
            Debug.LogError("LoadSceneData is not set for the door.");
            return;
        }

        if (_sceneService == null)
        {
            Debug.LogError("SceneService is not available.");
            return;
        }

        _sceneService.TransitionToScene(_loadSceneData);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            HandleDoorInteract();
        }
    }

    private SceneService _sceneService;

    protected virtual void OnEnable()
    {
        ServiceLocator.TryOrWaitForService<SceneService>(SetupSceneService);
    }

    protected virtual void OnDisable()
    {
        ServiceLocator.RemoveServiceCallback<SceneService>(SetupSceneService);
    }

    private void SetupSceneService(IService service)
    {
        if (service is not SceneService sceneService)
        {
            Debug.LogError($"ServiceLocator returned a service of type {service.GetType()} instead of SceneService.");
            return;
        }

        _sceneService = sceneService;
    }
}

using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private BaseUIButton startButton;
    private SceneService _sceneService;
    private FileLogService _fileLogService;

    private void HandleSceneService(IService service)
    {
        if (service is not SceneService sceneService)
        {
            Debug.LogError($"ServiceLocator returned a service of type {service.GetType()} instead of SceneService.");
            return;
        }

        _sceneService = sceneService;
    }

    void OnEnable()
    {
        ServiceLocator.TryOrWaitForService<SceneService>(HandleSceneService);
        ServiceLocator.TryOrWaitForService<FileLogService>(HandleFileLogService);
        startButton.AddListener(HandleStartButtonClick);
        if (startButton == null)
        {
            Debug.LogError("Start button is not assigned in the inspector.");
        }
        else
        {
            Debug.Log("Start button is assigned and listener added.");
        }
        Debug.Log("MainMenu enabled and listeners added.");
    }

    void OnDisable()
    {
        ServiceLocator.RemoveServiceCallback<SceneService>(HandleSceneService);
        ServiceLocator.RemoveServiceCallback<FileLogService>(HandleFileLogService);
        startButton.RemoveListener(HandleStartButtonClick);
        if (startButton == null)
        {
            Debug.LogError("Start button is not assigned in the inspector.");
        }
        else
        {
            Debug.Log("Start button listener removed.");
        }
        Debug.Log("MainMenu disabled and listeners removed.");
    }

    private void HandleFileLogService(IService service)
    {
        if (service is not FileLogService fileLogService)
        {
            Debug.LogError($"ServiceLocator returned a service of type {service.GetType()} instead of FileLogService.");
            return;
        }

        _fileLogService = fileLogService;
    }

    private void HandleStartButtonClick()
    {
        if (_sceneService == null)
        {
            Debug.LogError("SceneService is not available. Cannot transition to StartRoad scene.");
            return;
        }

        Debug.Log("Start button clicked. Transitioning to StartRoad scene.");

        _fileLogService?.EnsureSessionKey();
        _sceneService.TransitionToScene("Assets/Scenes/StartRoad.unity");
    }
}

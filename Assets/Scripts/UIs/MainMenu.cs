using TMPro;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private BaseUIButton startButton;
    [SerializeField] private BaseUIButton copySessionKeyButton;
    [SerializeField] private TextMeshProUGUI sessionKeyText;
    [SerializeField] private ClipboardManager clipboardManager;
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
        copySessionKeyButton.AddListener(HandleCopyButtonClick);
    }

    void OnDisable()
    {
        ServiceLocator.RemoveServiceCallback<SceneService>(HandleSceneService);
        ServiceLocator.RemoveServiceCallback<FileLogService>(HandleFileLogService);
        startButton.RemoveListener(HandleStartButtonClick);
        copySessionKeyButton.RemoveListener(HandleCopyButtonClick);
    }

    private void HandleFileLogService(IService service)
    {
        if (service is not FileLogService fileLogService)
        {
            Debug.LogError($"ServiceLocator returned a service of type {service.GetType()} instead of FileLogService.");
            return;
        }

        _fileLogService = fileLogService;
        _fileLogService.EnsureSessionKey();
        if (sessionKeyText != null)
        {
            sessionKeyText.text = $"Session key: {_fileLogService.SessionKey}";
        }
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

    private void HandleCopyButtonClick()
    {
        if (clipboardManager == null)
        {
            Debug.LogError("ClipboardManager is not assigned in the inspector.");
            return;
        }

        clipboardManager.CopyText(_fileLogService?.SessionKey ?? string.Empty);
    }
}

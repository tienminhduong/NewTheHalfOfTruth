using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private BaseUIButton startButton;
    private SceneService _sceneService;

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
        startButton.AddListener(HandleStartButtonClick);
    }

    void OnDisable()
    {
        ServiceLocator.RemoveServiceCallback<SceneService>(HandleSceneService);
        startButton.RemoveListener(HandleStartButtonClick);
    }

    private void HandleStartButtonClick()
    {
        if (_sceneService == null)
        {
            return;
        }

        _sceneService.TransitionToScene("Assets/Scenes/StartRoad.unity");
    }
}

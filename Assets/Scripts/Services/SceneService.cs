using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.UI;

public class SceneService : MonoBehaviour, IService
{
    [SerializeField] private Image sceneTransitionImage;
    [SerializeField] private float fadeDuration = 1f;

    private SceneInstance activeSceneInstance;
    private CD.LoadSceneData currentLoadSceneData;
    public CD.LoadSceneData CurrentLoadSceneData => currentLoadSceneData;
    public bool HasLoadData { get; private set; } = false;

    public void Awake() => ServiceLocator.RegisterService(this);

    public void OnDestroy() => ServiceLocator.UnregisterService(this);

    public void TransitionToScene(string sceneAddress)
    {
        TransitionToSceneAsync(sceneAddress).Forget();
    }

    public void TransitionToScene(AssetReference sceneReference)
    {
        if (sceneReference.RuntimeKeyIsValid())
        {
            TransitionToSceneAsync(sceneReference.RuntimeKey.ToString()).Forget();
        }
        else
        {
            Debug.LogError($"Invalid scene reference: {sceneReference.AssetGUID}");
        }
    }

    public void TransitionToScene(CD.LoadSceneData loadSceneData)
    {
        currentLoadSceneData = loadSceneData;
        HasLoadData = true;
        TransitionToScene(loadSceneData.SceneToLoad);
    }

    private async UniTask TransitionToSceneAsync(string sceneAddress)
    {
        await sceneTransitionImage.DOFade(1, fadeDuration).AsyncWaitForCompletion();
        sceneTransitionImage.raycastTarget = true;

        var loadOperation = Addressables.LoadSceneAsync(sceneAddress);
        activeSceneInstance = await loadOperation.Task;

        await sceneTransitionImage.DOFade(0, fadeDuration).AsyncWaitForCompletion();
        sceneTransitionImage.raycastTarget = false;
    }
}

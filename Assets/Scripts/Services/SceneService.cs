using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.UI;

public class SceneService : MonoBehaviourService<SceneService>
{
    private const string CutscenePlayedKeyPrefix = "SceneService.CutscenePlayed.";

    [SerializeField] private Image sceneTransitionImage;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private string endgameSceneAddress = "Assets/Scenes/EndScene.unity";
    [Tooltip("Max seconds to keep the transition overlay up while scene Addressables finish loading. 0 = wait indefinitely.")]
    [SerializeField] private float assetLoadTimeout = 30f;

    [SerializeField] List<CD.CutsceneData> cutsceneDataList = new();

    private readonly Dictionary<string, CD.CutsceneData> cutsceneDataDict = new();
    private bool _logUploadStarted;
    private bool _isTransitioning;
    private FileLogService _fileLogService;

    public override void Awake()
    {
        base.Awake();

        ServiceLocator.TryOrWaitForService<FileLogService>(HandleFileLogService);

        foreach (var cutsceneData in cutsceneDataList)
        {
            if (!cutsceneDataDict.ContainsKey(cutsceneData.sceneTriggerAddress))
            {
                cutsceneDataDict.Add(cutsceneData.sceneTriggerAddress, cutsceneData);
            }
            else
            {
                Debug.LogWarning($"Duplicate scene trigger address found: {cutsceneData.sceneTriggerAddress}. Only the first occurrence will be used.");
            }
        }
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

    private AsyncOperationHandle<SceneInstance> activeSceneHandle;
    private CD.LoadSceneData currentLoadSceneData;
    public CD.LoadSceneData CurrentLoadSceneData => currentLoadSceneData;
    public bool HasLoadData { get; private set; } = false;

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

    /// <summary>
    /// Keeps the transition overlay up until every Addressable load requested by the new scene
    /// (e.g. animation clip preloads started in Start) has finished.
    /// </summary>
    private async UniTask WaitForSceneAssetsAsync()
    {
        // Let the new scene's Start() methods run so their loads get registered.
        await UniTask.NextFrame(destroyCancellationToken);

        if (!AddressableLoadTracker.HasPendingLoads)
            return;

        if (assetLoadTimeout <= 0f)
        {
            await AddressableLoadTracker.WaitForPendingLoadsAsync(destroyCancellationToken);
            return;
        }

        var timedOut = await AddressableLoadTracker.WaitForPendingLoadsAsync(destroyCancellationToken)
            .TimeoutWithoutException(TimeSpan.FromSeconds(assetLoadTimeout));
        if (timedOut)
            Debug.LogWarning($"Scene assets still loading after {assetLoadTimeout}s; revealing scene anyway.");
    }

    private async UniTask TransitionToSceneAsync(string sceneAddress)
    {
        if (_isTransitioning)
        {
            Debug.LogWarning($"Scene transition already in progress; ignoring request for '{sceneAddress}'.");
            return;
        }

        _isTransitioning = true;
        try
        {
            await TransitionToSceneCoreAsync(sceneAddress);
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    private async UniTask TransitionToSceneCoreAsync(string sceneAddress)
    {
        await sceneTransitionImage.DOFade(1, fadeDuration).AsyncWaitForCompletion();
        sceneTransitionImage.raycastTarget = true;
        InputHandler.FocusUIInput();
        if (cutsceneDataDict.TryGetValue(sceneAddress, out var cutsceneData))
        {
            var cutscenePlayedKey = GetCutscenePlayedKey(sceneAddress);
            if (!PlayerPrefs.HasKey(cutscenePlayedKey))
            {
                PlayerPrefs.SetInt(cutscenePlayedKey, 1);
                PlayerPrefs.Save();
                sceneAddress = cutsceneData.cutsceneAsset.RuntimeKey.ToString();
            }
        }

        var prevSceneHandle = activeSceneHandle;
        var loadOperation = Addressables.LoadSceneAsync(sceneAddress);
        await loadOperation.Task;

        if (loadOperation.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"Failed to load scene '{sceneAddress}': {loadOperation.OperationException}");
            if (loadOperation.IsValid())
                Addressables.Release(loadOperation);

            // Previous scene (if any) is still the active one; reveal it and give control back.
            await sceneTransitionImage.DOFade(0, fadeDuration).AsyncWaitForCompletion();
            sceneTransitionImage.raycastTarget = false;
            InputHandler.FocusPlayerInput();
            return;
        }

        activeSceneHandle = loadOperation;

        // Single-mode loads already unloaded (and auto-released) the previous scene; this is a no-op then.
        await UnloadActiveSceneAsync(prevSceneHandle);

        await WaitForSceneAssetsAsync();

        await sceneTransitionImage.DOFade(0, fadeDuration).AsyncWaitForCompletion();
        sceneTransitionImage.raycastTarget = false;
        InputHandler.FocusPlayerInput();

        if (!_logUploadStarted && string.Equals(sceneAddress, endgameSceneAddress, StringComparison.Ordinal))
        {
            _logUploadStarted = true;
            _fileLogService?.UploadLogToGoogleFormAsync().Forget();
        }
    }

    private static string GetCutscenePlayedKey(string sceneTriggerAddress)
    {
        return CutscenePlayedKeyPrefix + sceneTriggerAddress;
    }

    private async UniTask UnloadActiveSceneAsync(AsyncOperationHandle<SceneInstance> sceneHandle)
    {
        if (sceneHandle.IsValid())
        {
            var unloadOperation = Addressables.UnloadSceneAsync(sceneHandle);
            await unloadOperation.Task;
        }
    }

    public override void OnDestroy()
    {
        ServiceLocator.RemoveServiceCallback<FileLogService>(HandleFileLogService);

        if (activeSceneHandle.IsValid())
        {
            var sceneHandle = activeSceneHandle;
            activeSceneHandle = default;
            Addressables.UnloadSceneAsync(sceneHandle);
        }

        base.OnDestroy();
    }
}

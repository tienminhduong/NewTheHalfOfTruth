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

    [SerializeField] List<CD.CutsceneData> cutsceneDataList = new();

    private readonly Dictionary<string, CD.CutsceneData> cutsceneDataDict = new();
    private bool _logUploadStarted;
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

    private async UniTask TransitionToSceneAsync(string sceneAddress)
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
        activeSceneHandle = loadOperation;
        await loadOperation.Task;

        await UnloadActiveSceneAsync(prevSceneHandle);

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

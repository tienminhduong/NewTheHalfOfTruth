using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.UI;

public class SceneService : MonoBehaviourService<SceneService>
{
    [SerializeField] private Image sceneTransitionImage;
    [SerializeField] private float fadeDuration = 1f;

    [SerializeField] List<CD.CutsceneData> cutsceneDataList = new();

    private readonly Dictionary<string, CD.CutsceneData> cutsceneDataDict = new();

    public override void Awake()
    {
        base.Awake();

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

    private SceneInstance activeSceneInstance;
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
            sceneAddress = cutsceneData.cutsceneAsset.RuntimeKey.ToString();
        var loadOperation = Addressables.LoadSceneAsync(sceneAddress);
        activeSceneInstance = await loadOperation.Task;

        await sceneTransitionImage.DOFade(0, fadeDuration).AsyncWaitForCompletion();
        sceneTransitionImage.raycastTarget = false;
        InputHandler.FocusPlayerInput();
    }
}

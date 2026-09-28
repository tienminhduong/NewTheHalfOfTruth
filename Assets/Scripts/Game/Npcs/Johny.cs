using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TriInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class Johny : ChatNpc
{
    [SerializeField] private AssetReference _spawnImageReference;
    [SerializeField] private string _dialogueAfterFirstInteraction;
    [SerializeField] private string _roadSceneName = "Road";

    [Title("Regret player question")]
    [SerializeField] private string[] playerQuestionsToRegret;
    [SerializeField] private AssetReference _endingCutsceneReference;
    private SceneService _sceneService;

    private bool isWeakPointHit = false;


    protected override void OnEnable()
    {
        base.OnEnable();

        LLMService.OnWeakPointHit += HandleWeakPointHit;

        ServiceLocator.TryOrWaitForService<SceneService>(SetupSceneService);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        LLMService.OnWeakPointHit -= HandleWeakPointHit;

        ServiceLocator.RemoveServiceCallback<SceneService>(SetupSceneService);
    }

    public override void OnChatEnd()
    {
        base.OnChatEnd();

        if (gameObject.scene.name == _roadSceneName)
            LoadSpawnImageAsync(destroyCancellationToken).ContinueWith(MoveOutScreen);

        if (isWeakPointHit)
        {
            if (_endingCutsceneReference != null)
            {
                _sceneService?.TransitionToScene(_endingCutsceneReference);
            }
        }
    }

    protected override void SetupSavedService(IService service)
    {
        base.SetupSavedService(service);

        if (SavedDataService != null)
        {
            if (SavedDataService.HasInteractedWithNPC(NpcName))
            {
                if (gameObject.scene.name == _roadSceneName)
                    Destroy(gameObject);
                else
                    StartUpDialogue = _dialogueAfterFirstInteraction;
            }
        }
    }

    protected void SetupSceneService(IService service)
    {
        if (service is SceneService sceneService)
        {
            _sceneService = sceneService;
        }
        else
        {
            Debug.LogError($"ServiceLocator returned a service of type {service.GetType()} instead of SceneService.");
        }
    }

    private async UniTask LoadSpawnImageAsync(CancellationToken cancellationToken)
    {
        if (_spawnImageReference != null)
        {
            var spawnImage = await _spawnImageReference.InstantiateAsync().Task;
            spawnImage.transform.position = transform.position;
            spawnImage.name = spawnImage.name[..^7]; // Remove "(Clone)" from the name
        }
    }

    private void MoveOutScreen()
    {
        _animSystem.PlayAnimation(CD.FaceDir.Right, CD.HumanState.Walk);
        IsInteractable = false;
        transform.DOMoveX(transform.position.x + 4f, 2f).SetEase(Ease.Linear).OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }

    private void HandleWeakPointHit()
    {
        Debug.Log("Weak point hit detected for Johny.");
        if (playerQuestionsToRegret != null && playerQuestionsToRegret.Length > 0)
        {
            DialogueUISystem.AutoPromptDialogue(playerQuestionsToRegret);
        }
        isWeakPointHit = true;
    }
}

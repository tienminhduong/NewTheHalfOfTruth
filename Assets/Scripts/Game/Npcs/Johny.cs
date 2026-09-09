using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class Johny : ChatNpc
{
    [SerializeField] private string _dialogueAfterFirstInteraction;
    [SerializeField] private string _roadSceneName = "Road";
    public override void OnChatEnd()
    {
        base.OnChatEnd();

        if (gameObject.scene.name == _roadSceneName)
            LoadSpawnImageAsync(destroyCancellationToken).ContinueWith(MoveOutScreen);
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

    private async UniTask LoadSpawnImageAsync(CancellationToken cancellationToken)
    {
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
}

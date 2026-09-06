using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class Brad : ChatNpc
{
    [SerializeField] private string _playerThoughAfterChatEnd;
    [SerializeField] private float _delayBeforePlayerThoughts = 2f;

    public override void OnChatEnd()
    {
        base.OnChatEnd();
        HandleChatEndedAsync().Forget();
    }

    private async UniTask HandleChatEndedAsync()
    {
        await UniTask.Delay(Mathf.RoundToInt(_delayBeforePlayerThoughts * 1000), cancellationToken: this.GetCancellationTokenOnDestroy());
        if (!string.IsNullOrEmpty(_playerThoughAfterChatEnd))
            DialogueUISystem.ShowDialogue(_playerThoughAfterChatEnd, false);
    }
}

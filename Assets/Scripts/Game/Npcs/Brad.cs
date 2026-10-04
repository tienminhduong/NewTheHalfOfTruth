using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class Brad : ChatNpc
{
    private const string PostChatDialogueShownKey = "Brad.PostChatDialogueShown";

    [SerializeField] private string _playerThoughAfterChatEnd;
    [SerializeField] private float _delayBeforePlayerThoughts = 2f;
    private bool _postChatDialoguePending;

    public override void OnChatEnd()
    {
        base.OnChatEnd();

        if (_postChatDialoguePending || SavedDataService?.GetCustomData(PostChatDialogueShownKey) == "true")
            return;

        _postChatDialoguePending = true;
        HandleChatEndedAsync().Forget();
    }

    private async UniTask HandleChatEndedAsync()
    {
        await UniTask.Delay(Mathf.RoundToInt(_delayBeforePlayerThoughts * 1000), cancellationToken: this.GetCancellationTokenOnDestroy());
        if (string.IsNullOrEmpty(_playerThoughAfterChatEnd))
            return;

        SavedDataService?.SaveCustomData(PostChatDialogueShownKey, "true");
        DialogueUISystem.ShowDialogue(_playerThoughAfterChatEnd, false);
    }
}

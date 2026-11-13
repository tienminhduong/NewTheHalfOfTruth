using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TypingEffectTMP : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _textMeshPro;
    [SerializeField] private float _typingSpeed = 0.05f;

    public bool IsTyping { get; private set; } = false;
    public event Action OnTypingComplete;

    private CancellationTokenSource typingCancellationTokenSource;
    private string _currentDialogue = string.Empty;
    private const string HTML_TRANSPARENT_TAG = "<color=#00000000>";
    private const string HTML_END_TAG = "</color>";

    void OnValidate()
    {
        _textMeshPro = GetComponent<TextMeshProUGUI>();
    }

    public void PlayDialogue(string dialogueLine)
    {
        EndCurrentDialogue();
        _currentDialogue = dialogueLine ?? string.Empty;
        typingCancellationTokenSource = new CancellationTokenSource();
        PlayDialogueAsync(_currentDialogue, typingCancellationTokenSource.Token).Forget();
    }

    public void EndCurrentDialogue()
    {
        if (IsTyping)
        {
            IsTyping = false;
            typingCancellationTokenSource.Cancel();
            _textMeshPro.text = _currentDialogue;
            OnTypingComplete?.Invoke();
        }
    }

    private async UniTask PlayDialogueAsync(string dialogueLine, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(dialogueLine))
        {
            OnTypingComplete?.Invoke();
            return;
        }
        IsTyping = true;

        _textMeshPro.text = string.Empty;
        var originalText = dialogueLine;
        int alphaIndex = 0;

        foreach (char c in originalText)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                alphaIndex++;
                string displayText = originalText.Insert(alphaIndex, HTML_TRANSPARENT_TAG) + HTML_END_TAG;
                _textMeshPro.text = displayText;

                await UniTask.Delay(TimeSpan.FromSeconds(_typingSpeed), cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (!IsTyping || typingCancellationTokenSource == null
                    || typingCancellationTokenSource.Token != cancellationToken)
                    return;

                IsTyping = false;
                _textMeshPro.text = originalText;
                OnTypingComplete?.Invoke();
                return;
            }
        }

        OnTypingComplete?.Invoke();
        IsTyping = false;
    }

    void OnDestroy()
    {
        EndCurrentDialogue();
    }
}
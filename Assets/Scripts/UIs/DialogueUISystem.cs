using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class DialogueUISystem : BaseUIPanelSystem
{
    [Header("Response Panel")]
    [SerializeField] private GameObject responsePanel;
    [SerializeField] private TypingEffectTMP dialogueText;

    [Header("Input Panel")]
    [SerializeField] private GameObject inputPanel;
    [SerializeField] private TMP_InputField inputField;

    [SerializeField] private BaseUIButton submitButton;
    [SerializeField] private BaseUIButton cancelButton;

    private LLMService _llmService;
    private ChatNpc _currentChatNpc;
    private bool _isWaitingForLLMResponse = false;

    private static event Action<string, bool> OnShowDialogue;
    private static event Action<ChatNpc> OnShowChatNpcDialogue;
    public static void ShowDialogue(string dialogue, bool showInputPanel = true)
    {
        OnShowDialogue?.Invoke(dialogue, showInputPanel);
    }

    public static void ShowChatNpcDialogue(ChatNpc npc)
    {
        OnShowChatNpcDialogue?.Invoke(npc);
    }

    override protected void OnEnable()
    {
        base.OnEnable();

        OnShowDialogue += ShowDialogueBox;
        OnShowChatNpcDialogue += ShowDialogueBox;
        dialogueText.OnTypingComplete += EnableSubmitButton;

        submitButton.AddListener(HandleSubmitButtonClick);
        cancelButton.AddListener(ClosePanel);
        inputField.onSubmit.AddListener(HandleSubmitButtonClick);

        ServiceLocator.TryOrWaitForService<LLMService>(SetupLLMService);
    }

    override protected void OnDisable()
    {
        base.OnDisable();

        OnShowDialogue -= ShowDialogueBox;
        OnShowChatNpcDialogue -= ShowDialogueBox;
        dialogueText.OnTypingComplete -= EnableSubmitButton;

        submitButton.RemoveListener(HandleSubmitButtonClick);
        cancelButton.RemoveListener(ClosePanel);
        inputField.onSubmit.RemoveListener(HandleSubmitButtonClick);

        ServiceLocator.RemoveServiceCallback<LLMService>(SetupLLMService);
    }

    private void ShowDialogueBox(string dialogue, bool showInputPanel = true)
    {
        OpenPanel();
        responsePanel.SetActive(true);
        inputPanel.SetActive(showInputPanel);
        dialogueText.PlayDialogue(dialogue);
    }

    private void ShowDialogueBox(ChatNpc npc)
    {
        _currentChatNpc = npc;
        ShowDialogueBox(npc.StartUpDialogue, true);
    }

    protected override void OpenPanel()
    {
        base.OpenPanel();

        inputField.text = string.Empty;
        submitButton.SetInteractable(true);
    }

    protected override void ClosePanel()
    {
        if (_isWaitingForLLMResponse)
            return;
        if (dialogueText.IsTyping)
        {
            dialogueText.EndCurrentDialogue();
            return;
        }
        base.ClosePanel();
        if (_currentChatNpc != null)
        {
            _currentChatNpc.OnChatEnd();
            _currentChatNpc = null;
        }
    }

    private void HandleSubmitButtonClick()
    {
        string playerInput = inputField.text;
        HandleSubmitButtonClick(playerInput);
    }

    private void HandleSubmitButtonClick(string playerInput)
    {
        if (string.IsNullOrWhiteSpace(playerInput))
        {
            return;
        }

        if (_llmService != null)
        {
            _llmService.CallLLMAsync(_currentChatNpc.NpcName, _currentChatNpc.NpcSystem, playerInput)
                .ContinueWith(HandleLLMResponse).Forget();
            LockClosePanel(true);
            submitButton.SetInteractable(false);
        }
        else
        {
            Debug.LogWarning("LLMService is not available. Player input will not be sent.");
        }
    }

    private void EnableSubmitButton()
    {
        submitButton.SetInteractable(true);
    }

    private void SetupLLMService(IService service)
    {
        if (service is LLMService llmService)
            _llmService = llmService;
    }

    private void ChangeDialogueText(string newDialogue)
    {
        if (dialogueText.IsTyping)
            dialogueText.EndCurrentDialogue();
        dialogueText.PlayDialogue(newDialogue);
    }

    private void HandleLLMResponse(LLMResponse response)
    {
        if (response.status == LLMResponse.Status.Success)
        {
            if (dialogueText.IsTyping)
                dialogueText.EndCurrentDialogue();
            ChangeDialogueText(response.response);
        }
        else
            Debug.LogError($"LLMService failed to generate a response: {response.response}");
        LockClosePanel(false);
    }

    private void LockClosePanel(bool lockPanel)
    {
        _isWaitingForLLMResponse = lockPanel;
        cancelButton.SetInteractable(!lockPanel);

    }
}
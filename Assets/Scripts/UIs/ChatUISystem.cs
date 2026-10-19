using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using TriInspector;
using UnityEngine;
using UnityEngine.UI;
public class ChatUISystem : BaseUIPanelSystem
{
    [SerializeField] private ChatBubble _chatBubblePrefab;
    [SerializeField] private Transform _content;
    [SerializeField] private ScrollRect _scrollRect;

    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private BaseUIButton _submitButton;

    [SerializeField] private NPCDatabase _npcDatabase;
    [SerializeField] private TextMeshProUGUI _npcNameText;
    [SerializeField] private TextMeshProUGUI _npcDescriptionText;

    [Header("For testing purposes only")]
    [SerializeField] private string chatContent;
    private LLMService _llmService;
    private ChatHistoryService _chatHistoryService;

    private ChatNpc _currentNPCName;
    private bool _isWaitingForLLMResponse = false;
    private bool IsUpdatingBubbleThough => _isWaitingForLLMResponse;

    [Header("Thinking Bubble")]
    [ReadOnly][SerializeField] private ChatBubble _thinkingBubble;
    private ChatBubble _streamingResponseBubble;
    private string _streamingResponseText = string.Empty;
    [SerializeField] private string _thinkingMessage = "...";
    [SerializeField] private float _thinkingInterval = 0.5f; // in seconds
    [SerializeField] private int _thinkingCharCount = 0;
    private readonly UnityCountdownTimer _thinkingTimer = new(isLooped: true);

    private void SetupLLMService(IService service)
    {
        if (service is LLMService llmService)
        {
            _llmService = llmService;
        }
    }

    private void SetupChatHistoryService(IService service)
    {
        if (service is ChatHistoryService chatHistoryService)
        {
            _chatHistoryService = chatHistoryService;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        ServiceLocator.TryOrWaitForService<LLMService>(SetupLLMService);
        ServiceLocator.TryOrWaitForService<ChatHistoryService>(SetupChatHistoryService);
    }

    void OnDestroy()
    {
        ServiceLocator.RemoveServiceCallback<LLMService>(SetupLLMService);
        ServiceLocator.RemoveServiceCallback<ChatHistoryService>(SetupChatHistoryService);

        _thinkingTimer?.Destroy();
    }

    [Button("Test call llm")]
    private void TestCallLLM()
    {
        if (!IsPanelOpen)
            return;

        if (!string.IsNullOrEmpty(chatContent))
        {
            _llmService?.CallLLMAsync(_currentNPCName.NpcName, _currentNPCName.NpcSystem, chatContent);
            chatContent = string.Empty;
        }
        else
        {
            Debug.LogWarning("Chat content is empty. Please enter a message to test.");
        }
    }

    private readonly List<ChatBubble> _activeChatBubbles = new();

    protected override void OnEnable()
    {
        base.OnEnable();

        ChatEvents.OnChatItemPushed += OnChatItemPushed;
        ChatEvents.OnChatKeySet += HandleKeySet;
        ChatEvents.OnChatKeyReleased += HandleKeyReleased;
        _submitButton.AddListener(OnSubmitButtonClicked);
        _inputField.onSubmit.AddListener(HandleSubmitButtonClick);

        _thinkingTimer.SetOnTimerComplete(UpdateThinkingBubble);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        HandleDisabled();
        ChatEvents.OnChatItemPushed -= OnChatItemPushed;
        ChatEvents.OnChatKeySet -= HandleKeySet;
        ChatEvents.OnChatKeyReleased -= HandleKeyReleased;
        _submitButton.RemoveListener(OnSubmitButtonClicked);
        _inputField.onSubmit.RemoveListener(HandleSubmitButtonClick);

        _thinkingTimer.RemoveOnTimerComplete();
    }

    private void OnChatItemPushed(ChatItem chatItem)
    {
        if (_chatBubblePrefab == null || _content == null)
        {
            Debug.LogWarning("ChatBubble prefab or content Transform is not assigned in the inspector.");
            return;
        }

        if (chatItem.personKey != "player" && _streamingResponseBubble != null)
        {
            _streamingResponseBubble.SetText(chatItem.message);
            _streamingResponseBubble.ForceUpdateLayout();
            _streamingResponseBubble = null;
            _streamingResponseText = string.Empty;
            return;
        }

        GetChatBubbleFromPool(chatItem.message, chatItem.personKey == "player");
    }

    private ChatBubble GetChatBubbleFromPool(string message = "", bool isPlayer = false)
    {
        var chatBubble = Instantiate(_chatBubblePrefab, _content);
        chatBubble.SetTextWithRole(message, isPlayer);
        chatBubble.ForceUpdateLayout();
        _activeChatBubbles.Add(chatBubble);

        Canvas.willRenderCanvases += OnBeforeCanvasRender;

        return chatBubble;
    }

    private void ReturnChatBubbleToPool(ChatBubble chatBubble)
    {
        _activeChatBubbles.Remove(chatBubble);
        Destroy(chatBubble.gameObject);
    }

    protected override void ClosePanel()
    {
        if (_isWaitingForLLMResponse)
            return;

        _llmService?.ReleaseChat();
        base.ClosePanel();
    }

    private void HandleDisabled()
    {
        for (var index = _activeChatBubbles.Count - 1; index >= 0; index--)
        {
            var chatBubble = _activeChatBubbles[index];
            if (chatBubble != null)
                ReturnChatBubbleToPool(chatBubble);
        }
        _activeChatBubbles.Clear();
    }

    private void HandleKeySet(ChatNpc activeChatKey)
    {
        _currentNPCName = activeChatKey;
        OpenPanel();
    }

    private void HandleKeyReleased()
    {
        base.ClosePanel();
        HandleDisabled();
    }

    protected override void OpenPanel()
    {
        base.OpenPanel();

        _npcNameText.text = _currentNPCName.NpcName;
        var npcData = _npcDatabase.GetNPCData(_currentNPCName.NpcName);
        _npcDescriptionText.text = npcData.npcDescription;

        if (_chatHistoryService.TryGetActiveChatHistory(out var chatHistory))
        {
            foreach (var chatItem in chatHistory.chatItems)
            {
                GetChatBubbleFromPool(chatItem.message, chatItem.personKey == "player");
            }
        }
    }

    private void HandleSubmitButtonClick(string _)
    {
        OnSubmitButtonClicked();
    }


    private void OnSubmitButtonClicked()
    {
        if (_isWaitingForLLMResponse)
            return;
        string playerInput = _inputField.text;
        if (string.IsNullOrWhiteSpace(playerInput))
        {
            Debug.LogWarning("Input field is empty. Please enter a message.");
            return;
        }

        var system = _npcDatabase.GetNPCData(_currentNPCName.NpcName).npcSystem;
        _isWaitingForLLMResponse = true;
        ResetStreamingResponseState();

        _llmService?.CallLLMAsync(_currentNPCName.NpcName, system, playerInput, AppendStreamingResponse)
            .ContinueWith(HandleLLMCallCompleted)
            .Forget();

        _thinkingBubble = GetChatBubbleFromPool(_thinkingMessage, isPlayer: false);
        _thinkingTimer.StartTimer(_thinkingInterval);

        _inputField.text = string.Empty;
    }

    private void AppendStreamingResponse(string token)
    {
        if (_streamingResponseBubble == null)
            _streamingResponseBubble = GetChatBubbleFromPool();

        _streamingResponseText += token;
        _streamingResponseBubble.SetText(_streamingResponseText);
        _streamingResponseBubble.ForceUpdateLayout();
    }

    private void HandleLLMCallCompleted(LLMResponse response)
    {
        _isWaitingForLLMResponse = false;
        if (_thinkingBubble != null)
        {
            ReturnChatBubbleToPool(_thinkingBubble);
            _thinkingBubble = null;
            _thinkingTimer.IsRunning = false;
        }

        if (response.status == LLMResponse.Status.Success && _streamingResponseBubble != null)
        {
            _streamingResponseBubble.SetText(response.response);
            _streamingResponseBubble.ForceUpdateLayout();
            ResetStreamingResponseState();
        }
        else if (response.status != LLMResponse.Status.Success && _streamingResponseBubble != null)
        {
            ReturnChatBubbleToPool(_streamingResponseBubble);
            ResetStreamingResponseState();
        }
    }

    private void ResetStreamingResponseState()
    {
        _streamingResponseBubble = null;
        _streamingResponseText = string.Empty;
    }

    private void Update()
    {
        _thinkingTimer.UpdateTimer(Time.deltaTime);
    }

    private void UpdateThinkingBubble()
    {
        if (_thinkingBubble == null || !IsUpdatingBubbleThough)
            return;

        var currentText = _thinkingMessage[.._thinkingCharCount];
        _thinkingBubble.SetText(currentText);

        _thinkingCharCount = DMath.GetNextInRange(_thinkingCharCount, 1, _thinkingMessage.Length, loop: true);
        // _thinkingCharCount = Mathf.Clamp(_thinkingCharCount, 1, _thinkingMessage.Length);
    }
    private void OnBeforeCanvasRender()
    {
        Canvas.willRenderCanvases -= OnBeforeCanvasRender;
        LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRect.content);
        _scrollRect.verticalNormalizedPosition = 0f; // Snap to bottom
    }
}

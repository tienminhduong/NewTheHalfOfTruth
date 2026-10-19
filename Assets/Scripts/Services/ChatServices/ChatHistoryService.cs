using System;
using System.Collections.Generic;
using UnityEngine;

public class ChatHistoryService : BaseSaveLoadService<ChatHistoryService>
{
    private readonly Dictionary<string, ChatHistory> _chatHistories = new();
    private const string PrefKeys = "ChatHistories";

    private ChatNpc _activeChatNpc = null;
    public ChatNpc ActiveChatNpc
    {
        get => _activeChatNpc;
        set
        {
            if (_activeChatNpc != value)
            {
                _activeChatNpc = value;
                if (ActiveChatNpc != null)
                    ChatEvents.OnChatKeySet?.Invoke(_activeChatNpc);
                else
                    ChatEvents.OnChatKeyReleased?.Invoke();
            }
        }
    }

    public bool TryGetChatHistory(string key, out ChatHistory chatHistory)
    {
        return _chatHistories.TryGetValue(key, out chatHistory);
    }

    public bool TryGetActiveChatHistory(out ChatHistory chatHistory)
    {
        if (ActiveChatNpc == null)
        {
            chatHistory = default;
            return false;
        }
        return _chatHistories.TryGetValue(ActiveChatNpc.NpcName, out chatHistory);
    }

    public void ReleaseActiveChatKey()
    {
        if (_activeChatNpc != null)
        {
            _activeChatNpc = null;
            ChatEvents.OnChatKeyReleased?.Invoke();
        }
    }

    public void PushChatItem(string personKey, string message)
    {
        if (ActiveChatNpc == null)
        {
            Debug.LogWarning("No active chat key set. Cannot push chat item.");
            return;
        }

        if (!_chatHistories.TryGetValue(ActiveChatNpc.NpcName, out var chatHistory))
        {
            chatHistory = new ChatHistory
            {
                key = ActiveChatNpc.NpcName,
                chatItems = new List<ChatItem>()
            };
            _chatHistories[ActiveChatNpc.NpcName] = chatHistory;
        }

        chatHistory.EnsureNotNull();
        var chatItem = new ChatItem { personKey = personKey, message = message };

        chatHistory.chatItems.Add(chatItem);
        ChatEvents.OnChatItemPushed?.Invoke(chatItem);
    }

    public void ReplaceActiveChatHistory(string summary)
    {
        if (ActiveChatNpc == null)
        {
            Debug.LogWarning("No active chat key set. Cannot replace chat history.");
            return;
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            Debug.LogError("Cannot replace chat history with an empty compact summary.");
            return;
        }

        ReplaceChatHistory(ActiveChatNpc.NpcName, summary);
    }

    public void ReplaceChatHistory(string key, string summary)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogError("Cannot replace chat history with an empty key.");
            return;
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            Debug.LogError("Cannot replace chat history with an empty compact summary.");
            return;
        }

        _chatHistories[key] = new ChatHistory
        {
            key = key,
            summary = summary,
            chatItems = new List<ChatItem>()
        };
    }

    public ChatHistoryService(bool loadFromPrefs) : base(loadFromPrefs)
    {
    }

    protected override void LoadFromPref()
    {
        if (PlayerPrefs.HasKey(PrefKeys))
        {
            var json = PlayerPrefs.GetString(PrefKeys);
            var savedData = JsonUtility.FromJson<ChatHistorySaved>(json);
            savedData.EnsureNotNull();
            foreach (var chatHistory in savedData.chatHistories)
            {
                _chatHistories[chatHistory.key] = chatHistory;
            }
        }
    }

    protected override void SaveToPref()
    {
        var savedData = new ChatHistorySaved
        {
            chatHistories = new List<ChatHistory>(_chatHistories.Values)
        };
        var json = JsonUtility.ToJson(savedData);
        PlayerPrefs.SetString(PrefKeys, json);
        PlayerPrefs.Save();
    }
}

[Serializable]
public struct ChatHistory
{
    public string key;
    public string summary;
    public List<ChatItem> chatItems;

    public void EnsureNotNull()
    {
        chatItems ??= new List<ChatItem>();
    }
}

[Serializable]
public struct ChatHistorySaved
{
    public List<ChatHistory> chatHistories;
    public void EnsureNotNull()
    {
        chatHistories ??= new List<ChatHistory>();
        foreach (var chatHistory in chatHistories)
        {
            chatHistory.EnsureNotNull();
        }
    }
}

[Serializable]
public struct ChatItem
{
    public string personKey; // Name of the person, 'player' for Player
    public string message;
}
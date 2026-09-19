using System.Collections.Generic;

public class ChatHistoryService : LogicOnlyService<ChatHistoryService>
{
    private readonly Dictionary<string, ChatHistory> _chatHistories = new();
    private bool _loadFromPrefs;

    private void LoadFromPref()
    {
        // Load chat history from PlayerPrefs or any other persistent storage
    }

    private void SaveToPref()
    {
        // Save chat history to PlayerPrefs or any other persistent storage
    }
}

public struct ChatHistory
{
    public string key;
    public List<ChatItem> chatItems;
}

public struct ChatItem
{
    public string personKey; // Name of the person, 'player' for Player
    public string message;
}
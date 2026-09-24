using System;

public static class ChatEvents
{
    public static Action<string> OnChatKeySet;
    public static Action OnChatKeyReleased;
    public static Action<ChatItem> OnChatItemPushed;
}
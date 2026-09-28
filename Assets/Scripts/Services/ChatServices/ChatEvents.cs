using System;

public static class ChatEvents
{
    public static Action<ChatNpc> OnChatKeySet;
    public static Action OnChatKeyReleased;
    public static Action<ChatItem> OnChatItemPushed;
}
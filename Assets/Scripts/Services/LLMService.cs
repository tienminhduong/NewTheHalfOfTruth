using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class LLMService : LogicOnlyService<LLMService>
{
    private const string BASE_URL = "https://rag-streaming-faiss.onrender.com/api/query/ask-stream";
    private const string COMPACT_HISTORY_URL = "https://rag-streaming-faiss.onrender.com/api/query/compact-history";
    private const int COMPACT_HISTORY_LINE_COUNT = 10;
    private const string ERROR_MESSAGE = "Bạn hỏi gì thế tôi không hiểu??";
    public static event Action OnWeakPointHit;

    public bool ReleaseChat()
    {
        var prev = _chatHistoryService.ActiveChatNpc;
        _chatHistoryService.ActiveChatNpc = null;
        if (prev != null)
            prev.OnChatEnd();

        return prev != null;
    }

    private ChatHistoryService _chatHistoryService;
    private FileLogService _fileLogService;

    public override void Awake()
    {
        base.Awake();

        _fileLogService = ServiceLocator.GetService<FileLogService>();
        ServiceLocator.TryOrWaitForService<ChatHistoryService>(HandleChatHistoryServiceReady);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        ServiceLocator.RemoveServiceCallback<ChatHistoryService>(HandleChatHistoryServiceReady);
    }

    private void HandleChatHistoryServiceReady(IService service)
    {
        if (service is not ChatHistoryService chatHistoryService)
        {
            Debug.LogError($"ServiceLocator returned a service of type {service.GetType()} instead of ChatHistoryService.");
            return;
        }

        _chatHistoryService = chatHistoryService;
    }

    public async UniTask<LLMResponse> CallLLMAsync(string npcName = "", string npcSystem = "", string question = "", Action<string> onTokenReceived = null)
    {
        var requestHistory = new List<ChatRequestItem>();
        var summary = string.Empty;
        if (_chatHistoryService != null &&
            _chatHistoryService.TryGetChatHistory(npcName, out var chatHistory))
        {
            summary = chatHistory.summary;
            if (chatHistory.chatItems != null)
            {
                var contextIndex = Mathf.Clamp(
                    chatHistory.currentContextIndex,
                    0,
                    chatHistory.chatItems.Count);
                for (var index = contextIndex; index < chatHistory.chatItems.Count; index++)
                {
                    var chatItem = chatHistory.chatItems[index];
                    requestHistory.Add(new ChatRequestItem
                    {
                        role = GetRequestRole(chatItem.personKey),
                        content = chatItem.message
                    });
                }
            }
        }

        LLMRequest requestData = new()
        {
            npcName = npcName,
            npcSystem = npcSystem,
            question = question,
            summary = summary,
            history = requestHistory
        };
        _chatHistoryService?.PushChatItem("player", question);

        string jsonData = JsonUtility.ToJson(requestData);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        string errorCode = null;
        try
        {
            Debug.Log($"LLMService: Sending request to LLM API with data: {jsonData}");
            Action<string> tokenLogger = token =>
            {
                Debug.Log($"LLMService: Token received: {token}");
                _fileLogService?.LogToken(token);
                onTokenReceived?.Invoke(token);
            };
            var streamResult = await PostRequestAsync(BASE_URL, jsonData, tokenLogger);
            string response = streamResult.Response;
            errorCode = streamResult.ErrorCode;
#if BENCHMARK_TIME
            stopwatch.Stop();
            Debug.Log($"LLMService: LLM API call time: {stopwatch.ElapsedMilliseconds} ms");
            // _fileLogService?.LogBenchmarkTime(stopwatch.ElapsedMilliseconds);
#endif
            stopwatch.Stop();
            _fileLogService?.LogConversation(
                npcName,
                question,
                jsonData,
                response,
                stopwatch.ElapsedMilliseconds,
                "Chat",
                errorCode);
            _fileLogService?.SubmitConversationToGoogleForm(
                npcName,
                question,
                jsonData,
                response,
                errorCode);
            _chatHistoryService?.PushChatItem(npcName, response);
            await CompactHistoryIfNeededAsync(npcName);
            return new LLMResponse
            {
                response = response,
                status = LLMResponse.Status.Success
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var requestException = ex as LlmRequestException;
            var serverMessage = requestException?.ServerMessage ?? ex.Message;
            errorCode ??= requestException?.ErrorCode ?? requestException?.HttpStatusCode.ToString();
            Debug.LogError($"LLMService: Error calling LLM API: {serverMessage}");
            _fileLogService?.LogConversation(
                npcName,
                question,
                jsonData,
                serverMessage,
                stopwatch.ElapsedMilliseconds,
                "Error",
                errorCode);
            _fileLogService?.SubmitConversationToGoogleForm(
                npcName,
                question,
                jsonData,
                serverMessage,
                errorCode);
            return new LLMResponse
            {
                response = serverMessage,
                status = LLMResponse.Status.Error
            };
        }
    }

    private async UniTask CompactHistoryIfNeededAsync(string npcName)
    {
        if (_chatHistoryService == null ||
            !_chatHistoryService.TryGetChatHistory(npcName, out var chatHistory) ||
            chatHistory.chatItems == null ||
            chatHistory.chatItems.Count - chatHistory.currentContextIndex < COMPACT_HISTORY_LINE_COUNT)
        {
            return;
        }

        var requestData = new CompactHistoryRequest
        {
            npcName = npcName,
            history = new List<CompactHistoryItem>()
        };

        foreach (var chatItem in chatHistory.chatItems)
        {
            requestData.history.Add(new CompactHistoryItem
            {
                role = GetRequestRole(chatItem.personKey),
                content = chatItem.message
            });
        }

        string jsonData = JsonUtility.ToJson(requestData);
        try
        {
            Debug.Log($"LLMService: Compacting chat history for NPC '{npcName}'.");
            string compactSummary = await PostCompactHistoryRequestAsync(COMPACT_HISTORY_URL, jsonData);
            _chatHistoryService.ReplaceChatHistory(npcName, compactSummary);
        }
        catch (Exception ex)
        {
            Debug.LogError($"LLMService: Error compacting chat history: {ex.Message}");
        }
    }

    private static string GetRequestRole(string personKey)
    {
        return personKey == "player" ? "player" : "npc";
    }

    private static async UniTask<string> PostCompactHistoryRequestAsync(string url, string json)
    {
        using var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json");

        await request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
            throw new InvalidOperationException(request.error ?? ERROR_MESSAGE);

        string responseText = request.downloadHandler.text?.Trim();
        if (string.IsNullOrWhiteSpace(responseText))
            throw new InvalidOperationException("Compact history endpoint returned an empty response.");

        if (responseText.StartsWith("{", StringComparison.Ordinal))
        {
            var response = JsonUtility.FromJson<CompactHistoryResponse>(responseText);
            responseText = response.summary;
        }
        else if (responseText.StartsWith("\"", StringComparison.Ordinal))
        {
            responseText = JsonUtility.FromJson<CompactHistoryStringResponse>(
                $"{{\"value\":{responseText}}}").value;
        }

        if (string.IsNullOrWhiteSpace(responseText))
            throw new InvalidOperationException("Compact history endpoint returned an empty summary.");

        return responseText;
    }

    private async UniTask<LlmStreamResult> PostRequestAsync(string url, string json, Action<string> onTokenReceived)
    {
        using var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        var downloadHandler = new SseDownloadHandler();
        request.downloadHandler = downloadHandler;
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "text/event-stream");

        var operation = request.SendWebRequest();
        var responseBuilder = new StringBuilder();
        var parser = new SseParser();
        while (!operation.isDone)
        {
            ProcessSseChunks(downloadHandler, parser, responseBuilder, onTokenReceived, OnWeakPointHit);
            await UniTask.Yield();
        }
        ProcessSseChunks(downloadHandler, parser, responseBuilder, onTokenReceived, OnWeakPointHit);
        parser.Complete(responseBuilder, onTokenReceived, OnWeakPointHit);

        if (request.result == UnityWebRequest.Result.Success)
            return new LlmStreamResult
            {
                Response = responseBuilder.ToString(),
                ErrorCode = parser.ErrorCode
            };

        throw new LlmRequestException(
            request.error ?? ERROR_MESSAGE,
            parser.ServerMessage,
            parser.ErrorCode,
            request.responseCode);
    }

    private static void ProcessSseChunks(
        SseDownloadHandler downloadHandler,
        SseParser parser,
        StringBuilder responseBuilder,
        Action<string> onTokenReceived,
        Action onWeakPointHit)
    {
        while (downloadHandler.TryDequeue(out var chunk))
            parser.Append(chunk, responseBuilder, onTokenReceived, onWeakPointHit);
    }
}

internal sealed class SseDownloadHandler : DownloadHandlerScript
{
    private readonly Queue<byte[]> _chunks = new();
    private readonly object _lock = new();

    protected override bool ReceiveData(byte[] data, int dataLength)
    {
        var chunk = new byte[dataLength];
        Buffer.BlockCopy(data, 0, chunk, 0, dataLength);
        lock (_lock)
            _chunks.Enqueue(chunk);
        return true;
    }

    public bool TryDequeue(out byte[] chunk)
    {
        lock (_lock)
        {
            if (_chunks.Count == 0)
            {
                chunk = null;
                return false;
            }

            chunk = _chunks.Dequeue();
            return true;
        }
    }
}

internal sealed class SseParser
{
    private readonly List<byte> _lineBytes = new();
    private readonly StringBuilder _eventData = new();
    private readonly StringBuilder _serverMessage = new();
    private string _eventName;
    private bool _weakPointHitRaised;
    public string ServerMessage => _serverMessage.ToString();
    public string ErrorCode { get; private set; }

    public void Append(byte[] chunk, StringBuilder responseBuilder, Action<string> onTokenReceived, Action onWeakPointHit)
    {
        foreach (var value in chunk)
        {
            if (value == (byte)'\n')
            {
                ProcessLine(Encoding.UTF8.GetString(_lineBytes.ToArray()), responseBuilder, onTokenReceived, onWeakPointHit);
                _lineBytes.Clear();
            }
            else if (value != (byte)'\r')
            {
                _lineBytes.Add(value);
            }
        }
    }

    public void Complete(StringBuilder responseBuilder, Action<string> onTokenReceived, Action onWeakPointHit)
    {
        if (_lineBytes.Count > 0)
        {
            ProcessLine(Encoding.UTF8.GetString(_lineBytes.ToArray()), responseBuilder, onTokenReceived, onWeakPointHit);
            _lineBytes.Clear();
        }

        if (_eventData.Length > 0)
            ProcessEvent(responseBuilder, onTokenReceived, onWeakPointHit);
    }

    private void ProcessLine(string line, StringBuilder responseBuilder, Action<string> onTokenReceived, Action onWeakPointHit)
    {
        if (string.IsNullOrEmpty(line))
        {
            ProcessEvent(responseBuilder, onTokenReceived, onWeakPointHit);
            return;
        }

        if (line.StartsWith("event:", StringComparison.Ordinal))
        {
            _eventName = line.Substring("event:".Length).Trim();
            return;
        }

        if (line.StartsWith("data:", StringComparison.Ordinal))
        {
            if (_eventData.Length > 0)
                _eventData.Append('\n');
            _eventData.Append(line.Substring("data:".Length).Trim());
        }
    }

    private void ProcessEvent(StringBuilder responseBuilder, Action<string> onTokenReceived, Action onWeakPointHit)
    {
        if (_eventData.Length == 0)
            return;

        var data = _eventData.ToString();
        _eventData.Clear();
        if (_serverMessage.Length > 0)
            _serverMessage.Append('\n');
        _serverMessage.Append(data);

        if (string.IsNullOrEmpty(_eventName))
            return;

        var eventName = _eventName;
        _eventName = null;

        if (data == "[DONE]" || string.Equals(eventName, "done", StringComparison.OrdinalIgnoreCase))
            return;

        if (string.Equals(eventName, "meta", StringComparison.OrdinalIgnoreCase))
        {
            var streamEvent = JsonUtility.FromJson<LlmStreamEvent>(data);
            ErrorCode = FirstNonEmpty(streamEvent.errorCode, streamEvent.code);
            if (streamEvent.weakPointHit && !_weakPointHitRaised)
            {
                _weakPointHitRaised = true;
                onWeakPointHit?.Invoke();
            }
            return;
        }

        var token = ExtractToken(data);
        var errorEvent = JsonUtility.FromJson<LlmStreamEvent>(data);
        ErrorCode = FirstNonEmpty(ErrorCode, errorEvent.errorCode, errorEvent.code);
        if (string.IsNullOrEmpty(token))
            return;

        responseBuilder.Append(token);
        onTokenReceived?.Invoke(token);
    }

    private static string FirstNonEmpty(params string[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrEmpty(value))
                return value;
        }

        return null;
    }

    private static string ExtractToken(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return string.Empty;

        var streamEvent = JsonUtility.FromJson<LlmStreamEvent>(data);
        if (!string.IsNullOrEmpty(streamEvent.text))
            return streamEvent.text;
        if (!string.IsNullOrEmpty(streamEvent.token))
            return streamEvent.token;

        return string.Empty;
    }
}

internal struct LlmStreamResult
{
    public string Response;
    public string ErrorCode;
}

internal sealed class LlmRequestException : Exception
{
    public LlmRequestException(string message, string serverMessage, string errorCode, long httpStatusCode)
        : base(message)
    {
        ServerMessage = string.IsNullOrWhiteSpace(serverMessage) ? message : serverMessage;
        ErrorCode = errorCode;
        HttpStatusCode = httpStatusCode;
    }

    public string ServerMessage { get; }
    public string ErrorCode { get; }
    public long HttpStatusCode { get; }
}

public struct LLMRequest
{
    public string npcName;
    public string npcSystem;
    public string question;
    public string summary;
    public List<ChatRequestItem> history;
}

[Serializable]
public struct ChatRequestItem
{
    public string role;
    public string content;
}

[Serializable]
public class CompactHistoryRequest
{
    public string npcName;
    public List<CompactHistoryItem> history;
}

[Serializable]
public struct CompactHistoryItem
{
    public string role;
    public string content;
}

[Serializable]
public struct CompactHistoryResponse
{
    public string summary;
}

[Serializable]
public struct CompactHistoryStringResponse
{
    public string value;
}

public struct LLMResponse
{
    public string response;
    public Status status;
    public enum Status
    {
        Success,
        Error
    }
}

public struct LlmApiResponse
{
    public string answer;
    public bool weakPointHit;
}

public struct LlmStreamEvent
{
    public string text;
    public string token;
    public bool weakPointHit;
    public string errorCode;
    public string code;
}
using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class LLMService : LogicOnlyService<LLMService>
{
    private const string BASE_URL = "https://rag-jkoh.onrender.com/api/query/ask";
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
        LLMRequest requestData = new()
        {
            npcName = npcName,
            npcSystem = npcSystem,
            question = question
        };
        _chatHistoryService?.PushChatItem("player", question);

        string jsonData = JsonUtility.ToJson(requestData);
        try
        {
            Debug.Log($"LLMService: Sending request to LLM API with data: {jsonData}");
#if BENCHMARK_TIME
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
#else
#endif
            string response = await PostRequestAsync(BASE_URL, jsonData, onTokenReceived);
#if BENCHMARK_TIME
            stopwatch.Stop();
            Debug.Log($"LLMService: LLM API call time: {stopwatch.ElapsedMilliseconds} ms");
            _fileLogService?.LogBenchmarkTime(stopwatch.ElapsedMilliseconds);
#else
#endif
            _chatHistoryService?.PushChatItem(npcName, response);
            // This will cause a DivideByZeroException
            return new LLMResponse
            {
                response = response,
                status = LLMResponse.Status.Success
            };
        }
        catch (Exception ex)
        {
            Debug.LogError($"LLMService: Error calling LLM API: {ex.Message}");
            return new LLMResponse
            {
                response = ex.Message,
                status = LLMResponse.Status.Error
            };
        }
    }

    private async UniTask<string> PostRequestAsync(string url, string json, Action<string> onTokenReceived)
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
            return responseBuilder.ToString();
        else
            throw new InvalidOperationException(request.error ?? ERROR_MESSAGE);
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
    private bool _weakPointHitRaised;

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

        if (line.StartsWith("data:", StringComparison.Ordinal))
        {
            if (_eventData.Length > 0)
                _eventData.Append('\n');
            _eventData.Append(line.Substring("data:".Length).TrimStart());
        }
    }

    private void ProcessEvent(StringBuilder responseBuilder, Action<string> onTokenReceived, Action onWeakPointHit)
    {
        if (_eventData.Length == 0)
            return;

        var data = _eventData.ToString();
        _eventData.Clear();
        if (data == "[DONE]")
            return;

        var streamEvent = JsonUtility.FromJson<LlmStreamEvent>(data);
        if (streamEvent.weakPointHit && !_weakPointHitRaised)
        {
            _weakPointHitRaised = true;
            onWeakPointHit?.Invoke();
        }
        if (string.IsNullOrEmpty(streamEvent.token))
            return;

        responseBuilder.Append(streamEvent.token);
        onTokenReceived?.Invoke(streamEvent.token);
    }
}

public struct LLMRequest
{
    public string npcName;
    public string npcSystem;
    public string question;
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
    public string token;
    public bool weakPointHit;
}
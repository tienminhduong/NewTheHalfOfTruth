using System;
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

    public async UniTask<LLMResponse> CallLLMAsync(string npcName = "", string npcSystem = "", string question = "")
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
            string response = await PostRequestAsync(BASE_URL, jsonData);
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

    private async UniTask<string> PostRequestAsync(string url, string json)
    {
        using var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        var operation = request.SendWebRequest();
        while (!operation.isDone)
        {
            await UniTask.Yield();
        }

        if (request.result == UnityWebRequest.Result.Success)
        {
            var apiResponse = JsonUtility.FromJson<LlmApiResponse>(request.downloadHandler.text);
            if (apiResponse.weakPointHit)
                OnWeakPointHit?.Invoke();
            return apiResponse.answer;
        }
        else
        {
            Debug.LogError($"Error: {request.error}");
            return ERROR_MESSAGE;
        }
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
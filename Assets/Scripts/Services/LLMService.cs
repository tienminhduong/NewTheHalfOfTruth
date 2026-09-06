using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class LLMService : LogicOnlyService
{
    private const string BASE_URL = "https://rag-3-4lv0.onrender.com/api/query/ask";
    public override void Awake()
    {
        ServiceLocator.RegisterService(this);
    }

    public override void OnDestroy()
    {
        ServiceLocator.UnregisterService(this);
    }

    public async UniTask<LLMResponse> CallLLMAsync(string npcName = "", string npcSystem = "", string question = "")
    {
        LLMRequest requestData = new()
        {
            npcName = npcName,
            npcSystem = npcSystem,
            question = question
        };

        string jsonData = JsonUtility.ToJson(requestData);
        try
        {
            Debug.Log($"LLMService: Sending request to LLM API with data: {jsonData}");
            string response = await PostRequestAsync(BASE_URL, jsonData);
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
            return apiResponse.answer;
        }
        else
        {
            Debug.LogError($"Error: {request.error}");
            return null;
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
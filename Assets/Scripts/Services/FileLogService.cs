using System;
using System.Globalization;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public sealed class FileLogService : LogicOnlyService<FileLogService>
{
    private const string LogFileName = "game.log";
    private const string SessionKeyPlayerPrefsKey = "FileLogService.SessionKey";
    private const string GoogleFormResponseUrl = "https://docs.google.com/forms/d/e/1FAIpQLSdPEzQrAuognEcZLi7KGNzfjALBexlPVl887V4IQBg4cf_qfg/formResponse";
    private const string GoogleFormSessionEntry = "entry.2070173541";
    private const string GoogleFormLogEntry = "entry.207076898";

    private string _logFilePath;
    private readonly object _writeLock = new();
    private bool _automaticUploadStarted;
    public string SessionKey { get; private set; }

    public override void Awake()
    {
        base.Awake();

        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var logsDirectory = Path.Combine(projectRoot, "Logs");

        Directory.CreateDirectory(logsDirectory);
        _logFilePath = Path.Combine(logsDirectory, LogFileName);

    }

    public void EnsureSessionKey()
    {
        if (!string.IsNullOrEmpty(SessionKey))
            return;

        SessionKey = PlayerPrefs.GetString(SessionKeyPlayerPrefsKey, string.Empty);
        if (!string.IsNullOrEmpty(SessionKey))
        {
            Debug.Log($"FileLogService: Resuming session {SessionKey}");
            return;
        }

        SessionKey = Guid.NewGuid().ToString("N");
        PlayerPrefs.SetString(SessionKeyPlayerPrefsKey, SessionKey);
        PlayerPrefs.Save();
        Debug.Log($"FileLogService: Started session {SessionKey}");
    }

    public void LogBenchmarkTime(long elapsedMilliseconds)
    {
        Write("Info", $"LLM API call time: {elapsedMilliseconds} ms");
    }

    public void LogConversation(string npcName, string question, string response, long elapsedMilliseconds, string level = "Chat")
    {
        Write(level, $"NpcName: {npcName} | ResponseTime: {elapsedMilliseconds} ms | Question: {question} | Response: {response}");
    }

    public void LogToken(string token)
    {
        // Write("Token", token);
    }

    public async UniTask<bool> UploadLogToGoogleFormAsync()
    {
        if (_automaticUploadStarted)
            return false;

        if (string.IsNullOrEmpty(SessionKey))
        {
            Debug.LogWarning("FileLogService: No active game session. Skipping log upload.");
            return false;
        }

        _automaticUploadStarted = true;
        return await UploadLogFileToGoogleFormAsync(_logFilePath, SessionKey);
    }

    public void UploadLogOnApplicationQuit()
    {
        UploadLogToGoogleFormAsync().Forget();
    }

    public static async UniTask<bool> UploadLogFileToGoogleFormAsync(string logFilePath, string sessionKey = null)
    {
        string log;
        try
        {
            if (string.IsNullOrEmpty(logFilePath) || !File.Exists(logFilePath))
            {
                Debug.LogWarning("FileLogService: No log file is available to upload.");
                return false;
            }

            log = File.ReadAllText(logFilePath, Encoding.UTF8).TrimEnd('\r', '\n');
        }
        catch (Exception exception)
        {
            Debug.LogError($"FileLogService: Could not read log file for upload: {exception.Message}");
            return false;
        }

        var form = new WWWForm();
        form.AddField(GoogleFormSessionEntry, sessionKey ?? PlayerPrefs.GetString(SessionKeyPlayerPrefsKey, string.Empty));
        form.AddField(GoogleFormLogEntry, log);
        using var request = UnityWebRequest.Post(GoogleFormResponseUrl, form);
        try
        {
            await request.SendWebRequest().ToUniTask();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"FileLogService: Could not upload log to Google Forms: {request.error}");
                return false;
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"FileLogService: Google Forms upload failed: {exception.Message}");
            return false;
        }

        Debug.Log("FileLogService: Log uploaded to Google Forms.");
        return true;
    }

    private void Write(string level, string message)
    {
        if (string.IsNullOrEmpty(_logFilePath))
            return;

        var timestamp = DateTime.Now.ToString("hh:mm:ss dd/MM/yy", CultureInfo.InvariantCulture);
        var cleanMessage = message.Replace("\r", "\\r").Replace("\n", "\\n");
        var line = $"[{timestamp}] [{level}] [{cleanMessage}]";

        try
        {
            lock (_writeLock)
            {
                File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"FileLogService: Could not write log file: {exception.Message}");
        }
    }
}

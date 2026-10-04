using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public sealed class FileLogService : LogicOnlyService<FileLogService>
{
    private const string LogFileName = "game.log";

    private string _logFilePath;
    private readonly object _writeLock = new();

    public override void Awake()
    {
        base.Awake();

        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var logsDirectory = Path.Combine(projectRoot, "Logs");

        Directory.CreateDirectory(logsDirectory);
        _logFilePath = Path.Combine(logsDirectory, LogFileName);
    }

    public void LogBenchmarkTime(long elapsedMilliseconds)
    {
        Write("Info", $"LLM API call time: {elapsedMilliseconds} ms");
    }

    public void LogConversation(string apiUrl, string question, string response, string level = "Chat")
    {
        Write(level, $"API: {apiUrl} | Question: {question} | Response: {response}");
    }

    public void LogToken(string token)
    {
        // Write("Token", token);
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

using UnityEditor;

[InitializeOnLoad]
internal static class PlayModeLogUploader
{
    static PlayModeLogUploader()
    {
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        switch (state)
        {
            case PlayModeStateChange.ExitingPlayMode:
                ServiceLocator.TryOrWaitForService<FileLogService>(HandleFileLogService);
                break;
            case PlayModeStateChange.EnteredEditMode:
                ServiceLocator.RemoveServiceCallback<FileLogService>(HandleFileLogService);
                break;
        }
    }

    private static void HandleFileLogService(IService service)
    {
        ServiceLocator.RemoveServiceCallback<FileLogService>(HandleFileLogService);

        if (service is FileLogService fileLogService)
            fileLogService.UploadLogOnApplicationQuit();
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class DeveloperToolsWindow : EditorWindow
{
    private const string WindowTitle = "Developer Tools";
    private const string TabPreferenceKey = "DeveloperToolsWindow.SelectedTab";
    private const string ScenesFolder = "Assets/Scenes";
    private const string LogFileName = "game.log";

    private static readonly string[] TabNames = { "Scenes", "Persistence", "Logs" };

    private int _selectedTab;
    private string _sceneSearch = string.Empty;
    private string[] _scenePaths = Array.Empty<string>();
    private int _selectedSceneIndex = -1;
    private Vector2 _sceneScrollPosition;

    private DependencyInitializer _initializer;
    private SerializedObject _initializerSerializedObject;
    private SerializedProperty _loadFromPrefsProperty;

    private readonly List<FileInfo> _logFiles = new();
    private int _selectedLogIndex = -1;
    private Vector2 _logScrollPosition;
    private string _logError;
    private string _logUploadStatus;

    [MenuItem("Tools/Developer Tools")]
    public static void ShowWindow()
    {
        GetWindow<DeveloperToolsWindow>(WindowTitle);
    }

    private void OnEnable()
    {
        _selectedTab = Mathf.Clamp(EditorPrefs.GetInt(TabPreferenceKey, 0), 0, TabNames.Length - 1);
        RefreshScenes();
        RefreshLogs();
        FindLoadedInitializer();
    }

    private void OnDisable()
    {
        EditorPrefs.SetInt(TabPreferenceKey, _selectedTab);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(4);
        _selectedTab = GUILayout.Toolbar(_selectedTab, TabNames);
        EditorGUILayout.Space(8);

        switch (_selectedTab)
        {
            case 0:
                DrawScenesTab();
                break;
            case 1:
                DrawPersistenceTab();
                break;
            case 2:
                DrawLogsTab();
                break;
        }
    }

    private void DrawScenesTab()
    {
        EditorGUILayout.LabelField("Scenes", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select a scene from Assets/Scenes and open it in the Unity editor.", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        _sceneSearch = EditorGUILayout.TextField("Search", _sceneSearch);
        if (GUILayout.Button("Refresh", GUILayout.Width(75)))
            RefreshScenes();
        EditorGUILayout.EndHorizontal();

        var visibleSceneIndices = GetVisibleSceneIndices();
        if (visibleSceneIndices.Count == 0)
        {
            EditorGUILayout.HelpBox("No matching scenes were found.", MessageType.Warning);
            return;
        }

        _sceneScrollPosition = EditorGUILayout.BeginScrollView(_sceneScrollPosition, GUILayout.MinHeight(100));
        foreach (var sceneIndex in visibleSceneIndices)
        {
            var scenePath = _scenePaths[sceneIndex];
            var sceneName = Path.GetFileNameWithoutExtension(scenePath);
            var isSelected = sceneIndex == _selectedSceneIndex;
            if (GUILayout.Toggle(isSelected, sceneName, "Button"))
                _selectedSceneIndex = sceneIndex;
        }
        EditorGUILayout.EndScrollView();

        using (new EditorGUI.DisabledScope(_selectedSceneIndex < 0 || _selectedSceneIndex >= _scenePaths.Length))
        {
            if (GUILayout.Button("Open Selected Scene", GUILayout.Height(28)))
                OpenSelectedScene();
        }
    }

    private void DrawPersistenceTab()
    {
        EditorGUILayout.LabelField("Persistence", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This changes the serialized DependencyInitializer setting. It affects services created during the next initialization.", MessageType.Info);

        var selectedInitializer = (DependencyInitializer)EditorGUILayout.ObjectField(
            "Dependency Initializer",
            _initializer,
            typeof(DependencyInitializer),
            true);

        if (selectedInitializer != _initializer)
        {
            _initializer = selectedInitializer;
            RebuildInitializerSerializedState();
        }

        if (_initializer == null)
        {
            if (GUILayout.Button("Find Loaded Initializer"))
                FindLoadedInitializer();

            EditorGUILayout.HelpBox("No DependencyInitializer is loaded. Open the owning scene or assign the component above.", MessageType.Warning);
            return;
        }

        if (_loadFromPrefsProperty == null)
        {
            EditorGUILayout.HelpBox("The selected initializer does not expose the _loadFromPrefs serialized field.", MessageType.Error);
            return;
        }

        _initializerSerializedObject.Update();
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(_loadFromPrefsProperty, new GUIContent("Load From PlayerPrefs"));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_initializer, "Change Load From PlayerPrefs");
            _initializerSerializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(_initializer);

            if (!EditorApplication.isPlaying)
                EditorSceneManager.MarkSceneDirty(_initializer.gameObject.scene);
        }

        if (EditorApplication.isPlaying)
            EditorGUILayout.HelpBox("Runtime services have already been initialized. Restart Play Mode to apply this setting.", MessageType.Warning);
    }

    private void DrawLogsTab()
    {
        EditorGUILayout.LabelField("Logs", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Folder", GetLogsDirectory());

        if (GUILayout.Button("Refresh Logs", GUILayout.Width(100)))
            RefreshLogs();

        if (!string.IsNullOrEmpty(_logError))
            EditorGUILayout.HelpBox(_logError, MessageType.Error);
        if (!string.IsNullOrEmpty(_logUploadStatus))
            EditorGUILayout.HelpBox(_logUploadStatus, MessageType.Info);

        using (new EditorGUI.DisabledScope(!File.Exists(Path.Combine(GetLogsDirectory(), LogFileName))))
        {
            if (GUILayout.Button("Upload Current Log to Google Form"))
                UploadCurrentLog();
        }

        if (_logFiles.Count == 0)
        {
            EditorGUILayout.HelpBox("No log files were found.", MessageType.Info);
            return;
        }

        _logScrollPosition = EditorGUILayout.BeginScrollView(_logScrollPosition, GUILayout.MinHeight(100));
        for (var index = 0; index < _logFiles.Count; index++)
        {
            var file = _logFiles[index];
            var label = $"{file.Name}   {FormatFileSize(file.Length)}   {file.LastWriteTime:yyyy-MM-dd HH:mm}";
            if (GUILayout.Toggle(index == _selectedLogIndex, label, "Button"))
                _selectedLogIndex = index;
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(!HasSelectedLog()))
        {
            if (GUILayout.Button("Open Selected Log"))
                OpenSelectedLog();
            if (GUILayout.Button("Delete Selected Log"))
                DeleteSelectedLog();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void RefreshScenes()
    {
        _scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { ScenesFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.StartsWith(ScenesFolder + "/", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _selectedSceneIndex = _scenePaths.Length == 0 ? -1 : Mathf.Clamp(_selectedSceneIndex, 0, _scenePaths.Length - 1);
        Repaint();
    }

    private void OpenSelectedScene()
    {
        if (_selectedSceneIndex < 0 || _selectedSceneIndex >= _scenePaths.Length)
            return;

        try
        {
            EditorSceneManager.OpenScene(_scenePaths[_selectedSceneIndex], OpenSceneMode.Single);
        }
        catch (Exception exception)
        {
            EditorUtility.DisplayDialog("Could Not Open Scene", exception.Message, "OK");
        }
    }

    private List<int> GetVisibleSceneIndices()
    {
        var indices = new List<int>();
        for (var index = 0; index < _scenePaths.Length; index++)
        {
            var sceneName = Path.GetFileNameWithoutExtension(_scenePaths[index]);
            if (string.IsNullOrWhiteSpace(_sceneSearch) || sceneName.IndexOf(_sceneSearch, StringComparison.OrdinalIgnoreCase) >= 0)
                indices.Add(index);
        }
        return indices;
    }

    private void FindLoadedInitializer()
    {
        var initializers = FindObjectsByType<DependencyInitializer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        _initializer = initializers.FirstOrDefault();
        RebuildInitializerSerializedState();
        Repaint();
    }

    private void RebuildInitializerSerializedState()
    {
        _initializerSerializedObject = null;
        _loadFromPrefsProperty = null;

        if (_initializer == null)
            return;

        _initializerSerializedObject = new SerializedObject(_initializer);
        _loadFromPrefsProperty = _initializerSerializedObject.FindProperty("_loadFromPrefs");
    }

    private void RefreshLogs()
    {
        _logFiles.Clear();
        _logError = null;
        var logsDirectory = GetLogsDirectory();

        try
        {
            if (Directory.Exists(logsDirectory))
            {
                var logFile = new FileInfo(Path.Combine(logsDirectory, LogFileName));
                if (logFile.Exists)
                    _logFiles.Add(logFile);
            }
        }
        catch (Exception exception)
        {
            _logError = $"Could not read the Logs folder: {exception.Message}";
        }

        _selectedLogIndex = _logFiles.Count == 0 ? -1 : Mathf.Clamp(_selectedLogIndex, 0, _logFiles.Count - 1);
        Repaint();
    }

    private void OpenSelectedLog()
    {
        if (!TryGetSelectedLog(out var file) || !IsInsideLogsDirectory(file.FullName))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = file.FullName,
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            _logError = $"Could not open {file.Name}: {exception.Message}";
        }
    }

    private void DeleteSelectedLog()
    {
        if (!TryGetSelectedLog(out var file) || !IsInsideLogsDirectory(file.FullName))
            return;

        if (EditorUtility.DisplayDialog("Delete Log", $"Delete {file.Name}?", "Delete", "Cancel"))
        {
            try
            {
                file.Delete();
                RefreshLogs();
            }
            catch (Exception exception)
            {
                _logError = $"Could not delete {file.Name}: {exception.Message}";
            }
        }
    }

    private void UploadCurrentLog()
    {
        _logUploadStatus = "Uploading current log...";
        _logError = null;
        Repaint();

        FileLogService.UploadLogFileToGoogleFormAsync(Path.Combine(GetLogsDirectory(), LogFileName))
            .ContinueWith(uploaded =>
            {
                _logUploadStatus = uploaded
                    ? "Current log uploaded successfully."
                    : "Current log upload failed. Check the Console for details.";
                Repaint();
            })
            .Forget();
    }

    private bool TryGetSelectedLog(out FileInfo file)
    {
        file = null;
        if (!HasSelectedLog())
            return false;

        file = _logFiles[_selectedLogIndex];
        return file.Exists;
    }

    private bool HasSelectedLog()
    {
        return _selectedLogIndex >= 0 && _selectedLogIndex < _logFiles.Count;
    }

    private static string GetLogsDirectory()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
    }

    private static bool IsInsideLogsDirectory(string path)
    {
        var logsDirectory = Path.GetFullPath(GetLogsDirectory()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(path);
        return fullPath.StartsWith(logsDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024f:0.0} KB";
        return $"{bytes / (1024f * 1024f):0.0} MB";
    }
}

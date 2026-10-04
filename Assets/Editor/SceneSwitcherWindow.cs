using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class SceneSwitcherWindow : EditorWindow
{
    private const string ScenesFolder = "Assets/Scenes";

    private readonly List<string> scenePaths = new();
    private DropdownField sceneDropdown;
    private Button loadButton;
    private Label statusLabel;

    [MenuItem("Tools/Developer/Scene Switcher")]
    private static void Open()
    {
        SceneSwitcherWindow window = GetWindow<SceneSwitcherWindow>();
        window.titleContent = new GUIContent("Dev: Scene Switcher");
        window.minSize = new Vector2(300f, 48f);
        window.maxSize = new Vector2(900f, 140f);
        window.Show();
    }

    private void CreateGUI()
    {
        rootVisualElement.style.paddingLeft = 6f;
        rootVisualElement.style.paddingRight = 6f;
        rootVisualElement.style.paddingTop = 6f;
        rootVisualElement.style.paddingBottom = 6f;

        VisualElement controls = new();
        controls.style.flexDirection = FlexDirection.Row;
        controls.style.alignItems = Align.Center;

        sceneDropdown = new DropdownField("Scene", new List<string> { "No scenes found" }, 0);
        sceneDropdown.style.flexGrow = 1f;
        sceneDropdown.RegisterValueChangedCallback(_ => UpdateLoadButton());

        Button refreshButton = new(RefreshScenes)
        {
            text = "Refresh",
            tooltip = "Refresh scenes from Assets/Scenes"
        };
        refreshButton.style.marginLeft = 4f;

        loadButton = new Button(LoadSelectedScene)
        {
            text = "Load Scene"
        };
        loadButton.style.marginLeft = 4f;

        controls.Add(sceneDropdown);
        controls.Add(refreshButton);
        controls.Add(loadButton);
        rootVisualElement.Add(controls);

        statusLabel = new Label();
        statusLabel.style.paddingTop = 4f;
        rootVisualElement.Add(statusLabel);

        RefreshScenes();
    }

    private void RefreshScenes()
    {
        string previousPath = GetSelectedScenePath();
        scenePaths.Clear();

        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { ScenesFolder });
        foreach (string sceneGuid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid).Replace('\\', '/');
            if (scenePath.StartsWith(ScenesFolder + "/", StringComparison.OrdinalIgnoreCase))
            {
                scenePaths.Add(scenePath);
            }
        }

        scenePaths.Sort(StringComparer.OrdinalIgnoreCase);
        List<string> sceneNames = new();
        foreach (string scenePath in scenePaths)
        {
            string relativePath = scenePath.Substring((ScenesFolder + "/").Length);
            sceneNames.Add(Path.ChangeExtension(relativePath, null).Replace('\\', '/'));
        }

        if (sceneNames.Count == 0)
        {
            sceneNames.Add("No scenes found");
        }

        sceneDropdown.choices = sceneNames;
        int selectedIndex = previousPath == null ? 0 : scenePaths.IndexOf(previousPath);
        sceneDropdown.index = selectedIndex >= 0 ? selectedIndex : 0;
        statusLabel.text = scenePaths.Count == 1
            ? "1 scene"
            : $"{scenePaths.Count} scenes";
        UpdateLoadButton();
    }

    private string GetSelectedScenePath()
    {
        if (sceneDropdown == null || sceneDropdown.index < 0 || sceneDropdown.index >= scenePaths.Count)
        {
            return null;
        }

        return scenePaths[sceneDropdown.index];
    }

    private void UpdateLoadButton()
    {
        loadButton?.SetEnabled(GetSelectedScenePath() != null);
    }

    private void LoadSelectedScene()
    {
        string scenePath = GetSelectedScenePath();
        if (scenePath == null || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        statusLabel.text = $"Loaded {scenePath}";
    }
}
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class SpriteAnimationGenerator : EditorWindow
{
    private const string PREFS_KEY = "SpriteAnimationGenerator_Settings";

    [System.Serializable]
    public class AnimationEntry
    {
        public string animationName = "NewAnimation";
        public int startFrame = 1;
        public int endFrame = 10;
        public float frameRate = 12f;
        public bool loopTime = true;
    }

    [System.Serializable]
    private class WindowSettingsData
    {
        public string texturePath = "";
        public string folderPath = "";
        public List<AnimationEntry> animationList = new List<AnimationEntry>();
    }

    private Texture2D sourceTexture;
    private DefaultAsset saveFolder;
    private List<AnimationEntry> animationList = new List<AnimationEntry>();
    private Vector2 scrollPos;

    // Cache of loaded sprites indexed by frame number
    private Dictionary<int, Sprite> spriteFrameMap = new Dictionary<int, Sprite>();
    private string detectedPrefix = "";

    [MenuItem("Tools/2D/Sprite Animation Generator")]
    public static void ShowWindow()
    {
        GetWindow<SpriteAnimationGenerator>("Sprite Anim Generator");
    }

    private void OnEnable()
    {
        LoadSettings();

        if (animationList.Count == 0)
        {
            animationList.Add(new AnimationEntry { animationName = "Idle", startFrame = 1, endFrame = 10 });
            animationList.Add(new AnimationEntry { animationName = "Walk", startFrame = 20, endFrame = 25 });
        }

        if (sourceTexture != null)
        {
            ParseSprites();
        }
    }

    private void OnDisable()
    {
        SaveSettings();
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.LabelField("Source & Output Settings", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        sourceTexture = (Texture2D)EditorGUILayout.ObjectField("Source Texture", sourceTexture, typeof(Texture2D), false);
        if (EditorGUI.EndChangeCheck())
        {
            if (sourceTexture != null)
            {
                ParseSprites();
            }
            else
            {
                spriteFrameMap.Clear();
                detectedPrefix = "";
            }
            SaveSettings();
        }

        EditorGUI.BeginChangeCheck();
        saveFolder = (DefaultAsset)EditorGUILayout.ObjectField("Save Folder", saveFolder, typeof(DefaultAsset), false);
        if (EditorGUI.EndChangeCheck())
        {
            SaveSettings();
        }

        if (sourceTexture != null && spriteFrameMap.Count > 0)
        {
            EditorGUILayout.HelpBox($"Loaded {spriteFrameMap.Count} sliced sprites.\nDetected Prefix: '{detectedPrefix}'\nIndex Range: {spriteFrameMap.Keys.Min()} to {spriteFrameMap.Keys.Max()}", MessageType.Info);
        }
        else if (sourceTexture != null)
        {
            EditorGUILayout.HelpBox("No sliced sprites found. Make sure Sprite Mode is set to 'Multiple' and sprites are sliced in Sprite Editor.", MessageType.Warning);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Animation Clips Setup", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        for (int i = 0; i < animationList.Count; i++)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            animationList[i].animationName = EditorGUILayout.TextField("Name", animationList[i].animationName);

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                animationList.RemoveAt(i);
                SaveSettings();
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            animationList[i].startFrame = EditorGUILayout.IntField("Start Frame", animationList[i].startFrame);
            animationList[i].endFrame = EditorGUILayout.IntField("End Frame", animationList[i].endFrame);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            animationList[i].frameRate = EditorGUILayout.FloatField("Frame Rate (fps)", animationList[i].frameRate);
            animationList[i].loopTime = EditorGUILayout.Toggle("Loop Time", animationList[i].loopTime);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        if (EditorGUI.EndChangeCheck())
        {
            SaveSettings();
        }

        if (GUILayout.Button("+ Add Animation Entry"))
        {
            animationList.Add(new AnimationEntry());
            SaveSettings();
        }

        EditorGUILayout.Space(15);

        GUI.enabled = sourceTexture != null && saveFolder != null && spriteFrameMap.Count > 0;
        if (GUILayout.Button("Generate Animations", GUILayout.Height(35)))
        {
            GenerateAllClips();
            SaveSettings();
        }
        GUI.enabled = true;

        EditorGUILayout.EndScrollView();
    }

    private void SaveSettings()
    {
        WindowSettingsData data = new WindowSettingsData
        {
            texturePath = sourceTexture != null ? AssetDatabase.GetAssetPath(sourceTexture) : "",
            folderPath = saveFolder != null ? AssetDatabase.GetAssetPath(saveFolder) : "",
            animationList = this.animationList
        };

        string json = JsonUtility.ToJson(data);
        EditorPrefs.SetString(PREFS_KEY, json);
    }

    private void LoadSettings()
    {
        if (!EditorPrefs.HasKey(PREFS_KEY)) return;

        string json = EditorPrefs.GetString(PREFS_KEY);
        if (string.IsNullOrEmpty(json)) return;

        WindowSettingsData data = JsonUtility.FromJson<WindowSettingsData>(json);
        if (data == null) return;

        if (!string.IsNullOrEmpty(data.texturePath))
        {
            sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(data.texturePath);
        }

        if (!string.IsNullOrEmpty(data.folderPath))
        {
            saveFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(data.folderPath);
        }

        if (data.animationList != null && data.animationList.Count > 0)
        {
            animationList = data.animationList;
        }
    }

    private void ParseSprites()
    {
        spriteFrameMap.Clear();
        detectedPrefix = "";

        string path = AssetDatabase.GetAssetPath(sourceTexture);
        if (string.IsNullOrEmpty(path)) return;

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

        foreach (Object obj in assets)
        {
            if (obj is Sprite sprite)
            {
                Match match = Regex.Match(sprite.name, @"(\d+)$");
                if (match.Success && int.TryParse(match.Value, out int frameIndex))
                {
                    spriteFrameMap[frameIndex] = sprite;

                    if (string.IsNullOrEmpty(detectedPrefix))
                    {
                        detectedPrefix = sprite.name.Substring(0, match.Index);
                    }
                }
            }
        }
    }

    private void GenerateAllClips()
    {
        string folderPath = AssetDatabase.GetAssetPath(saveFolder);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            EditorUtility.DisplayDialog("Error", "Please select a valid destination folder.", "OK");
            return;
        }

        int successCount = 0;

        foreach (var entry in animationList)
        {
            if (string.IsNullOrEmpty(entry.animationName)) continue;

            List<Sprite> frames = new List<Sprite>();
            int step = entry.startFrame <= entry.endFrame ? 1 : -1;

            for (int i = entry.startFrame; step > 0 ? i <= entry.endFrame : i >= entry.endFrame; i += step)
            {
                if (spriteFrameMap.TryGetValue(i, out Sprite s))
                {
                    frames.Add(s);
                }
            }

            if (frames.Count == 0)
            {
                Debug.LogWarning($"[SpriteAnimationGenerator] No sprites found for '{entry.animationName}' between frames {entry.startFrame} and {entry.endFrame}. Skipping.");
                continue;
            }

            CreateAnimationClip(entry, frames, folderPath);
            successCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Done", $"Successfully created {successCount} animation clips!", "OK");
    }

    private void CreateAnimationClip(AnimationEntry entry, List<Sprite> frames, string targetFolder)
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = entry.frameRate;

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = entry.loopTime;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorCurveBinding curveBinding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[frames.Count];
        float frameDuration = 1f / entry.frameRate;

        for (int i = 0; i < frames.Count; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i * frameDuration,
                value = frames[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, curveBinding, keyframes);

        string outputPath = Path.Combine(targetFolder, $"{entry.animationName}.anim").Replace("\\", "/");
        outputPath = AssetDatabase.GenerateUniqueAssetPath(outputPath);

        AssetDatabase.CreateAsset(clip, outputPath);
    }
}
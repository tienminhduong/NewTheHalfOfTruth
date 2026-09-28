using System.IO;
using UnityEditor;
using UnityEngine;

public static class ScriptableObjectToJson
{
    [MenuItem("Assets/Export/To JSON", true)]
    private static bool ValidateExportJson()
    {
        return Selection.activeObject is ScriptableObject;
    }

    [MenuItem("Assets/Export/To JSON")]
    public static void ExportToJson()
    {
        ScriptableObject targetObject = Selection.activeObject as ScriptableObject;
        if (targetObject == null) return;

        // Open save file dialog
        string defaultName = $"{targetObject.name}.json";
        string path = EditorUtility.SaveFilePanel("Export ScriptableObject to JSON", Application.dataPath, defaultName, "json");

        if (string.IsNullOrEmpty(path)) return;

        // Serialize fields (prettyPrint = true for readable formatting)
        string json = JsonUtility.ToJson(targetObject, true);

        File.WriteAllText(path, json);
        AssetDatabase.Refresh();

        Debug.Log($"Successfully exported {targetObject.name} to JSON at: {path}");
    }
}
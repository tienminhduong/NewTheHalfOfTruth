using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EvidenceDatabase", menuName = "Scriptable Objects/EvidenceDatabase")]
public class EvidenceDatabase : ScriptableObject
{
    [SerializeField] private EvidenceData[] evidenceData;

    private Dictionary<string, EvidenceData> evidenceDictionary;
    public void LoadEvidenceData()
    {
        evidenceDictionary ??= new Dictionary<string, EvidenceData>();
        foreach (var evidence in evidenceData)
        {
            evidenceDictionary[evidence.key] = evidence;
        }
    }

    public EvidenceData GetEvidenceData(string key)
    {
        if (evidenceDictionary == null)
            LoadEvidenceData();

        if (evidenceDictionary.TryGetValue(key, out var evidence))
            return evidence;
        else
        {
            Debug.LogWarning($"Evidence with key '{key}' not found in the database.");
            return default;
        }
    }
}

[System.Serializable]
public struct EvidenceData
{
    public string key;
    public string description;
    public string playerGuide;
    public Sprite evidenceSprite;
    public string playerNote;
}
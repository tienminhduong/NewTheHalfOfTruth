using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EvidenceDatabase", menuName = "Scriptable Objects/EvidenceDatabase")]
public class EvidenceDatabase : ScriptableObject
{
    [SerializeField] private EvidenceData[] evidenceData;

    private Dictionary<string, EvidenceData> evidenceDictionary;
    private SavedDataService _savedDataService;

    public void SetSavedDataService(SavedDataService savedDataService)
    {
        _savedDataService = savedDataService;
        LoadEvidenceData();
        for (int i = 0; i < evidenceData.Length; i++)
        {
            var evidence = evidenceDictionary[evidenceData[i].key];
            if (_savedDataService != null)
            {
                var playerNote = _savedDataService.GetPlayerNoteForEvidence(evidence.key);
                if (playerNote != null)
                    evidence.playerNote = playerNote;
            }
        }
    }

    void OnEnable()
    {
        SavedDataService.OnEvidenceNoteSaved += HandleEvidenceDataChanged;
    }

    void OnDisable()
    {
        SavedDataService.OnEvidenceNoteSaved -= HandleEvidenceDataChanged;
    }

    public void LoadEvidenceData()
    {
        if (evidenceDictionary != null)
            return;
        evidenceDictionary = new Dictionary<string, EvidenceData>();
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

    public bool TryGetEvidenceData(string key, out EvidenceData evidence)
    {
        if (evidenceDictionary == null)
            LoadEvidenceData();

        return evidenceDictionary.TryGetValue(key, out evidence);
    }

    public void HandleEvidenceDataChanged(NotebookSavedData notebookData)
    {
        if (evidenceDictionary == null)
            LoadEvidenceData();

        if (evidenceDictionary.TryGetValue(notebookData.evidenceKey, out var evidence))
        {
            evidence.playerNote = notebookData.playerNote;
            evidenceDictionary[notebookData.evidenceKey] = evidence;
        }
    }
}

[System.Serializable]
public struct EvidenceData
{
    public string key;
    public string name;
    public string description;
    public string playerGuide;
    public Sprite evidenceSprite;
    public string playerNote;
}
using System;
using System.Collections.Generic;
using UnityEngine;

public class SavedDataService : BaseSaveLoadService<SavedDataService>
{
    private PlayerSavedData _playerSavedData;
    private const string PlayerPrefsKey = "PlayerSavedData";
    private EvidenceDatabase _evidenceDatabase;

    public static event Action<NotebookSavedData> OnEvidenceNoteSaved;
    public static event Action<InteractedNPCSavedData> OnNPCInteractedSaved;
    public static event Action<string> OnNPCInteracted;
    public static event Action<UnlockedDoorSavedData> OnDoorUnlockedSaved;

    public SavedDataService(EvidenceDatabase database, bool loadFromPrefs = true) : base(loadFromPrefs)
    {
        _evidenceDatabase = database;
        _evidenceDatabase.SetSavedDataService(this);
    }

    public void SaveNotebookData(string evidenceKey, string playerNote)
    {
        if (_evidenceDatabase != null)
        {
            if (!_evidenceDatabase.TryGetEvidenceData(evidenceKey, out var evidenceData))
            {
                Debug.LogError($"Evidence with key '{evidenceKey}' does not exist in the EvidenceDatabase.");
                return;
            }
        }
        else
        {
            Debug.LogWarning("EvidenceDatabase is not set. Skipping evidence validation.");
        }

        _notebookDataDictionary ??= new Dictionary<string, NotebookSavedData>();

        var notebookData = new NotebookSavedData
        {
            evidenceKey = evidenceKey,
            playerNote = playerNote
        };

        _notebookDataDictionary[evidenceKey] = notebookData;

        _playerSavedData.EnsureNonNullLists();
        for (int i = 0; i < _playerSavedData.notebookData.Count; i++)
        {
            if (_playerSavedData.notebookData[i].evidenceKey == evidenceKey)
            {
                _playerSavedData.notebookData[i] = notebookData;
                SaveToPref();
                OnEvidenceNoteSaved?.Invoke(notebookData);
                return;
            }
        }

        _playerSavedData.notebookData.Add(notebookData);
        SaveToPref();
        OnEvidenceNoteSaved?.Invoke(notebookData);
    }

    public void SaveInteractedNPC(string npcName)
    {
        _interactedNPCs ??= new HashSet<string>();
        OnNPCInteracted?.Invoke(npcName);

        if (_interactedNPCs.Add(npcName))
        {
            var npcData = new InteractedNPCSavedData
            {
                name = npcName
            };

            _playerSavedData.EnsureNonNullLists();
            _playerSavedData.interactedNPCs.Add(npcData);
            SaveToPref();
            OnNPCInteractedSaved?.Invoke(npcData);
        }
    }

    public void SaveUnlockedDoor(string doorName, DoorUnlockedState unlockedState)
    {
        _unlockedDoorsDictionary ??= new Dictionary<string, UnlockedDoorSavedData>();

        var doorData = new UnlockedDoorSavedData
        {
            doorName = doorName,
            unlockedState = unlockedState
        };

        _unlockedDoorsDictionary[doorName] = doorData;
        for (int i = 0; i < _playerSavedData.unlockedDoors.Count; i++)
        {
            if (_playerSavedData.unlockedDoors[i].doorName == doorName)
            {
                _playerSavedData.unlockedDoors[i] = doorData;
                SaveToPref();
                OnDoorUnlockedSaved?.Invoke(doorData);
                return;
            }
        }

        _playerSavedData.EnsureNonNullLists();
        _playerSavedData.unlockedDoors.Add(doorData);
        SaveToPref();
        OnDoorUnlockedSaved?.Invoke(doorData);
    }

    public void ClearSavedData()
    {
        _playerSavedData = new PlayerSavedData();
        _notebookDataDictionary?.Clear();
        _interactedNPCs?.Clear();
        _unlockedDoorsDictionary?.Clear();

        PlayerPrefs.DeleteKey(PlayerPrefsKey);
    }

    public string GetPlayerNoteForEvidence(string evidenceKey)
    {
        if (_notebookDataDictionary != null && _notebookDataDictionary.TryGetValue(evidenceKey, out var notebookData))
            return notebookData.playerNote;

        return null;
    }

    public string GetCustomData(string key)
    {
        if (_customDataDictionary != null && _customDataDictionary.TryGetValue(key, out var value))
            return value;

        return null;
    }

    public void SaveCustomData(string key, string value)
    {
        _customDataDictionary ??= new Dictionary<string, string>();
        _customDataDictionary[key] = value;

        _playerSavedData.EnsureNonNullLists();
        for (var index = 0; index < _playerSavedData.customData.Count; index++)
        {
            if (_playerSavedData.customData[index].key != key)
                continue;

            _playerSavedData.customData[index] = new CustomSavedData { key = key, value = value };
            SaveToPref();
            return;
        }

        _playerSavedData.customData.Add(new CustomSavedData { key = key, value = value });
        SaveToPref();
    }

    public bool HasInteractedWithNPC(string npcName)
    {
        return _interactedNPCs != null && _interactedNPCs.Contains(npcName);
    }

    public DoorUnlockedState? GetDoorUnlockedState(string doorName)
    {
        if (_unlockedDoorsDictionary != null && _unlockedDoorsDictionary.TryGetValue(doorName, out var doorData))
            return doorData.unlockedState;

        return null;
    }

    private Dictionary<string, NotebookSavedData> _notebookDataDictionary;
    private HashSet<string> _interactedNPCs;
    private Dictionary<string, UnlockedDoorSavedData> _unlockedDoorsDictionary;
    private Dictionary<string, string> _customDataDictionary;

    protected override void LoadFromPref()
    {
        string savedDataJson = _loadFromPrefs ? PlayerPrefs.GetString(PlayerPrefsKey, string.Empty) : string.Empty;
        if (!string.IsNullOrEmpty(savedDataJson))
        {
            _playerSavedData = JsonUtility.FromJson<PlayerSavedData>(savedDataJson);
            _playerSavedData.EnsureNonNullLists();

            _notebookDataDictionary ??= new Dictionary<string, NotebookSavedData>();
            foreach (var notebookData in _playerSavedData.notebookData)
            {
                _notebookDataDictionary[notebookData.evidenceKey] = notebookData;
            }

            _interactedNPCs ??= new HashSet<string>();
            foreach (var npcData in _playerSavedData.interactedNPCs)
            {
                _interactedNPCs.Add(npcData.name);
            }

            _unlockedDoorsDictionary ??= new Dictionary<string, UnlockedDoorSavedData>();
            foreach (var doorData in _playerSavedData.unlockedDoors)
            {
                _unlockedDoorsDictionary[doorData.doorName] = doorData;
            }

            _customDataDictionary ??= new Dictionary<string, string>();
            foreach (var customData in _playerSavedData.customData)
            {
                _customDataDictionary[customData.key] = customData.value;
            }
        }
        else
        {
            Debug.Log("[SavedDataService] No saved data found in PlayerPrefs. Initializing with default values.");
            _playerSavedData = new PlayerSavedData();
            _playerSavedData.EnsureNonNullLists();
        }
    }

    protected override void SaveToPref()
    {
        if (!_loadFromPrefs)
            return;

        string savedDataJson = JsonUtility.ToJson(_playerSavedData);
        PlayerPrefs.SetString(PlayerPrefsKey, savedDataJson);
        PlayerPrefs.Save();
    }

    public IEnumerable<string> GetEvidenceKeys()
    {
        if (_notebookDataDictionary != null)
        {
            foreach (var kvp in _notebookDataDictionary)
            {
                yield return kvp.Key;
            }
        }
    }

    public IEnumerable<string> GetInteractedNPCNames()
    {
        if (_interactedNPCs == null)
            yield break;

        foreach (var npcName in _interactedNPCs)
            yield return npcName;
    }
}

[Serializable]
public struct PlayerSavedData
{
    public List<NotebookSavedData> notebookData;
    public List<InteractedNPCSavedData> interactedNPCs;
    public List<UnlockedDoorSavedData> unlockedDoors;
    public List<CustomSavedData> customData;

    public void EnsureNonNullLists()
    {
        notebookData ??= new List<NotebookSavedData>();
        interactedNPCs ??= new List<InteractedNPCSavedData>();
        unlockedDoors ??= new List<UnlockedDoorSavedData>();
        customData ??= new List<CustomSavedData>();
    }
}

[Serializable]
public struct NotebookSavedData
{
    public string evidenceKey;
    public string playerNote;
}

[Serializable]
public struct InteractedNPCSavedData
{
    public string name;
}

[Serializable]
public struct UnlockedDoorSavedData
{
    public string doorName;
    public DoorUnlockedState unlockedState;
}

[Serializable]
public struct CustomSavedData
{
    public string key;
    public string value;
}

public enum DoorUnlockedState
{
    Locked,
    FirstTimeOpened,
    Opened
}
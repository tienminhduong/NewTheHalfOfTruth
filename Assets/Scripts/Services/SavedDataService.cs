using System;
using System.Collections.Generic;
using UnityEngine;

public class SavedDataService : LogicOnlyService
{
    private PlayerSavedData _playerSavedData;
    private const string PlayerPrefsKey = "PlayerSavedData";
    private EvidenceDatabase _evidenceDatabase;

    public static event Action<NotebookSavedData> OnEvidenceNoteSaved;
    public static event Action<InteractedNPCSavedData> OnNPCInteractedSaved;
    public static event Action<UnlockedDoorSavedData> OnDoorUnlockedSaved;


    public override void Awake()
    {
        LoadFromPref();
        ServiceLocator.RegisterService(this);
    }

    public override void OnDestroy()
    {
        SaveToPref();
        ServiceLocator.UnregisterService(this);
    }

    public SavedDataService(EvidenceDatabase database)
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

        for (int i = 0; i < _playerSavedData.notebookData.Count; i++)
        {
            if (_playerSavedData.notebookData[i].evidenceKey == evidenceKey)
            {
                _playerSavedData.notebookData[i] = notebookData;
                OnEvidenceNoteSaved?.Invoke(notebookData);
                return;
            }
        }

        _playerSavedData.notebookData.Add(notebookData);
        OnEvidenceNoteSaved?.Invoke(notebookData);
    }

    public void SaveInteractedNPC(string npcName)
    {
        _interactedNPCs ??= new HashSet<string>();

        if (_interactedNPCs.Add(npcName))
        {
            var npcData = new InteractedNPCSavedData
            {
                name = npcName
            };

            _playerSavedData.interactedNPCs.Add(npcData);
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
                OnDoorUnlockedSaved?.Invoke(doorData);
                return;
            }
        }

        _playerSavedData.unlockedDoors.Add(doorData);
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

    private void LoadFromPref()
    {
        string savedDataJson = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
        if (!string.IsNullOrEmpty(savedDataJson))
        {
            _playerSavedData = JsonUtility.FromJson<PlayerSavedData>(savedDataJson);

            _notebookDataDictionary = new Dictionary<string, NotebookSavedData>();
            foreach (var notebookData in _playerSavedData.notebookData)
            {
                _notebookDataDictionary[notebookData.evidenceKey] = notebookData;
            }

            _interactedNPCs = new HashSet<string>();
            foreach (var npcData in _playerSavedData.interactedNPCs)
            {
                _interactedNPCs.Add(npcData.name);
            }

            _unlockedDoorsDictionary = new Dictionary<string, UnlockedDoorSavedData>();
            foreach (var doorData in _playerSavedData.unlockedDoors)
            {
                _unlockedDoorsDictionary[doorData.doorName] = doorData;
            }
        }
        else
        {
            Debug.Log("[SavedDataService] No saved data found in PlayerPrefs. Initializing with default values.");
            _playerSavedData = new PlayerSavedData();
            _playerSavedData.EnsureNonNullLists();
        }
    }

    private void SaveToPref()
    {
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
}

[Serializable]
public struct PlayerSavedData
{
    public List<NotebookSavedData> notebookData;
    public List<InteractedNPCSavedData> interactedNPCs;
    public List<UnlockedDoorSavedData> unlockedDoors;

    public void EnsureNonNullLists()
    {
        notebookData ??= new List<NotebookSavedData>();
        interactedNPCs ??= new List<InteractedNPCSavedData>();
        unlockedDoors ??= new List<UnlockedDoorSavedData>();
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

public enum DoorUnlockedState
{
    FirstTimeOpened,
    AlreadyOpened
}
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NPCDatabase", menuName = "Scriptable Objects/NPCDatabase")]
public class NPCDatabase : ScriptableObject
{
    [SerializeField] private NPCData[] _npcs;

    private Dictionary<string, NPCData> _npcDictionary;

    public void EnsureLoadedNPCData()
    {
        if (_npcDictionary != null)
            return;

        _npcDictionary = new Dictionary<string, NPCData>();
        foreach (var npc in _npcs)
        {
            _npcDictionary[npc.npcName] = npc;
        }
    }

    public NPCData GetNPCData(string npcName)
    {
        EnsureLoadedNPCData();
        return _npcDictionary[npcName];
    }

    public bool TryGetNPCData(string npcName, out NPCData npcData)
    {
        EnsureLoadedNPCData();
        return _npcDictionary.TryGetValue(npcName, out npcData);
    }
}

[Serializable]
public struct NPCData
{
    public string npcName;
    public string npcSystem;
    public string startUpDialogue;
    public string npcDescription;
    public string npcGuideChat;
}
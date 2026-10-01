using System.Collections.Generic;
using UnityEngine;

public class DependencyInitializer : Singleton<DependencyInitializer>
{
    [SerializeField] private EvidenceDatabase _evidenceDatabase;
    [SerializeField] private NPCDatabase _npcDatabase;
    [SerializeField] private bool _loadFromPrefs = true;
    private readonly List<IService> _logicOnlyServices = new();

    protected override void Awake()
    {
        base.Awake();

        _logicOnlyServices.Add(new FileLogService());
        _logicOnlyServices.Add(new SavedDataService(_evidenceDatabase, _loadFromPrefs));
        _logicOnlyServices.Add(new ChatHistoryService(_loadFromPrefs));
        _logicOnlyServices.Add(new LLMService());

        foreach (var service in _logicOnlyServices)
            service.Awake();
    }

    private void OnDestroy()
    {
        foreach (var service in _logicOnlyServices)
            service.OnDestroy();
    }
}
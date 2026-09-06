using System.Collections.Generic;
using UnityEngine;

public class DependencyInitializer : Singleton<DependencyInitializer>
{
    [SerializeField] private EvidenceDatabase _evidenceDatabase;
    private readonly List<LogicOnlyService> _logicOnlyServices = new();

    protected override void Awake()
    {
        base.Awake();

        _logicOnlyServices.Add(new SavedDataService(_evidenceDatabase));
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
using System.Collections.Generic;
using UnityEngine;

public class PersistentObject : Singleton<PersistentObject>
{
    private readonly List<LogicOnlyService> _logicOnlyServices = new();

    override protected void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);

        _logicOnlyServices.Add(new SavedDataService());
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
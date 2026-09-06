using System.Collections.Generic;
using UnityEngine;

public class PersistentObject : Singleton<PersistentObject>
{
    override protected void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }
}
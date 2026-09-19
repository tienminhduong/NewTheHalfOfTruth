using UnityEngine;

public class MonoBehaviourService<T> : MonoBehaviour, IService where T : MonoBehaviourService<T>
{
    public virtual void Awake()
    {
        ServiceLocator.RegisterService(this as T);
    }

    public virtual void OnDestroy()
    {
        ServiceLocator.UnregisterService(this as T);
    }
}
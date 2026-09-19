public abstract class LogicOnlyService<T> : IService where T : LogicOnlyService<T>
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
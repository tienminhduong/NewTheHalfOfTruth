public abstract class BaseSaveLoadService<T> : LogicOnlyService<T> where T : BaseSaveLoadService<T>
{
    protected abstract void LoadFromPref();
    protected abstract void SaveToPref();

    protected bool _loadFromPrefs;

    public BaseSaveLoadService(bool loadFromPrefs)
    {
        _loadFromPrefs = loadFromPrefs;
    }

    override public void Awake()
    {
        if (_loadFromPrefs)
            LoadFromPref();
        base.Awake();
    }

    override public void OnDestroy()
    {
        if (_loadFromPrefs)
            SaveToPref();
        base.OnDestroy();
    }
}
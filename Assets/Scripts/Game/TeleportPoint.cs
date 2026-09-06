using UnityEngine;

public class TeleportPoint : MonoBehaviour, IIdentifiable
{
    [SerializeField] private string _id;

    public string Id => _id;

    private void Awake()
    {
        ObjectIdentifier.RegisterObject(_id, this);
    }

    private void OnDestroy()
    {
        ObjectIdentifier.UnregisterObject(_id);
    }
}
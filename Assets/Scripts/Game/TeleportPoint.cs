using UnityEngine;

public class TeleportPoint : MonoBehaviour, IIdentifiable
{
    [SerializeField] private string _id;

    public string Id => _id;
}
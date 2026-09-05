using UnityEngine;
using UnityExtensions;

[RequireComponent(typeof(SpriteRenderer))]
public class ChatNpc : Interactable, IShowThoughable
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    public override InteractableType InteractableType => InteractableType.NPC;

    public Vector3 ThoughIndicatorPosition => _spriteRenderer.GetTopCenter();

    void OnValidate()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void OnInteract()
    {
        Debug.Log("Interacted with ChatNpc:  " + gameObject.name);
        SavedDataService?.SaveInteractedNPC(gameObject.name);
    }
}

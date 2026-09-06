using UnityEngine;
using UnityExtensions;

[RequireComponent(typeof(SpriteRenderer))]
public class ChatNpc : Interactable, IShowThoughable
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private string _npcSystem;
    [SerializeField] private string _startUpDialogue;

    public override InteractableType InteractableType => InteractableType.NPC;
    public string NpcName => gameObject.name;
    public string NpcSystem => _npcSystem;
    public string StartUpDialogue => _startUpDialogue;

    public Vector3 ThoughIndicatorPosition => _spriteRenderer.GetTopCenter();

    void OnValidate()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void OnInteract()
    {
        Debug.Log("Interacted with ChatNpc:  " + gameObject.name);
        SavedDataService?.SaveInteractedNPC(gameObject.name);
        DialogueUISystem.ShowChatNpcDialogue(this);
    }

    public virtual void OnChatEnd()
    {

    }
}

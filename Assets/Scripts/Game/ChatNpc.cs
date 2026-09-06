using UnityEngine;
using UnityExtensions;

[RequireComponent(typeof(SpriteRenderer))]
public class ChatNpc : Interactable, IShowThoughable
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private NPCDatabase _npcDatabase;

    public override InteractableType InteractableType => InteractableType.NPC;
    public string NpcName => gameObject.name;
    public string NpcSystem { get; private set; } = "default";
    public string StartUpDialogue { get; private set; } = "Hello";

    public Vector3 ThoughIndicatorPosition => _spriteRenderer.GetTopCenter();

    void Awake()
    {
        if (_npcDatabase != null)
        {
            if (_npcDatabase.TryGetNPCData(NpcName, out var npcData))
            {
                NpcSystem = npcData.npcSystem;
                StartUpDialogue = npcData.startUpDialogue;
            }
            else
            {
                Debug.LogWarning($"NPC data for '{NpcName}' not found in NPCDatabase.");
            }
        }
        else
        {
            Debug.LogError("NPCDatabase is not assigned in the inspector.");
        }
    }

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

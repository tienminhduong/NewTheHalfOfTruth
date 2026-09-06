using System.Collections.Generic;
using CD;
using UnityEngine;
using UnityExtensions;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class ChatNpc : Interactable, IHuman
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private NPCDatabase _npcDatabase;

    [SerializeField] private FaceDir _initialFacingDirection = FaceDir.Right;

    [SerializeField] private List<AnimClipReference> _animRefs;
    private Animator _animator;
    private HumanAnimSystem _animSystem;

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

        if (_animator == null)
            _animator = GetComponent<Animator>();
    }

    void Start()
    {
        _animSystem = new HumanAnimSystem(_animator, _animRefs, _initialFacingDirection);
    }

    void OnValidate()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();
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

    public void RotateTo(FaceDir direction)
    {
        _animSystem.PlayAnimation(direction, HumanState.Idle);
    }

    void OnDestroy()
    {
        _animSystem?.Destroy();
    }
}

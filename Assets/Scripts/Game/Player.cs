using System.Collections.Generic;
using CD;
using UnityEngine;
using UnityExtensions;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour, IHuman
{
    [SerializeField] private List<AnimClipReference> _animRefs;
    private Animator _animator;
    private HumanAnimSystem _animSystem;

    [SerializeField] private float _moveSpeed = 5f;
    private Vector2 _direction;

    // Kinematic body moved with Rigidbody2D.Slide: casts ahead and stops at walls,
    // so the player never overlaps a collider and never gets pushed back out.
    private Rigidbody2D _rb;
    private Rigidbody2D.SlideMovement _slideMovement;

    [SerializeField] private SpriteRenderer _spriteRenderer;

    public Vector3 ThoughIndicatorPosition => _spriteRenderer.GetTopCenter();

    private InteractableService _interactableService;
    private SceneService _sceneService;
    private CD.FaceDir _faceDirOnLoad = CD.FaceDir.Down;
    private string _startPointKeyOnLoad;

    private void SetupInteractable(IService service)
    {
        if (service is not InteractableService interactableService)
        {
            Debug.LogError($"ServiceLocator returned a service of type {service.GetType()} instead of InteractableService.");
            return;
        }

        _interactableService = interactableService;
        _interactableService.RegisterPlayer(this);
    }

    private void SetupSceneService(IService service)
    {
        if (service is not SceneService sceneService)
        {
            Debug.LogError($"ServiceLocator returned a service of type {service.GetType()} instead of SceneService.");
            return;
        }

        _sceneService = sceneService;
        _startPointKeyOnLoad = null;
        if (_sceneService == null || !_sceneService.HasLoadData)
            return;

        _faceDirOnLoad = _sceneService.CurrentLoadSceneData.PlayerFacingDirection;
        _startPointKeyOnLoad = _sceneService.CurrentLoadSceneData.StartPointKey;
        ObjectIdentifier.TryOrWaitFor(_startPointKeyOnLoad, SetPositionToTeleportPoint);
    }

    void OnValidate()
    {
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _slideMovement = new Rigidbody2D.SlideMovement
        {
            maxIterations = 3,
            surfaceSlideAngle = 90f,
            gravitySlipAngle = 90f,
            surfaceUp = Vector2.up,
            surfaceAnchor = Vector2.zero, // top-down: no ground detection
            gravity = Vector2.zero,       // top-down: no gravity
            useSimulationMove = true,     // move via MovePosition so interpolation applies
        };
    }

    void Start()
    {
        gameObject.EnsureComponent(ref _animator, true);
        gameObject.EnsureComponent(ref _spriteRenderer, true);
        _animSystem = new HumanAnimSystem(_animator, _animRefs, _faceDirOnLoad);
    }

    void OnEnable()
    {
        InputHandler.Move += OnMove;
        ServiceLocator.TryOrWaitForService<InteractableService>(SetupInteractable);
        ServiceLocator.TryOrWaitForService<SceneService>(SetupSceneService);
    }

    void OnDisable()
    {
        InputHandler.Move -= OnMove;
        _interactableService?.UnregisterPlayer(this);
        ServiceLocator.RemoveServiceCallback<InteractableService>(SetupInteractable);
        ServiceLocator.RemoveServiceCallback<SceneService>(SetupSceneService);

        if (_startPointKeyOnLoad != null)
            ObjectIdentifier.RemovePendingCallback(_startPointKeyOnLoad, SetPositionToTeleportPoint);
    }

    void FixedUpdate()
    {
        if (_direction == Vector2.zero)
            return;

        _rb.Slide(_moveSpeed * _direction, Time.fixedDeltaTime, _slideMovement);
    }

    private void OnMove(Vector2 moveInput)
    {
        if (!_animSystem.IsInitialized)
            return;

        if (Mathf.Abs(moveInput.x) == Mathf.Abs(moveInput.y))
        {
            if (_direction.x != 0)
                moveInput.x = 0;
            else
                moveInput.y = 0;
        }
        else
        {
            if (Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y))
                moveInput.y = 0;
            else
                moveInput.x = 0;
        }

        _direction = moveInput.normalized;
        _animSystem.PlayAnimation(moveInput);
    }

    void OnDestroy()
    {
        _animSystem?.Destroy();
        _interactableService?.UnregisterPlayer(this);
    }

    public void RotateTo(FaceDir direction)
    {
        _animSystem.PlayAnimation(direction, HumanState.Idle);
    }

    private void SetPositionToTeleportPoint(IIdentifiable teleportPoint)
    {
        Vector3 target = teleportPoint.gameObject.transform.position;
        // Set both so the body and the rendered transform jump together (no interpolated lerp from the old spot).
        if (_rb != null)
            _rb.position = target;
        transform.position = target;
    }
}
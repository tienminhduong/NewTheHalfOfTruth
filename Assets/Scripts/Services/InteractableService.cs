using System.Collections.Generic;
using UnityEngine;

public class InteractableService : MonoBehaviour, IService
{
    [SerializeField] private float _interactionRange = 2f;
    [SerializeField] private RectTransform _interactionIndicatorCanvas;


    private readonly HashSet<Interactable> _interactables = new();
    private Interactable _currentInteractable;
    private Player _activePlayer;
    private IShowThoughable _trackingObject;

    public void RegisterPlayer(Player player) => _activePlayer = player;
    public void UnregisterPlayer(Player player) => _activePlayer = (_activePlayer == player) ? null : _activePlayer;

    public void RegisterInteractable(Interactable interactable) => _interactables.Add(interactable);
    public void UnregisterInteractable(Interactable interactable) => _interactables.Remove(interactable);

    void Start()
    {
        _interactionIndicatorCanvas.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        InputHandler.Interact += HandleInteraction;
    }

    void OnDisable()
    {
        InputHandler.Interact -= HandleInteraction;
    }


    void Update()
    {
        CheckForInteraction();
        UpdateTrackingObjectPosition();
    }

    private void CheckForInteraction()
    {
        if (_activePlayer == null)
            return;

        float lowestDistance = 0f;
        Interactable closestInteractable = null;
        foreach (var interactable in _interactables)
        {
            if (!interactable.IsInteractable)
                continue;

            float distance = Vector2.Distance(_activePlayer.transform.position, interactable.transform.position);
            if (distance <= _interactionRange)
            {
                if (closestInteractable == null || distance < lowestDistance)
                {
                    lowestDistance = distance;
                    closestInteractable = interactable;
                }
            }
        }
        if (closestInteractable != _currentInteractable)
        {
            _interactionIndicatorCanvas.gameObject.SetActive(closestInteractable != null);
            if (closestInteractable != null)
            {
                if (closestInteractable.InteractableType == InteractableType.NPC)
                    _trackingObject = closestInteractable as IShowThoughable;
                else
                    _trackingObject = _activePlayer;
            }
            _currentInteractable = closestInteractable;
        }

        if (_currentInteractable == null)
            _trackingObject = null;
    }

    private void UpdateTrackingObjectPosition()
    {
        if (_trackingObject != null)
            _interactionIndicatorCanvas.position = _trackingObject.ThoughIndicatorPosition;
    }

    private void HandleInteraction()
    {
        Debug.Log("InteractableService: HandleInteraction called.");
        _currentInteractable?.OnInteract();
    }

    public void Awake()
    {
        ServiceLocator.RegisterService(this);
    }

    public void OnDestroy()
    {
        ServiceLocator.UnregisterService(this);
    }
}

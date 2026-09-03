using System.Collections.Generic;
using UnityEngine;

public class InteractableService : MonoBehaviour, IService
{
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private RectTransform interactionIndicatorCanvas;

    private readonly HashSet<Interactable> _interactables = new();
    private Interactable _currentInteractable;
    private Player _activePlayer;

    public void RegisterPlayer(Player player) => _activePlayer = player;
    public void UnregisterPlayer(Player player) => _activePlayer = (_activePlayer == player) ? null : _activePlayer;

    public void RegisterInteractable(Interactable interactable) => _interactables.Add(interactable);
    public void UnregisterInteractable(Interactable interactable) => _interactables.Remove(interactable);

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
            if (distance <= interactionRange)
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
            interactionIndicatorCanvas.gameObject.SetActive(closestInteractable != null);
            if (closestInteractable != null)
                interactionIndicatorCanvas.position = closestInteractable.IndicatorPosition;
            _currentInteractable = closestInteractable;
        }
    }

    private void HandleInteraction()
    {
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

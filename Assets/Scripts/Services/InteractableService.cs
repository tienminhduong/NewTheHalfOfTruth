using System.Collections.Generic;
using CD;
using UnityEngine;
using UnityExtensions;

public class InteractableService : MonoBehaviourService<InteractableService>
{
    [SerializeField] private float _interactionRange = 2f;
    [SerializeField] private RectTransform _interactionIndicatorCanvas;


    private readonly HashSet<Interactable> _interactables = new();
    private Interactable _currentInteractable;
    private Player _activePlayer;
    private IHuman _trackingObject;
    private bool _updateLogic = true;

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
        InputHandler.OnInputFocusChange += HandleFocusUIChange;
    }

    void OnDisable()
    {
        InputHandler.Interact -= HandleInteraction;
        InputHandler.OnInputFocusChange -= HandleFocusUIChange;
    }


    void Update()
    {
        if (!_updateLogic)
            return;
        CheckForInteraction();
        UpdateTrackingObjectPosition();
    }

    private void HandleFocusUIChange(bool isUIFocused)
    {
        _updateLogic = !isUIFocused;
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
                    _trackingObject = closestInteractable as IHuman;
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
        if (_currentInteractable == null)
            return;

        if (!_currentInteractable.IsInteractable)
            return;

        if (_activePlayer != null)
        {
            Vector3 directionToInteractable = _currentInteractable.transform.position - _activePlayer.transform.position;
            if (directionToInteractable != Vector3.zero)
            {
                FaceDir faceDir;
                if (Mathf.Abs(directionToInteractable.x) > Mathf.Abs(directionToInteractable.y))
                    faceDir = directionToInteractable.x > 0 ? FaceDir.Right : FaceDir.Left;
                else
                    faceDir = directionToInteractable.y > 0 ? FaceDir.Up : FaceDir.Down;

                _activePlayer.RotateTo(faceDir);

                if (_currentInteractable is IHuman human)
                {
                    human.RotateTo(faceDir.Reverse());
                }
            }
        }

        // if (_currentInteractable is IHuman human)
        // {
        //     if (_activePlayer == null)
        //     {
        //         Debug.LogWarning("No active player registered to handle interaction.");
        //         return;
        //     }

        //     // Rotate the player to face the NPC before interaction
        //     Vector3 directionToNPC = human.transform.position - _activePlayer.transform.position;
        //     if (directionToNPC != Vector3.zero)
        //     {
        //         FaceDir faceDir;
        //         if (Mathf.Abs(directionToNPC.x) > Mathf.Abs(directionToNPC.y))
        //             faceDir = directionToNPC.x > 0 ? FaceDir.Right : FaceDir.Left;
        //         else
        //             faceDir = directionToNPC.y > 0 ? FaceDir.Up : FaceDir.Down;

        //         _activePlayer.RotateTo(faceDir);
        //         human.RotateTo(faceDir.Reverse());
        //     }
        // }

        _currentInteractable?.OnInteract();
        _currentInteractable = null;
        _trackingObject = null;
        _interactionIndicatorCanvas.gameObject.SetActive(false);
    }
}

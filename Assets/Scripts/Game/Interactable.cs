using UnityEngine;
using UnityExtensions;

public abstract class Interactable : MonoBehaviour
{
    public bool IsInteractable { get; set; } = true;
    public abstract InteractableType InteractableType { get; }

    private InteractableService _interactableService;

    public abstract void OnInteract();

    protected virtual void OnEnable()
    {
        ServiceLocator.TryOrWaitForService<InteractableService>(SetupService);
    }

    private void SetupService(IService service)
    {
        if (service is not InteractableService interactableService)
        {
            Debug.LogError($"ServiceLocator returned a service of type {service.GetType()} instead of InteractableService.");
            return;
        }

        _interactableService = interactableService;
        _interactableService.RegisterInteractable(this);
    }

    protected virtual void OnDisable()
    {
        _interactableService?.UnregisterInteractable(this);
        ServiceLocator.RemoveServiceCallback<InteractableService>(SetupService);
    }
}

public enum InteractableType
{
    Evidence,
    NPC,
    Item
}
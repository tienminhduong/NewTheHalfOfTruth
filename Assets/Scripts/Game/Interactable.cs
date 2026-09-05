using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public bool IsInteractable { get; set; } = true;
    public abstract InteractableType InteractableType { get; }

    private InteractableService _interactableService;
    private SavedDataService _savedDataService;

    protected SavedDataService SavedDataService => _savedDataService;

    public abstract void OnInteract();

    protected virtual void OnEnable()
    {
        ServiceLocator.TryOrWaitForService<InteractableService>(SetupInteractableService);
        ServiceLocator.TryOrWaitForService<SavedDataService>(SetupSavedService);
    }

    private void SetupInteractableService(IService service)
    {
        if (service is not InteractableService interactableService)
        {
            Debug.LogError($"ServiceLocator returned a service of type {service.GetType()} instead of InteractableService.");
            return;
        }

        _interactableService = interactableService;
        _interactableService.RegisterInteractable(this);
    }

    protected virtual void SetupSavedService(IService service)
    {
        if (service is not SavedDataService savedDataService)
        {
            Debug.LogError($"ServiceLocator returned a service of type {service.GetType()} instead of SavedDataService.");
            return;
        }

        _savedDataService = savedDataService;
    }

    protected virtual void OnDisable()
    {
        _interactableService?.UnregisterInteractable(this);
        ServiceLocator.RemoveServiceCallback<InteractableService>(SetupInteractableService);
        ServiceLocator.RemoveServiceCallback<SavedDataService>(SetupSavedService);
        _savedDataService = null;
        _interactableService = null;
    }
}

public enum InteractableType
{
    Evidence,
    NPC,
    Item
}
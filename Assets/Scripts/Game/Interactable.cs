using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public abstract class Interactable : MonoBehaviour
{
    public bool IsInteractable { get; set; } = true;
    public Vector3 IndicatorPosition { get; private set; } = Vector3.zero;

    private InteractableService _interactableService;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    protected virtual void OnValidate()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    public abstract void OnInteract();

    protected virtual void OnEnable()
    {
        IndicatorPosition = _spriteRenderer.bounds.center + Vector3.up * _spriteRenderer.bounds.extents.y;
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

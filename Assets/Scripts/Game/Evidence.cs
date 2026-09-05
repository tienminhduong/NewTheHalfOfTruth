using UnityEngine;

public class Evidence : Interactable
{
    public override InteractableType InteractableType => InteractableType.Evidence;

    override protected void OnEnable()
    {
        base.OnEnable();
        ServiceLocator.TryOrWaitForService<SavedDataService>(SetupSavedService);
    }

    override protected void OnDisable()
    {
        base.OnDisable();
        ServiceLocator.RemoveServiceCallback<SavedDataService>(SetupSavedService);
    }

    public override void OnInteract()
    {
        IsInteractable = false;
        if (SavedDataService != null)
            SavedDataService.SaveNotebookData(gameObject.name, "");
    }

    protected override void SetupSavedService(IService service)
    {
        base.SetupSavedService(service);
        if (SavedDataService != null)
        {
            IsInteractable = SavedDataService.GetPlayerNoteForEvidence(gameObject.name) == null;
        }
    }
}

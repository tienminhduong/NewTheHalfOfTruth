using UnityEngine;

public class Evidence : Interactable
{
    public override InteractableType InteractableType => InteractableType.Evidence;

    override protected void OnEnable()
    {
        base.OnEnable();
    }

    override protected void OnDisable()
    {
        base.OnDisable();
    }

    public override void OnInteract()
    {
        IsInteractable = false;
        if (SavedDataService != null)
        {
            SavedDataService.SaveNotebookData(gameObject.name, "");
        }
        else
        {
            Debug.LogError("SavedDataService is not available. Cannot save notebook data.");
        }
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

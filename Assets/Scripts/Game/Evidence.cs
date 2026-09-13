using UnityEngine;

public class Evidence : Interactable
{
    public override InteractableType InteractableType => InteractableType.Evidence;
    public string EvidenceKey => gameObject.name;
    [SerializeField] private EvidenceDatabase _evidenceDatabase;
    [SerializeField] private bool disappearAfterInteraction = false;

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
        if (_evidenceDatabase != null)
        {
            if (_evidenceDatabase.TryGetEvidenceData(EvidenceKey, out var evidenceData))
                DialogueUISystem.ShowDialogue(evidenceData.description, false);
            else
                Debug.LogError($"Evidence data for '{EvidenceKey}' not found in the EvidenceDatabase.");
        }
        else
            Debug.LogError("EvidenceDatabase is not assigned. Cannot retrieve evidence data.");

        if (SavedDataService != null)
            SavedDataService.SaveNotebookData(EvidenceKey, "");
        else
            Debug.LogError("SavedDataService is not available. Cannot save notebook data.");

        if (disappearAfterInteraction)
            Destroy(gameObject);
    }

    protected override void SetupSavedService(IService service)
    {
        base.SetupSavedService(service);
        if (SavedDataService != null)
        {
            IsInteractable = SavedDataService.GetPlayerNoteForEvidence(EvidenceKey) == null;
        }

        if (disappearAfterInteraction)
        {
            if (SavedDataService != null && SavedDataService.GetPlayerNoteForEvidence(EvidenceKey) != null)
                Destroy(gameObject);
        }
    }
}

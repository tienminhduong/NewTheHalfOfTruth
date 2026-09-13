using UnityEngine;

public class AppearCondNpc : ChatNpc
{
    [SerializeField] private string[] _evidenceKeysRequired;
    [SerializeField] private string[] _npcNamesRequired;

    protected override void SetupSavedService(IService service)
    {
        base.SetupSavedService(service);

        if (SavedDataService != null)
        {
            bool hasAllEvidence = true;
            foreach (var evidenceKey in _evidenceKeysRequired)
            {
                if (SavedDataService.GetPlayerNoteForEvidence(evidenceKey) == null)
                {
                    hasAllEvidence = false;
                    break;
                }
            }

            bool hasAllNpcInteractions = true;
            foreach (var npcName in _npcNamesRequired)
            {
                if (!SavedDataService.HasInteractedWithNPC(npcName))
                {
                    hasAllNpcInteractions = false;
                    break;
                }
            }

            if (!hasAllEvidence || !hasAllNpcInteractions)
            {
                Destroy(gameObject);
            }
        }
    }
}

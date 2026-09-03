using UnityEngine;

public class Evidence : Interactable
{
    public override InteractableType InteractableType => InteractableType.Evidence;

    public override void OnInteract()
    {
        Debug.Log("Interacted with Evidence: " + gameObject.name);
    }
}
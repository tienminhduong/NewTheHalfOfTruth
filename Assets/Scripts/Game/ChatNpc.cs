using UnityEngine;

public class ChatNpc : Interactable
{
    public override void OnInteract()
    {
        Debug.Log("Interacted with ChatNpc: " + gameObject.name);
    }
}

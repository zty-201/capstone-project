using UnityEngine;

public class TestNPC : MonoBehaviour, IInteractable
{
    [Header("NPC Data")]
    public string npcName = "Distressed Farmer";

    public void Interact()
    {
        Debug.Log($"<color=yellow>[Interaction]</color> You interacted with {npcName}!");

        // Later, this is where you will fire:
        // EventBus.OnDialogueStarted?.Invoke(missionData);
    }
}
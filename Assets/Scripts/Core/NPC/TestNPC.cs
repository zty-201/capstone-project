using UnityEngine;

public class NPCController : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        Debug.Log("<color=yellow>[NPC]</color> Farmer says: 'Please fix my pipes!'");

        // Transition the game into the minigame state. 
        // This locks player movement and unlocks the PipeVisual clicks!
        GameManager.Instance.StateManager.ChangeState(GameManager.Instance.PuzzleState);
    }
}
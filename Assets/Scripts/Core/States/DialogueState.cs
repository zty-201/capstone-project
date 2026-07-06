using UnityEngine;

public class DialogueState : IState
{
    public void Enter()
    {
        Debug.Log("<color=yellow>[DialogueState]</color> Dialogue started.");
    }

    public void Tick()
    {
        if (PointerInput.PrimaryPressedThisFrame())
            DialogueManager.Instance.OnAdvanceDialogue();
    }

    public void Exit()
    {
        Debug.Log("<color=yellow>[DialogueState]</color> Dialogue ended.");
    }
}
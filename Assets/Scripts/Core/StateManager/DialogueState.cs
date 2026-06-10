using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueState : IState
{
    public void Enter()
    {
        Debug.Log("<color=yellow>[DialogueState]</color> Dialogue started.");
    }

    public void Tick()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            DialogueManager.Instance.OnAdvanceDialogue();
    }

    public void Exit()
    {
        Debug.Log("<color=yellow>[DialogueState]</color> Dialogue ended.");
    }
}
using UnityEngine;
using UnityEngine.InputSystem;

public class PatchWellState : IState
{
    public void Enter()
    {
        Debug.Log("<color=orange>[PatchWellState]</color> Entered: Tap the well to patch it.");
    }

    public void Tick()
    {
        if (PointerInput.TryGetPrimaryPressWorldPosition(out Vector3 worldPos))
            EventBus.RaiseWellClicked(worldPos);

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Debug.Log("<color=orange>[PatchWellState]</color> ESC pressed. Transitioning back to Exploration State...");
            GameManager.Instance.StateManager.ChangeState(GameStateType.Exploration);
        }
    }

    public void Exit()
    {
        Debug.Log("<color=orange>[PatchWellState]</color> Exited.");
    }
}
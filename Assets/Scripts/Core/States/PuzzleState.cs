using UnityEngine;
using UnityEngine.InputSystem;

public class PuzzleState : IState
{
    public void Enter()
    {
        Debug.Log("<color=orange>[PuzzleState]</color> Entered: Playing the Pipe Puzzle.");
    }

    public void Tick()
    {
        if (PointerInput.TryGetPrimaryPressWorldPosition(out Vector3 worldPos))
            EventBus.RaisePuzzleClicked(worldPos);

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Debug.Log("<color=orange>[PuzzleState]</color> ESC pressed. Transitioning back to Exploration State...");
            GameManager.Instance.StateManager.ChangeState(GameStateType.Exploration);
        }
    }

    public void Exit()
    {
        Debug.Log("<color=orange>[PuzzleState]</color> Exited: Returning to town.");
    }
}

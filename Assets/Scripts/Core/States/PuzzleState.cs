using UnityEngine;
using UnityEngine.InputSystem;

// The pipe puzzle is now a Canvas panel (PuzzleCanvas) with IPointerClickHandler pipes — Unity's
// own EventSystem/GraphicRaycaster resolves clicks directly against whichever PipeVisual is
// actually under the cursor (see PipeVisual.OnPointerClick), so this state no longer needs to
// poll/forward pointer positions itself. Same shape as MissionBoardState/InfoBoardState: claim
// GameStateType so ExplorationState's own click handling stops, and provide the ESC escape hatch.
public class PuzzleState : IState
{
    public void Enter()
    {
        Debug.Log("<color=orange>[PuzzleState]</color> Entered: Playing the Pipe Puzzle.");
    }

    public void Tick()
    {
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

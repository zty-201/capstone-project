using UnityEngine;

// The pipe puzzle is now a Canvas panel (PuzzleCanvas) with IPointerClickHandler pipes — Unity's
// own EventSystem/GraphicRaycaster resolves clicks directly against whichever PipeVisual is
// actually under the cursor (see PipeVisual.OnPointerClick), so this state no longer needs to
// poll/forward pointer positions itself. All it does is claim GameStateType so
// ExplorationState's own click handling stops. No ESC exit: the only way into this state is
// MinigameActivator on OnSolutionSelected, and the well's own trigger is already disabled by
// then, so leaving would strand the player with no way back in.
public class PuzzleState : IState
{
    public void Enter()
    {
        Debug.Log("<color=orange>[PuzzleState]</color> Entered: Playing the Pipe Puzzle.");
    }

    public void Tick() { }

    public void Exit()
    {
        Debug.Log("<color=orange>[PuzzleState]</color> Exited: Returning to town.");
    }
}

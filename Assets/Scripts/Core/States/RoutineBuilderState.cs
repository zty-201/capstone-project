using UnityEngine;

// Mission 3's minigame is a screen-space UI panel (drag-to-reorder, via Unity's own
// EventSystem/IDragHandler), not a world-space playground — so unlike PuzzleState/
// BridgeBuilderState before their own UGUI conversions, this state drives no per-frame pointer
// logic of its own; the UI handles its own input regardless of GameStateType. All this state does
// is claim GameStateType so ExplorationState's movement/click polling stops while the panel is
// open. No ESC exit — same reasoning as PuzzleState: RoutineBoardInteractable has already
// disabled itself, so there'd be no way back in.
public class RoutineBuilderState : IState
{
    public void Enter()
    {
        Debug.Log("<color=orange>[RoutineBuilderState]</color> Entered: Planning the farmer's routine.");
    }

    public void Tick() { }

    public void Exit()
    {
        Debug.Log("<color=orange>[RoutineBuilderState]</color> Exited: Returning to town.");
    }
}

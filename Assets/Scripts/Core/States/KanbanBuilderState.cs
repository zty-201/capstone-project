using UnityEngine;

// Mission 4's minigame is a screen-space UI panel (drag a reorder-point marker per stall, then
// run a simulated day) — same shape as RoutineBuilderState: this state only claims
// GameStateType so ExplorationState's movement/click polling stops while the panel is open. All
// actual input (dragging thresholds, the Run Day button) is handled by Unity's own
// EventSystem/UGUI handlers regardless of GameStateType. No ESC exit — same reasoning as
// PuzzleState: the merchant's NPCController has already locked itself, so there'd be no way back in.
public class KanbanBuilderState : IState
{
    public void Enter()
    {
        Debug.Log("<color=orange>[KanbanBuilderState]</color> Entered: Setting up the market's Kanban signals.");
    }

    public void Tick() { }

    public void Exit()
    {
        Debug.Log("<color=orange>[KanbanBuilderState]</color> Exited: Returning to town.");
    }
}

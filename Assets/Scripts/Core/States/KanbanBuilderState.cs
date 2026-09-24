using UnityEngine;
using UnityEngine.InputSystem;

// Mission 4's optimal minigame is a screen-space UI panel (drag a reorder-point marker per
// stall, then run a simulated day) — same shape as RoutineBuilderState: this state only claims
// GameStateType so ExplorationState's movement/click polling stops while the panel is open, and
// provides the ESC-to-Exploration escape hatch. All actual input (dragging thresholds, the Run
// Day button) is handled by Unity's own EventSystem/UGUI handlers regardless of GameStateType.
public class KanbanBuilderState : IState
{
    public void Enter()
    {
        Debug.Log("<color=orange>[KanbanBuilderState]</color> Entered: Setting up the market's Kanban signals.");
    }

    public void Tick()
    {
        // ESC is only honored outside of a running simulation — leaving mid-sim would strand
        // KanbanBuilderSystem's pass/fail result off-screen with no way back into this state to
        // see it, same reasoning as RoutineBuilderState/BridgeBuilderState's canLeave guards.
        bool canLeave = KanbanBuilderSystem.Instance == null || !KanbanBuilderSystem.Instance.IsSimulating;

        if (canLeave && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Debug.Log("<color=orange>[KanbanBuilderState]</color> ESC pressed. Transitioning back to Exploration State...");
            GameManager.Instance.StateManager.ChangeState(GameStateType.Exploration);
        }
    }

    public void Exit()
    {
        Debug.Log("<color=orange>[KanbanBuilderState]</color> Exited: Returning to town.");
    }
}

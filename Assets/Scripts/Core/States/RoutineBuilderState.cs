using UnityEngine;
using UnityEngine.InputSystem;

// Mission 3's minigame is a screen-space UI panel (drag-to-reorder, via Unity's own
// EventSystem/IDragHandler), not a world-space playground — so unlike PuzzleState/
// BridgeBuilderState before their own UGUI conversions, this state drives no per-frame pointer
// logic of its own; the UI handles its own input regardless of GameStateType. All this state does
// is claim GameStateType so ExplorationState's movement/click polling stops while the panel is
// open, and provide the ESC-to-Exploration escape hatch (same shape as MissionBoardState).
public class RoutineBuilderState : IState
{
    public void Enter()
    {
        Debug.Log("<color=orange>[RoutineBuilderState]</color> Entered: Planning the farmer's routine.");
    }

    public void Tick()
    {
        // ESC is only honored outside of a submit's simulate/resolve coroutine — leaving mid-
        // resolution would strand FarmRoutineSystem's pass/fail result off-screen with no way
        // back into this state to see it, same reasoning as BridgeBuilderState's canLeave guard.
        bool canLeave = FarmRoutineSystem.Instance == null || !FarmRoutineSystem.Instance.IsSimulating;

        if (canLeave && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Debug.Log("<color=orange>[RoutineBuilderState]</color> ESC pressed. Transitioning back to Exploration State...");
            GameManager.Instance.StateManager.ChangeState(GameStateType.Exploration);
        }
    }

    public void Exit()
    {
        Debug.Log("<color=orange>[RoutineBuilderState]</color> Exited: Returning to town.");
    }
}

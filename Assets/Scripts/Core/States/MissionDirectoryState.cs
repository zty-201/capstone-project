using UnityEngine;
using UnityEngine.InputSystem;

// The expanded Mission Directory overlay (MissionDirectoryUI.Open). Same shape as
// SettingsMenuState: entered from a persistent HUD element rather than a world IInteractable,
// and claiming GameStateType so ExplorationState's map clicks stop while the overlay is up.
// Tapping outside the window (MissionDirectoryUI.Close, wired to the backdrop button) is the
// main way out — it works on Android, which has no ESC; ESC is honored too, same as everywhere else.
public class MissionDirectoryState : IState
{
    public void Enter() => Debug.Log("<color=magenta>[MissionDirectoryState]</color> Directory opened.");

    public void Tick()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            GameManager.Instance.StateManager.ChangeState(GameStateType.Exploration);
    }

    public void Exit()
    {
        MissionDirectoryUI.Instance.HideExpanded();
        Debug.Log("<color=magenta>[MissionDirectoryState]</color> Directory closed.");
    }
}

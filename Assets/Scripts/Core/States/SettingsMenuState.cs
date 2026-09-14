using UnityEngine;
using UnityEngine.InputSystem;

// Same shape as InfoBoardState/MissionBoardState — the one thing that differs from those two is
// how this state gets *entered* (SettingsMenuUI.OnOpenButtonPressed, wired to a persistent HUD
// button, rather than walking up to a world IInteractable), not how it's left. ESC is honored
// here same as everywhere else, but it's not the only way out: this game targets Android too,
// which has no ESC/back-key equivalent reaching Keyboard.current, so SettingsMenuUI also has its
// own Close button (OnCloseButtonPressed) wired directly to the same ChangeState call.
public class SettingsMenuState : IState
{
    public void Enter() => Debug.Log("<color=magenta>[SettingsMenuState]</color> Menu opened.");

    public void Tick()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            GameManager.Instance.StateManager.ChangeState(GameStateType.Exploration);
    }

    public void Exit()
    {
        SettingsMenuUI.Instance.Hide();
        Debug.Log("<color=magenta>[SettingsMenuState]</color> Menu closed.");
    }
}

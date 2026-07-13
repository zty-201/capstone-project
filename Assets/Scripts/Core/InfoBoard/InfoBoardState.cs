using UnityEngine;
using UnityEngine.InputSystem;

public class InfoBoardState : IState
{
    public void Enter() => Debug.Log("<color=magenta>[InfoBoardState]</color> Board opened.");

    public void Tick()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            GameManager.Instance.StateManager.ChangeState(GameStateType.Exploration);
    }

    public void Exit()
    {
        InfoBoardUI.Instance.Hide();
        Debug.Log("<color=magenta>[InfoBoardState]</color> Board closed.");
    }
}

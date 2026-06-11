using UnityEngine;
using UnityEngine.InputSystem;

public class MissionBoardState : IState
{
    public void Enter() => Debug.Log("<color=magenta>[MissionBoardState]</color> Board opened.");

    public void Tick()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            GameManager.Instance.StateManager.ChangeState(GameManager.Instance.ExploreState);
    }

    public void Exit()
    {
        MissionBoardUI.Instance.Hide();
        Debug.Log("<color=magenta>[MissionBoardState]</color> Board closed.");
    }
}
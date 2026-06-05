using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class ExplorationState : IState
{
    public void Enter()
    {
        Debug.Log("<color=lightblue>[ExplorationState]</color> Entered: Player is roaming the town.");
    }

    public void Execute()
    {
        // Wait for the player to press 'M' (Simulating opening a mission)
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
        {
            Debug.Log("<color=lightblue>[ExplorationState]</color> 'M' pressed. Transitioning to Mission State...");
            GameManager.Instance.ChangeState(new MissionState());
        }
    }

    public void Exit()
    {
        Debug.Log("<color=lightblue>[ExplorationState]</color> Exited: Leaving town view.");
    }
}
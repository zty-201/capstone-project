using UnityEngine;
using UnityEngine.InputSystem;

public class MissionState : IState
{
    public void Enter()
    {
        Debug.Log("<color=orange>[MissionState]</color> Entered: Playing a Kaizen minigame.");
    }

    public void Execute()
    {
        // Wait for the player to press 'X' (Simulating finishing the mission)
        if (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame)
        {
            Debug.Log("<color=orange>[MissionState]</color> 'X' pressed. Transitioning back to Exploration State...");
            GameManager.Instance.ChangeState(new ExplorationState());
        }
    }

    public void Exit()
    {
        Debug.Log("<color=orange>[MissionState]</color> Exited: Returning to town.");
    }
}
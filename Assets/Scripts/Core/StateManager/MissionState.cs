using UnityEngine;
using UnityEngine.InputSystem;

public class MissionState : IState
{
    public void Enter()
    {
        Debug.Log("<color=orange>[MissionState]</color> Entered: Playing a Kaizen minigame.");
    }

    // 1. Renamed Execute() to Tick() to satisfy the IState interface
    public void Tick()
    {
        // Wait for the player to press 'X' (Simulating finishing the mission)
        if (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame)
        {
            Debug.Log("<color=orange>[MissionState]</color> 'X' pressed. Transitioning back to Exploration State...");

            // 2. Use the pre-allocated StateManager and ExploreState to avoid memory allocation
            GameManager.Instance.StateManager.ChangeState(GameManager.Instance.ExploreState);
        }
    }

    public void Exit()
    {
        Debug.Log("<color=orange>[MissionState]</color> Exited: Returning to town.");
    }
}
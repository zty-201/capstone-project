using UnityEngine;
using UnityEngine.InputSystem;

public class MissionState : IState
{
    public void Enter()
    {
        Debug.Log("<color=orange>[MissionState]</color> Entered: Playing a Kaizen minigame.");

        // 1. Turn the pipes ON when the state starts
        if (GameManager.Instance.puzzleContainer != null)
        {
            GameManager.Instance.puzzleContainer.SetActive(true);
        }
    }

    public void Tick()
    {
        // Wait for the player to press 'X'
        if (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame)
        {
            Debug.Log("<color=orange>[MissionState]</color> 'X' pressed. Transitioning back to Exploration State...");
            GameManager.Instance.StateManager.ChangeState(GameManager.Instance.ExploreState);
        }
    }

    public void Exit()
    {
        Debug.Log("<color=orange>[MissionState]</color> Exited: Returning to town.");

        // 2. Turn the pipes OFF when leaving the state
        if (GameManager.Instance.puzzleContainer != null)
        {
            GameManager.Instance.puzzleContainer.SetActive(false);
        }
    }

}
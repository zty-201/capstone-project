using UnityEngine;

public class MissionBoardInteractable : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        MissionBoardUI.Instance.Show();
        GameManager.Instance.StateManager.ChangeState(GameManager.Instance.MissionBoardState);
    }
}
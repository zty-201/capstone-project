using UnityEngine;

public class InfoBoardInteractable : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        InfoBoardUI.Instance.Show();
        GameManager.Instance.StateManager.ChangeState(GameStateType.InfoBoard);
    }
}

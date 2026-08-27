using UnityEngine;

public class InfoBoardInteractable : MonoBehaviour, IInteractable
{
    [Header("Audio")]
    [SerializeField] private AudioClip interactSfx;
    public AudioClip InteractSfx => interactSfx;

    public void Interact()
    {
        InfoBoardUI.Instance.Show();
        GameManager.Instance.StateManager.ChangeState(GameStateType.InfoBoard);
    }
}

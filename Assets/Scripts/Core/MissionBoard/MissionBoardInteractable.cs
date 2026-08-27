using UnityEngine;

public class MissionBoardInteractable : MonoBehaviour, IInteractable
{
    [Header("Audio")]
    [SerializeField] private AudioClip interactSfx;
    public AudioClip InteractSfx => interactSfx;

    public void Interact()
    {
        MissionBoardUI.Instance.Show();
        GameManager.Instance.StateManager.ChangeState(GameStateType.MissionBoard);
    }
}

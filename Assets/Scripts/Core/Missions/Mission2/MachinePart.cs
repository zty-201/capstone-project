using UnityEngine;

public class MachinePart : MonoBehaviour, IInteractable
{
    [SerializeField] private PartCollectionSystem collectionSystem;

    [Header("Audio")]
    [SerializeField] private AudioClip interactSfx;
    public AudioClip InteractSfx => interactSfx;

    public void Interact()
    {
        collectionSystem.OnPartCollected();
        gameObject.SetActive(false);
    }

    public void ResetPart() => gameObject.SetActive(true);
}

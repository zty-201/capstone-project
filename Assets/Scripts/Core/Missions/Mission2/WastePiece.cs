using UnityEngine;

public class WastePiece : MonoBehaviour, IInteractable
{
    [SerializeField] private WastePickupSystem pickupSystem;
    [SerializeField] private GameObject wasteVisual;

    [Header("Audio")]
    [SerializeField] private AudioClip interactSfx;
    public AudioClip InteractSfx => interactSfx;

    public void Interact()
    {
        if (wasteVisual != null) wasteVisual.SetActive(false);
        pickupSystem.OnWasteRemoved();
        gameObject.SetActive(false);
    }

    public void ResetPiece()
    {
        if (wasteVisual != null) wasteVisual.SetActive(true);
        gameObject.SetActive(true);
    }
}

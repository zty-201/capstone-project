using UnityEngine;

public class WastePiece : MonoBehaviour, IInteractable
{
    [SerializeField] private WastePickupSystem pickupSystem;
    [SerializeField] private GameObject wasteVisual;

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

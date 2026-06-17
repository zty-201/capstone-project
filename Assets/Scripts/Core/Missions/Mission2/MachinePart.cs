using UnityEngine;

public class MachinePart : MonoBehaviour, IInteractable
{
    [SerializeField] private PartCollectionSystem collectionSystem;

    public void Interact()
    {
        collectionSystem.OnPartCollected();
        gameObject.SetActive(false);
    }
}

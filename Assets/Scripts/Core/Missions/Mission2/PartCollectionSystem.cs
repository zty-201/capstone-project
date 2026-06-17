using UnityEngine;

public class PartCollectionSystem : MonoBehaviour
{
    [SerializeField] private int totalParts = 3;
    [SerializeField] private AssemblyPoint assemblyPoint;

    private int collectedCount;

    private void OnEnable()
    {
        collectedCount = 0;
        assemblyPoint.gameObject.SetActive(false);
    }

    public void OnPartCollected()
    {
        collectedCount++;
        if (collectedCount >= totalParts)
            assemblyPoint.gameObject.SetActive(true);
    }
}

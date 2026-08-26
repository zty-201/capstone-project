using UnityEngine;

// Functional replacement for what TownSatisfactionSystem.HandleMissionCompleted used to do:
// grants the player a Gold Coin item for every mission solved at the root cause. Trivial
// completions earn nothing.
public class CoinRewardSystem : MonoBehaviour
{
    [SerializeField] private ItemData goldCoinItem;

    private void OnEnable() => EventBus.OnMissionCompleted += HandleMissionCompleted;
    private void OnDisable() => EventBus.OnMissionCompleted -= HandleMissionCompleted;

    private void HandleMissionCompleted(int missionID, bool wasOptimal)
    {
        if (!wasOptimal) return;
        InventorySystem.Instance.TryAddItem(goldCoinItem, 1);
    }
}

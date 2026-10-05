using UnityEngine;

// The town's improvement budget — the "Act" step after a root-cause fix: Gold Coins
// (earned only by optimal fixes, see CoinRewardSystem) are spent here to move the whole town up a
// visual tier. Purchases happen through TownHallInteractable; every BuildingUpgrade and any
// gameplay effect (TrashSpawner's spawn rate) just listen for EventBus.OnTownUpgraded.
public class TownUpgradeSystem : MonoBehaviour
{
    public static TownUpgradeSystem Instance { get; private set; }

    [SerializeField] private ItemData goldCoinItem;
    // Coins spent to reach each next level: index 0 = rundown -> level 1, index 1 = level 1 -> 2.
    // 3 + 2 = all 5 coins in the game.
    [SerializeField] private int[] upgradeCosts = { 3, 2 };

    public int CurrentLevel { get; private set; }
    public bool IsMaxLevel => CurrentLevel >= upgradeCosts.Length;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (goldCoinItem == null) Debug.LogError($"[{name}] goldCoinItem is not assigned!", this);
    }

    public bool CanAffordNextUpgrade() => !IsMaxLevel && CoinsNeededForNextUpgrade == 0;

    public int CoinsNeededForNextUpgrade =>
        Mathf.Max(0, upgradeCosts[CurrentLevel] - InventorySystem.Instance.CountItem(goldCoinItem));

    public void PurchaseNextUpgrade()
    {
        if (!CanAffordNextUpgrade())
            throw new System.InvalidOperationException("PurchaseNextUpgrade called without enough coins — check CanAffordNextUpgrade first.");

        InventorySystem.Instance.TryRemoveItem(goldCoinItem, upgradeCosts[CurrentLevel]);
        CurrentLevel++;
        EventBus.RaiseTownUpgraded(CurrentLevel);
    }
}

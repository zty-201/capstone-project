using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int slotCount = 8;

    private InventorySlot[] slots;

    public IReadOnlyList<InventorySlot> Slots => slots;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        slots = new InventorySlot[slotCount];
        for (int i = 0; i < slotCount; i++) slots[i] = new InventorySlot();
    }

    /// <returns>False if the item couldn't fit anywhere (no matching stack with room, no empty slot).</returns>
    public bool TryAddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        if (item.stackable)
        {
            foreach (var slot in slots)
            {
                if (slot.IsEmpty || slot.item != item || slot.count >= item.maxStack) continue;
                slot.count += amount;
                EventBus.RaiseInventoryChanged();
                return true;
            }
        }

        foreach (var slot in slots)
        {
            if (!slot.IsEmpty) continue;
            slot.item = item;
            slot.count = amount;
            EventBus.RaiseInventoryChanged();
            return true;
        }

        return false;
    }

    public int CountItem(ItemData item)
    {
        int total = 0;
        foreach (var slot in slots)
            if (!slot.IsEmpty && slot.item == item) total += slot.count;
        return total;
    }

    /// <returns>False (no change made) if the inventory doesn't hold at least `amount`.</returns>
    public bool TryRemoveItem(ItemData item, int amount)
    {
        if (amount <= 0 || CountItem(item) < amount) return false;

        int remaining = amount;
        foreach (var slot in slots)
        {
            if (remaining <= 0) break;
            if (slot.IsEmpty || slot.item != item) continue;

            int take = Mathf.Min(remaining, slot.count);
            slot.count -= take;
            remaining -= take;
            if (slot.count <= 0) slot.item = null;
        }

        EventBus.RaiseInventoryChanged();
        return true;
    }

    public void RemoveAllOfItem(ItemData item)
    {
        bool changed = false;
        foreach (var slot in slots)
        {
            if (slot.IsEmpty || slot.item != item) continue;
            slot.item = null;
            slot.count = 0;
            changed = true;
        }

        if (changed) EventBus.RaiseInventoryChanged();
    }
}

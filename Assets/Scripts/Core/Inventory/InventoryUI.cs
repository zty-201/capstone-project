using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Fixed-slot HUD readout of InventorySystem. Occupies the screen position the old
// SatisfactionBarUI used to hold.
public class InventoryUI : MonoBehaviour
{
    [System.Serializable]
    public class SlotUI
    {
        public GameObject root;
        public Image icon;
        public TextMeshProUGUI countText;
    }

    [Header("Slot References — one per InventorySystem slot, in order")]
    [SerializeField] private SlotUI[] slotUIs;

    private void OnEnable() => EventBus.OnInventoryChanged += Refresh;
    private void OnDisable() => EventBus.OnInventoryChanged -= Refresh;

    private void Start() => Refresh();

    private void Refresh()
    {
        var slots = InventorySystem.Instance.Slots;

        for (int i = 0; i < slotUIs.Length; i++)
        {
            bool hasItem = i < slots.Count && !slots[i].IsEmpty;
            slotUIs[i].root.SetActive(hasItem);
            if (!hasItem) continue;

            slotUIs[i].icon.sprite = slots[i].item.icon;
            slotUIs[i].countText.text = slots[i].count > 1 ? slots[i].count.ToString() : "";
        }
    }
}

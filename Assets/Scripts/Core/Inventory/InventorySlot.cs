// Plain data holder for one inventory slot. Not a MonoBehaviour — InventorySystem owns a fixed
// array of these directly, the same way StageManager owns plain dictionaries for its own state.
public class InventorySlot
{
    public ItemData item;
    public int count;

    public bool IsEmpty => item == null || count <= 0;
}

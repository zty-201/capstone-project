using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Kaizen Systems/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Item Identity")]
    public string itemID;
    public string itemName;
    public Sprite icon;

    [Header("Stacking")]
    public bool stackable = true;
    public int maxStack = 99;
}

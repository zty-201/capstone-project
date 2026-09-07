using UnityEngine;

// One selectable plank material for the bridge-builder minigame (see BridgeBuilderSystem) —
// same "plain data, no logic" convention as ItemData. costPerUnitLength is what the player
// actually trades off against breakForce: a cheap, weak material stretches a limited budget
// further but snaps sooner under the test cart's weight.
[CreateAssetMenu(fileName = "NewBridgeMaterial", menuName = "Kaizen Systems/Bridge Material Data")]
public class BridgeMaterialData : ScriptableObject
{
    [Header("Material Identity")]
    public string materialID;
    public string materialName;
    public Sprite icon;

    [Header("Physical Properties")]
    public float costPerUnitLength = 1f;
    public float breakForce = 40f;
    public Color plankColor = Color.white;
}

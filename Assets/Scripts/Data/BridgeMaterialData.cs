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

    [Header("Road vs. Reinforcement")]
    // True (the default): planks of this material sit on BridgeBuilderSystem's road layer, the
    // one the test cart's own collider can actually touch. False: purely structural — the plank
    // still fully participates in HingeJoint2D load-bearing (breakForce, reaction force), it just
    // sits on a separate layer the cart's collider is configured to never collide with (see
    // Project Settings > Physics 2D > Layer Collision Matrix), so it can reinforce a span without
    // ever being mistaken for drivable surface — same distinction real Poly Bridge draws between
    // road and every other material.
    public bool isRoad = true;
}

using UnityEngine;
using UnityEngine.Serialization;

// Attach to any building with one child GameObject per town tier (index 0 = rundown, 1 = first
// upgrade, 2 = final). Swaps to the matching tier whenever TownUpgradeSystem raises
// OnTownUpgraded. Each tier child can hold several SpriteRenderers (e.g. a Base on the
// EntityTilemap sorting layer and a Roof on ForeGroundTilemap, so the roof still draws in front
// of the player).
public class BuildingUpgrade : MonoBehaviour
{
    [FormerlySerializedAs("stages")]
    [SerializeField] private GameObject[] tiers;

    private void OnEnable() => EventBus.OnTownUpgraded += ApplyTier;
    private void OnDisable() => EventBus.OnTownUpgraded -= ApplyTier;

    // Start, not OnEnable: every Awake (including TownUpgradeSystem's) has run by now, so the
    // current level is safe to read — same reasoning as NPCTrustUI.
    private void Start() => ApplyTier(TownUpgradeSystem.Instance.CurrentLevel);

    private void ApplyTier(int level)
    {
        if (level >= tiers.Length)
            throw new System.IndexOutOfRangeException($"[{name}] has {tiers.Length} tiers but the town reached level {level}.");

        for (int i = 0; i < tiers.Length; i++)
            tiers[i].SetActive(i == level);
    }
}

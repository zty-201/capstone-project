using UnityEngine;

// Companion component for a mission-giving interactable (NPCController, RiverInteractable),
// same "attach alongside, don't couple to" pattern as InteractionIndicator. Shows trust as a
// row of pip icons, low -> high. SpriteRenderer, not UI Image/Canvas: these are toggled on/off
// world sprites with no fill/layout/text need, so they follow the same rendering system as
// every other world object rather than pulling in a per-NPC Canvas (see World-Attached NPC UI
// in CLAUDE.md).
public class NPCTrustUI : MonoBehaviour
{
    [SerializeField] private int missionID;
    [SerializeField] private SpriteRenderer[] trustPips;

    // Read in Start(), not OnEnable(): Unity guarantees every object's Awake runs before any
    // object's Start, so TrustSystem.Instance is safe here without the OnEnable-ordering
    // workaround NPCPatrol needs for GameManager.Instance.
    private void Start() => Refresh(TrustSystem.Instance.GetTrust(missionID));

    private void OnEnable() => EventBus.OnTrustChanged += HandleTrustChanged;
    private void OnDisable() => EventBus.OnTrustChanged -= HandleTrustChanged;

    private void HandleTrustChanged(int changedMissionID, int newTrust)
    {
        if (changedMissionID != missionID) return;
        Refresh(newTrust);
    }

    private void Refresh(int trust)
    {
        for (int i = 0; i < trustPips.Length; i++)
            trustPips[i].enabled = i < trust;
    }
}

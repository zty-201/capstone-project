using UnityEngine;

// One stall in Mission 4's trivial "Restock by Feel" minigame. Stock drains on its own
// (MarketStallTrivialSystem.Update ticks every stall each frame); the player interacts to shove
// it back to full, reactively — there's no signal telling them a stall is about to run dry, which
// is exactly what the optimal Kanban path adds and this path lacks.
public class MarketStall : MonoBehaviour, IInteractable
{
    [SerializeField] private float depletionRate = 0.08f; // stock ratio lost per second
    [SerializeField] private SpriteRenderer stockVisual;
    [SerializeField] private Color fullColor = Color.green;
    [SerializeField] private Color emptyColor = Color.red;

    [Header("Audio")]
    [SerializeField] private AudioClip interactSfx;
    public AudioClip InteractSfx => interactSfx;

    private float stockRatio = 1f;

    public void Interact() => ResetStall();

    public void TickDeplete(float dt)
    {
        stockRatio = Mathf.Clamp01(stockRatio - depletionRate * dt);
        if (stockVisual != null) stockVisual.color = Color.Lerp(emptyColor, fullColor, stockRatio);
    }

    public void ResetStall()
    {
        stockRatio = 1f;
        if (stockVisual != null) stockVisual.color = fullColor;
    }
}

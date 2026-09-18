using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Define the core shapes your pipes can be
public enum PipeShape
{
    EndPiece, // Single opening
    Straight, // Two opposite openings
    Corner,   // Two adjacent openings
    TJunction,// Three openings
    Cross     // Four openings
}

// UI Image, not SpriteRenderer: Container_Optimal_M1 is a Canvas panel (see PuzzleCanvas in the
// Mission 1 setup guide), same "separate Canvas encapsulating the UI" shape as Mission 5's
// BridgeCanvas. Click detection is IPointerClickHandler through Unity's own EventSystem/
// GraphicRaycaster instead of a broadcast EventBus.OnPuzzleClicked world-position + per-object
// Collider2D.OverlapPoint check — the raycaster only ever calls this on the pipe actually under
// the cursor, so there's nothing left for this class to resolve itself, only react to.
//
// GetStartingBits/rotation math is completely unaffected by this: it only ever reads
// transform.eulerAngles.z, which RectTransform (Image's transform) reports identically to a
// world-space Transform — the canonical-bits-per-shape calibration below still has to match
// whatever the sprite art actually shows at 0°, exactly as before.
public class PipeVisual : MonoBehaviour, IPointerClickHandler
{
    [Header("Level Design")]
    public PipeShape shapeType;
    public int gridX;
    public int gridY;

    [Header("Visuals")]
    [SerializeField] private Sprite filledSprite;

    [Header("Audio")]
    [SerializeField] private AudioClip rotateClip;

    private Image image;
    private Sprite emptySprite;

    private void Awake()
    {
        image = GetComponent<Image>();
        emptySprite = image.sprite;
    }

    public void SetPowered(bool powered)
    {
        image.sprite = powered ? filledSprite : emptySprite;
    }

    public void ResetRotation(float originalZ)
    {
        transform.eulerAngles = new Vector3(0f, 0f, originalZ);
    }

    public PipeDirection GetStartingBits()
    {
        PipeDirection bits = PipeDirection.None;

        switch (shapeType)
        {
            case PipeShape.EndPiece: bits = PipeDirection.Up; break;
            case PipeShape.Straight: bits = PipeDirection.Up | PipeDirection.Down; break;
            case PipeShape.Corner: bits = PipeDirection.Down | PipeDirection.Right; break;
            case PipeShape.TJunction: bits = PipeDirection.Left | PipeDirection.Right | PipeDirection.Down; break;
            case PipeShape.Cross: bits = PipeDirection.Up | PipeDirection.Right | PipeDirection.Down | PipeDirection.Left; break;
        }

        float zRot = transform.eulerAngles.z;

        int rotations = 0;
        if (Mathf.Approximately(zRot, 270f)) rotations = 1;
        else if (Mathf.Approximately(zRot, 180f)) rotations = 2;
        else if (Mathf.Approximately(zRot, 90f)) rotations = 3;

        for (int i = 0; i < rotations; i++)
        {
            int b = (int)bits;
            bits = (PipeDirection)(((b << 1) | (b >> 3)) & 15);
        }

        return bits;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Container_Optimal_M1 stays active (and visible, via its own screen-space Canvas)
        // between a player Esc-ing out mid-puzzle and the mission actually resolving — same
        // pre-existing behavior the old world-position broadcast had too, since nothing there
        // ever hid the container on Exit either. This guard is what stops a stray click from
        // rotating a pipe while the player isn't actually in Puzzle state anymore; unlike the old
        // broadcast (which only Puzzle state ever raised in the first place), UGUI's EventSystem
        // will call this on any active, raycast-target Image regardless of GameStateType, so the
        // check has to live here now instead of being implicit in who raises the event.
        if (GameManager.Instance.StateManager.CurrentStateType != GameStateType.Puzzle) return;

        PipePuzzleSystem.Instance.RotatePipeAt(gridX, gridY);
        transform.Rotate(0, 0, -90f);
        AudioManager.Instance.PlaySFX(rotateClip);
    }
}

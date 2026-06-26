using UnityEngine;

// Define the core shapes your pipes can be
public enum PipeShape
{
    EndPiece, // Single opening
    Straight, // Two opposite openings
    Corner,   // Two adjacent openings
    TJunction,// Three openings
    Cross     // Four openings
}

public class PipeVisual : MonoBehaviour
{
    [Header("Level Design")]
    public PipeShape shapeType;
    public int gridX;
    public int gridY;

    [Header("Dependencies")]
    public PipePuzzleSystem puzzleSystem;

    private BoxCollider2D col;
    private SpriteRenderer sr;

    private static readonly Color PoweredColor = new Color(0.45f, 0.85f, 1f);

    private void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    public void SetPowered(bool powered)
    {
        sr.color = powered ? PoweredColor : Color.white;
    }

    private void OnEnable()
    {
        // Subscribe to your custom EventBus
        EventBus.OnPuzzleClicked += HandlePuzzleClick;
    }

    private void OnDisable()
    {
        EventBus.OnPuzzleClicked -= HandlePuzzleClick;
    }

    public PipeDirection GetStartingBits()
    {
        PipeDirection bits = PipeDirection.None;

        switch (shapeType)
        {
            case PipeShape.EndPiece: bits = PipeDirection.Up; break;
            case PipeShape.Straight: bits = PipeDirection.Up | PipeDirection.Down; break;
            case PipeShape.Corner: bits = PipeDirection.Down | PipeDirection.Right; break;
            case PipeShape.TJunction: bits = PipeDirection.Up | PipeDirection.Right | PipeDirection.Down; break;
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

    private void HandlePuzzleClick(Vector3 worldPos)
    {
        // 1. Guard Clause: Ignore if not in the puzzle state
        if (GameManager.Instance.StateManager.CurrentStateType != GameStateType.Puzzle) return;

        // 2. Check if the mouse click exactly overlapped THIS pipe's collider
        if (col.OverlapPoint(worldPos))
        {
            puzzleSystem.RotatePipeAt(gridX, gridY);
            transform.Rotate(0, 0, -90f);
        }
    }
}
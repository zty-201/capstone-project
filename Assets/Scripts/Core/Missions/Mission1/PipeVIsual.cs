using UnityEngine;
using UnityEngine.EventSystems;

// IPointerClickHandler allows this object to detect standard UI clicks or Physics2D Raycasts
public class PipeVisual : MonoBehaviour, IPointerClickHandler
{
    [Header("Grid Coordinates")]
    public int gridX;
    public int gridY;

    [Header("Dependencies")]
    public PipePuzzleSystem puzzleSystem; // Drag your GameManager/PuzzleManager here

    public void OnPointerClick(PointerEventData eventData)
    {
        // 1. Guard Clause: Only accept clicks if the game is actually in Puzzle Mode!
        if (GameManager.Instance.StateManager.CurrentState != GameManager.Instance.PuzzleState)
            return;

        // 2. Tell the backend to run the bitwise math
        puzzleSystem.RotatePipeAt(gridX, gridY);

        // 3. Visually rotate this sprite -90 degrees on the Z axis (Clockwise in Unity 2D)
        transform.Rotate(0, 0, -90f);
    }
}
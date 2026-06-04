using UnityEngine;
using UnityEngine.InputSystem; // Using the New Input System for touch/click simulation

public class GridTester : MonoBehaviour
{
    private GridSystem gridSystem;

    [Header("Grid Settings")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1f;

    private void Start()
    {
        // 1. Initialize your mathematical grid in memory
        gridSystem = new GridSystem(gridWidth, gridHeight, cellSize);
        Debug.Log($"<color=cyan>[GridTester]</color> Grid instantiated: {gridWidth}x{gridHeight}");
    }

    [Header("Prototype References")]
    public Transform playerPrototype; // Drag your player sprite here in the Inspector

    private void Update()
    {
        // Simulate the frontend touch interaction
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 screenPosition = Mouse.current.position.ReadValue();
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, Camera.main.nearClipPlane));

            // Convert the world position to grid coordinate integers
            int gridX = Mathf.FloorToInt(worldPosition.x / cellSize);
            int gridY = Mathf.FloorToInt(worldPosition.y / cellSize);

            // Query your backend validation logic
            if (gridSystem.IsValidMove(gridX, gridY))
            {
                Debug.Log($"<color=green>Moving Player to:</color> ({gridX}, {gridY})");

                // Snap the player to the center of the clicked cell
                if (playerPrototype != null)
                {
                    playerPrototype.position = new Vector3(gridX + (cellSize / 2f), gridY + (cellSize / 2f), 0);
                }

                // Broadcast the movement to the rest of the game via your Event Bus!
                EventBus.OnPlayerMoved?.Invoke(new Vector2Int(gridX, gridY));
            }
            else
            {
                Debug.Log($"<color=red>Invalid Move!</color> Cannot walk to ({gridX}, {gridY})");
            }
        }
    }

    // 5. Draw the invisible math in the Unity Editor Scene View
    private void OnDrawGizmos()
    {
        // Only draw if the game is running and the grid exists
        if (!Application.isPlaying || gridSystem == null) return;

        Gizmos.color = Color.white;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // Calculate the bottom-left corner of each cell
                Vector3 cellPosition = new Vector3(x * cellSize, y * cellSize, 0);

                // Gizmos.DrawWireCube draws from the center, so we offset it by half the cell size
                Vector3 centerOffset = new Vector3(cellSize / 2f, cellSize / 2f, 0);

                Gizmos.DrawWireCube(cellPosition + centerOffset, new Vector3(cellSize, cellSize, 0));
            }
        }
    }
}
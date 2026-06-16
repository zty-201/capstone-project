using UnityEngine;

public class InputManager : MonoBehaviour
{
    [Header("Dependencies")]
    public Transform playerTransform;
    public PathfindingSystem pathfindingSystem;
    public float cellSize = 1f;

    private void Awake()
    {
        if (playerTransform == null) Debug.LogError($"[{name}] playerTransform is not assigned!", this);
        if (pathfindingSystem == null) Debug.LogError($"[{name}] pathfindingSystem is not assigned!", this);
    }

    private void OnEnable() => EventBus.OnMapClicked += HandleMapClicked;
    private void OnDisable() => EventBus.OnMapClicked -= HandleMapClicked;

    private void HandleMapClicked(Vector3 worldPos)
    {
        if (pathfindingSystem == null || playerTransform == null) return;

        pathfindingSystem.GetGridCoordinates(worldPos, out int targetX, out int targetY);
        pathfindingSystem.GetGridCoordinates(playerTransform.position, out int startX, out int startY);

        Collider2D hitCollider = Physics2D.OverlapPoint(worldPos);
        if (hitCollider != null && hitCollider.TryGetComponent(out IInteractable interactable))
        {
            int distance = Mathf.Abs(startX - targetX) + Mathf.Abs(startY - targetY);

            if (distance <= 1)
            {
                interactable.Interact();
                return;
            }
            else
            {
                int dx = startX - targetX;
                int dy = startY - targetY;

                if (Mathf.Abs(dx) > Mathf.Abs(dy)) targetX += (dx > 0) ? 1 : -1;
                else targetY += (dy > 0) ? 1 : -1;
            }
        }

        EventBus.RaisePathRequested(new Vector2Int(startX, startY), new Vector2Int(targetX, targetY));
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Coroutine movementCoroutine;

    private void OnEnable() => EventBus.OnPathGenerated += HandlePathGenerated;
    private void OnDisable() => EventBus.OnPathGenerated -= HandlePathGenerated;

    public PathfindingSystem pathfindingSystem; // Drag GridManager here in the Inspector

    private void HandlePathGenerated(List<GridNode> path)
    {
        if (movementCoroutine != null) StopCoroutine(movementCoroutine);
        movementCoroutine = StartCoroutine(FollowPath(path));
    }

    private IEnumerator FollowPath(List<GridNode> path)
    {
        for (int i = 1; i < path.Count; i++)
        {
            // USE THE HELPER INSTEAD OF HARDCODED MATH
            Vector3 targetPos = pathfindingSystem.GetWorldPositionCenter(path[i].x, path[i].y);

            while (Vector3.Distance(transform.position, targetPos) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = targetPos;
            EventBus.OnPlayerMoved?.Invoke(new Vector2Int(path[i].x, path[i].y));
        }
    }
    
}
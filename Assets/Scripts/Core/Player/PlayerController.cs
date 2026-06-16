using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Coroutine movementCoroutine;

    private Animator anim;
    private SpriteRenderer spriteRenderer; // 1. Add reference

    public PathfindingSystem pathfindingSystem;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>(); // 2. Cache component
    }

    private void OnEnable() => EventBus.OnPathGenerated += HandlePathGenerated;
    private void OnDisable() => EventBus.OnPathGenerated -= HandlePathGenerated;

    private void HandlePathGenerated(List<GridNode> path)
    {
        if (movementCoroutine != null) StopCoroutine(movementCoroutine);
        movementCoroutine = StartCoroutine(FollowPath(path));
    }

    private IEnumerator FollowPath(List<GridNode> path)
    {
        anim.SetFloat("Speed", 1f);

        for (int i = 1; i < path.Count; i++)
        {
            Vector3 targetPos = pathfindingSystem.GetWorldPositionCenter(path[i].x, path[i].y);
            Vector3 moveDir = (targetPos - transform.position).normalized;

            anim.SetFloat("MoveX", moveDir.x);
            anim.SetFloat("MoveY", moveDir.y);

            // 3. Flip the sprite based on horizontal direction
            // We only check > 0 or < 0 so that strictly vertical movement doesn't overwrite the last horizontal facing state
            if (moveDir.x < -0.01f)
            {
                spriteRenderer.flipX = true;  // Facing Left
            }
            else if (moveDir.x > 0.01f)
            {
                spriteRenderer.flipX = false; // Facing Right
            }

            while (Vector3.Distance(transform.position, targetPos) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = targetPos;
            EventBus.RaisePlayerMoved(new Vector2Int(path[i].x, path[i].y));
        }

        anim.SetFloat("Speed", 0f);
    }
}
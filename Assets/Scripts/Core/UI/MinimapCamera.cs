using UnityEngine;

// Keeps a top-right minimap camera centered on the player. Renders to a
// RenderTexture displayed by a UI RawImage - see PendingTasks editor setup.
public class MinimapCamera : MonoBehaviour
{
    private Transform player;

    private void Start()
    {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    private void LateUpdate()
    {
        if (player == null) return;

        Vector3 pos = player.position;
        pos.z = transform.position.z;
        transform.position = pos;
    }
}

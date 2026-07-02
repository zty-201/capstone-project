using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    [SerializeField] private Vector3 offset;

    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
    }

    private void LateUpdate()
    {
        Vector3 pos = cam.transform.position + offset;
        pos.z = transform.position.z;
        transform.position = pos;
    }
}

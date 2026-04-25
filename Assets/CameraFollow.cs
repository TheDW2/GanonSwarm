using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow Settings")]
    [Range(1f, 20f)]
    public float smoothSpeed = 5f;       // Higher = snappier, Lower = more delayed

    [Header("Zoom")]
    public float cameraSize = 10f;       // Orthographic size — higher = more zoomed out

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (cam.orthographic)
            cam.orthographicSize = cameraSize;

        // Snap to player immediately on start (no delay on first frame)
        if (target != null)
        {
            Vector3 startPos = target.position;
            startPos.z = transform.position.z;
            transform.position = startPos;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Allow live adjustments in inspector during play mode
        if (cam.orthographic)
            cam.orthographicSize = cameraSize;
    }
}
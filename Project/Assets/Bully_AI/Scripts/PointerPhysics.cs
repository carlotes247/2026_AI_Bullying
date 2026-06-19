using UnityEngine;

/// <summary>
/// Follows the mouse pointer in the physics loop
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PointerPhysics : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How fast the rigidbody moves towards the cursor.")]
    public float speed = 10f;          // units per second
 
    [Tooltip("If true, uses MovePosition (interpolated) instead of velocity.")]
    public bool useMovePosition = false;

    [Header("Debug / Visual")]
    [Tooltip("Draw a gizmo showing the target position on the plane.")]
    public bool drawGizmo = true;
    public Color gizmoColor = Color.cyan;
    [SerializeField]
    private Vector3 targetPos;
    [Header("zOffset Settings")]
    [SerializeField]
    private Vector2 zOffsetBounds = Vector2.one * 10f;
    [SerializeField]
    private float zOffset = 10f;
    [SerializeField]
    private float zOffsetMult = 1f;

    // Cached references
    private Rigidbody rb;
    private Camera mainCam;

    void Awake()
    {
        //rb   = GetComponent<Rigidbody>();
        mainCam = Camera.main;
        if (mainCam == null)
            Debug.LogError("MouseFollower: No MainCamera found in the scene!");

    }

    /// <summary>
    /// Called once per physics step.
    /// </summary>
    void FixedUpdate()
    {
        Vector3 mousePos = Input.mousePosition;
        float mouseDelta = Input.mouseScrollDelta.y;
        // Ensure the z axis will never be out of bounds
        zOffset += mouseDelta * zOffsetMult;
        if (zOffset < zOffsetBounds.x)
            zOffset = zOffsetBounds.x;
        else if (zOffset > zOffsetBounds.y)
            zOffset = zOffsetBounds.y;
        mousePos.z = zOffset;

        targetPos = mainCam.ScreenToWorldPoint(mousePos);

        if (rb == null) return;
        // 4. Move the rigidbody towards that position
        if (useMovePosition)
        {
            // Interpolated movement – great for “exact following”
            rb.MovePosition(Vector3.Lerp(rb.position, targetPos, speed * Time.fixedDeltaTime));
        }
        else
        {
            // Velocity‑based approach – lets physics simulate the motion
            Vector3 desiredVelocity = (targetPos - rb.position).normalized * speed;
            rb.linearVelocity = desiredVelocity;
        }
    }

    public void Grab(Interactable obj)
    {
        if (obj == null) return;
        if (!obj.InMouse) return;
        obj.Grabbed = true;
        rb = obj.GetComponent<Rigidbody>();
        Debug.Log($"Grabbing {obj.name}");
    }

    public void Release(Interactable obj)
    {
        if (obj == null) return;
        obj.Grabbed = false;
        if (rb == obj.GetComponent<Rigidbody>())
            rb = null;
        Debug.Log($"Releasing {obj.name}");
    }
}


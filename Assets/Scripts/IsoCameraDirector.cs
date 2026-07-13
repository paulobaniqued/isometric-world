using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Drives a single isometric camera between four preset viewpoints (one per
/// building) with a quick eased translate/zoom. Keys Q/W/E/R jump to
/// viewpoints 1..4 (Cam1 -> Cam2 -> Cam3 -> Cam4).
///
/// The viewpoints are plain Transform anchors placed in the scene; this is the
/// only Camera in the scene and it animates to match each anchor's exact
/// position and rotation (orthographic size is taken from <see cref="orthoSizes"/>).
/// </summary>
[RequireComponent(typeof(Camera))]
public class IsoCameraDirector : MonoBehaviour
{
    [Tooltip("Target camera poses, selected by Q/W/E/R in order.")]
    public Transform[] viewpoints = new Transform[4];

    [Tooltip("Orthographic size to use at each viewpoint (parallel to viewpoints).")]
    public float[] orthoSizes = new float[4];

    [Tooltip("Seconds for a full transition between two viewpoints.")]
    public float transitionDuration = 0.5f;

    [Tooltip("Viewpoint the camera starts on (0 = Cam1).")]
    public int startIndex = 0;

    Camera cam;
    int targetIndex;

    // interpolation state
    Vector3 fromPos, toPos;
    Quaternion fromRot, toRot;
    float fromSize, toSize;
    float u = 1f;   // 1 = settled

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        targetIndex = Mathf.Clamp(startIndex, 0, Mathf.Max(0, viewpoints.Length - 1));
        SnapTo(targetIndex);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.qKey.wasPressedThisFrame) GoTo(0);
            else if (kb.wKey.wasPressedThisFrame) GoTo(1);
            else if (kb.eKey.wasPressedThisFrame) GoTo(2);
            else if (kb.rKey.wasPressedThisFrame) GoTo(3);
        }

        if (u < 1f)
        {
            u = Mathf.Clamp01(u + Time.deltaTime / Mathf.Max(transitionDuration, 0.0001f));
            float e = u * u * (3f - 2f * u);   // smoothstep ease in/out
            transform.position = Vector3.Lerp(fromPos, toPos, e);
            transform.rotation = Quaternion.Slerp(fromRot, toRot, e);
            cam.orthographicSize = Mathf.Lerp(fromSize, toSize, e);
        }
    }

    /// <summary>Begin an animated transition to viewpoint <paramref name="i"/>.</summary>
    public void GoTo(int i)
    {
        if (viewpoints == null || i < 0 || i >= viewpoints.Length || viewpoints[i] == null) return;
        targetIndex = i;
        fromPos = transform.position;
        fromRot = transform.rotation;
        fromSize = cam.orthographicSize;
        toPos = viewpoints[i].position;
        toRot = viewpoints[i].rotation;
        toSize = SizeFor(i, cam.orthographicSize);
        u = 0f;
    }

    void SnapTo(int i)
    {
        if (viewpoints == null || i < 0 || i >= viewpoints.Length || viewpoints[i] == null) return;
        transform.SetPositionAndRotation(viewpoints[i].position, viewpoints[i].rotation);
        cam.orthographicSize = SizeFor(i, cam.orthographicSize);
        u = 1f;
    }

    float SizeFor(int i, float fallback)
        => (orthoSizes != null && i < orthoSizes.Length && orthoSizes[i] > 0f) ? orthoSizes[i] : fallback;
}

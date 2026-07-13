using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Single isometric camera that frames one room/building at a time and animates
/// (translate + zoom) to the next target when Q/W/E/R are pressed (viewpoint 0..3).
///
/// Two ways to specify the four targets:
///   * Anchor mode  - assign <see cref="viewpoints"/> (Transform per target) and
///     optional <see cref="orthoSizes"/>. Used by the L-shaped community builder.
///   * Baked mode   - assign <see cref="positions"/>, <see cref="eulerAngles"/> and
///     the target half-extents (<see cref="halfHeights"/>/<see cref="halfWidths"/>);
///     the orthographic size is derived from the live screen aspect so the target
///     fills the view at any resolution. Used to frame each room to fill the screen.
/// If any viewpoint Transform is assigned, anchor mode wins.
/// </summary>
[RequireComponent(typeof(Camera))]
public class IsoCameraDirector : MonoBehaviour
{
    [Header("Anchor mode (optional)")]
    [Tooltip("Target camera poses as Transforms, selected by Q/W/E/R in order.")]
    public Transform[] viewpoints;

    [Tooltip("Orthographic size at each anchor (parallel to viewpoints).")]
    public float[] orthoSizes;

    [Header("Baked mode (fill-the-screen)")]
    [Tooltip("Camera world position for each viewpoint.")]
    public Vector3[] positions = new Vector3[4];

    [Tooltip("Camera euler rotation for each viewpoint.")]
    public Vector3[] eulerAngles = new Vector3[4];

    [Tooltip("Target world half-height (screen-up extent) per viewpoint.")]
    public float[] halfHeights = new float[4];

    [Tooltip("Target world half-width (screen-right extent) per viewpoint.")]
    public float[] halfWidths = new float[4];

    [Tooltip("1.0 = target exactly touches screen edges; >1 leaves a margin (zooms out).")]
    public float padding = 1.10f;

    [Header("Transition")]
    [Tooltip("Seconds for a full transition between two viewpoints.")]
    public float transitionDuration = 0.6f;

    [Tooltip("Viewpoint the camera starts on (0 = Q).")]
    public int startIndex = 0;

    Camera cam;
    Vector3 fromPos, toPos;
    Quaternion fromRot, toRot;
    float fromSize, toSize;
    float u = 1f;   // 1 = settled

    bool UseAnchors()
    {
        if (viewpoints == null) return false;
        for (int k = 0; k < viewpoints.Length; k++)
            if (viewpoints[k] != null) return true;
        return false;
    }

    int Count => UseAnchors() ? viewpoints.Length : Mathf.Min(positions.Length, eulerAngles.Length);

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        int i = Mathf.Clamp(startIndex, 0, Mathf.Max(0, Count - 1));
        if (TryPose(i, out var p, out var r, out var s))
        {
            transform.SetPositionAndRotation(p, r);
            cam.orthographicSize = s;
        }
        u = 1f;
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
            u = Mathf.Clamp01(u + Time.deltaTime / Mathf.Max(transitionDuration, 1e-4f));
            float e = u * u * (3f - 2f * u);   // smoothstep ease in/out
            transform.position = Vector3.Lerp(fromPos, toPos, e);
            transform.rotation = Quaternion.Slerp(fromRot, toRot, e);
            cam.orthographicSize = Mathf.Lerp(fromSize, toSize, e);
        }
    }

    /// <summary>Begin an animated transition to viewpoint <paramref name="i"/>.</summary>
    public void GoTo(int i)
    {
        if (!TryPose(i, out var p, out var r, out var s)) return;
        fromPos = transform.position;
        fromRot = transform.rotation;
        fromSize = cam.orthographicSize;
        toPos = p;
        toRot = r;
        toSize = s;
        u = 0f;
    }

    bool TryPose(int i, out Vector3 pos, out Quaternion rot, out float size)
    {
        pos = transform.position;
        rot = transform.rotation;
        size = cam != null ? cam.orthographicSize : 10f;

        if (UseAnchors())
        {
            if (viewpoints == null || i < 0 || i >= viewpoints.Length || viewpoints[i] == null) return false;
            pos = viewpoints[i].position;
            rot = viewpoints[i].rotation;
            if (orthoSizes != null && i < orthoSizes.Length && orthoSizes[i] > 0f) size = orthoSizes[i];
            return true;
        }

        if (positions == null || i < 0 || i >= Count) return false;
        pos = positions[i];
        rot = Quaternion.Euler(eulerAngles[i]);
        size = SizeFor(i);
        return true;
    }

    /// <summary>Orthographic size that makes baked target i fill the current screen aspect.</summary>
    public float SizeFor(int i)
    {
        float aspect = (cam != null && cam.aspect > 0.01f) ? cam.aspect : 16f / 9f;
        float hh = (halfHeights != null && i < halfHeights.Length) ? halfHeights[i] : 6f;
        float hw = (halfWidths != null && i < halfWidths.Length) ? halfWidths[i] : 6f;
        return Mathf.Max(hh, hw / aspect) * Mathf.Max(padding, 0.01f);
    }
}

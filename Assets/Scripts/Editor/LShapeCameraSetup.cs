using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Sets up a SINGLE isometric camera in IsometricWorld-Lshape.unity that frames
/// one room at a time (zoomed so the room fills the screen) and animates to the
/// next room on Q/W/E/R via <see cref="IsoCameraDirector"/>.
///
/// The four viewpoints are computed from each room group's renderer bounds, so no
/// preset camera objects are needed. Order: Q=Bedroom, W=Assembly, E=Lab, R=Storage.
/// Only the camera is created; no other game objects are modified.
/// </summary>
public static class LShapeCameraSetup
{
    const string ScenePath = "Assets/Scenes/IsometricWorld-Lshape.unity";

    // Q, W, E, R -> these room groups, in this order.
    static readonly string[] RoomGroups = { "Bedroom", "Assembly", "Lab", "Storage" };

    static readonly Vector3 IsoEuler = new Vector3(35.264f, 45f, 0f);
    const float CamDistance = 200f;
    const float Padding = 1.10f;   // ~5% more margin than a tight fit (zoomed out)
    const float VerifyAspect = 1280f / 720f;

    [MenuItem("Tools/Isometric World/Setup L-Shape Global Camera")]
    public static void Setup()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var all = new List<Transform>();
        foreach (var root in scene.GetRootGameObjects())
            all.AddRange(root.GetComponentsInChildren<Transform>(true));

        var rot = Quaternion.Euler(IsoEuler);
        Vector3 fwd = rot * Vector3.forward, right = rot * Vector3.right, up = rot * Vector3.up;

        int n = RoomGroups.Length;
        var positions = new Vector3[n];
        var eulers = new Vector3[n];
        var halfH = new float[n];
        var halfW = new float[n];

        for (int i = 0; i < n; i++)
        {
            var group = all.FirstOrDefault(t => t.name == RoomGroups[i]);
            if (group == null) { Debug.LogError("[LShapeCameraSetup] Missing room group: " + RoomGroups[i]); return; }

            if (!TryGetBounds(group, out var b))
            {
                Debug.LogError("[LShapeCameraSetup] No renderers under " + RoomGroups[i]); return;
            }

            float hr = 0f, hu = 0f;
            foreach (var corner in Corners(b))
            {
                Vector3 d = corner - b.center;
                hr = Mathf.Max(hr, Mathf.Abs(Vector3.Dot(d, right)));
                hu = Mathf.Max(hu, Mathf.Abs(Vector3.Dot(d, up)));
            }

            positions[i] = b.center - fwd * CamDistance;
            eulers[i] = IsoEuler;
            halfH[i] = hu;
            halfW[i] = hr;
            float size = Mathf.Max(hu, hr / VerifyAspect) * Padding;
            Debug.Log($"[LShapeCameraSetup] {RoomGroups[i]} center={b.center} size={b.size} -> halfW={hr:F2} halfH={hu:F2} orthoSize@16:9={size:F2}");
        }

        // single global camera
        var go = GameObject.Find("GlobalIsoCamera");
        if (go == null) go = new GameObject("GlobalIsoCamera");
        go.tag = "MainCamera";

        var cam = go.GetComponent<Camera>();
        if (cam == null) cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Hex("#E7EBF2");
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 600f;
        cam.cullingMask = ~0;
        cam.depth = 0f;

        if (go.GetComponent<UniversalAdditionalCameraData>() == null)
            go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        if (go.GetComponent<AudioListener>() == null)
            go.AddComponent<AudioListener>();

        var dir = go.GetComponent<IsoCameraDirector>();
        if (dir == null) dir = go.AddComponent<IsoCameraDirector>();
        dir.positions = positions;
        dir.eulerAngles = eulers;
        dir.halfHeights = halfH;
        dir.halfWidths = halfW;
        dir.padding = Padding;
        dir.transitionDuration = 0.6f;
        dir.startIndex = 0;

        go.transform.SetPositionAndRotation(positions[0], Quaternion.Euler(eulers[0]));
        cam.aspect = VerifyAspect;
        cam.orthographicSize = Mathf.Max(halfH[0], halfW[0] / VerifyAspect) * Padding;
        cam.ResetAspect();

        // warn if any stray cameras remain (there shouldn't be)
        int camCount = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length;
        if (camCount > 1) Debug.LogWarning($"[LShapeCameraSetup] {camCount} cameras in scene - expected 1.");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[LShapeCameraSetup] Done. Single GlobalIsoCamera fills each room; Q/W/E/R = Bedroom/Assembly/Lab/Storage.");
    }

    /// <summary>Verification: render the fill view of each of the 4 rooms.</summary>
    public static void RenderViewpointsBatch()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var dir = Object.FindFirstObjectByType<IsoCameraDirector>();
        if (dir == null) { Debug.LogError("[LShapeCameraSetup] No director in scene."); return; }
        var cam = dir.GetComponent<Camera>();

        string outDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Screenshots");
        Directory.CreateDirectory(outDir);

        for (int i = 0; i < dir.positions.Length; i++)
        {
            cam.transform.SetPositionAndRotation(dir.positions[i], Quaternion.Euler(dir.eulerAngles[i]));
            cam.aspect = VerifyAspect;
            cam.orthographicSize = Mathf.Max(dir.halfHeights[i], dir.halfWidths[i] / VerifyAspect) * dir.padding;

            const int w = 1280, h = 720;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var req = new RenderPipeline.StandardRequest();
            if (RenderPipeline.SupportsRenderRequest(cam, req)) { req.destination = rt; RenderPipeline.SubmitRenderRequest(cam, req); }
            else { cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; }
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(Path.Combine(outDir, $"iso_lshape_stop{i + 1}.png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
        Debug.Log("[LShapeCameraSetup] Rendered 4 room fill views.");
    }

    public static void SetupAndVerifyBatch()
    {
        Setup();
        RenderViewpointsBatch();
    }

    // ---- helpers ----

    static bool TryGetBounds(Transform group, out Bounds bounds)
    {
        bounds = default;
        bool any = false;
        foreach (var r in group.GetComponentsInChildren<Renderer>(true))
        {
            if (!any) { bounds = r.bounds; any = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return any;
    }

    static IEnumerable<Vector3> Corners(Bounds b)
    {
        Vector3 c = b.center, e = b.extents;
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    yield return c + new Vector3(sx * e.x, sy * e.y, sz * e.z);
    }

    static Color Hex(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
}

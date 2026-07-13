using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Wires the IsometricWorld-Lshape scene so a SINGLE global isometric camera
/// animates between the four building viewpoints (Cam1 -> Cam2 -> Cam3 -> Cam4)
/// on keys Q/W/E/R, via <see cref="IsoCameraDirector"/>.
///
/// Cam1..Cam4 are kept only as transform anchors (their Camera/AudioListener
/// components are removed) so the scene contains exactly one Camera. No other
/// game objects are touched.
/// </summary>
public static class LShapeCameraSetup
{
    const string ScenePath = "Assets/Scenes/IsometricWorld-Lshape.unity";
    static readonly string[] CamNames = { "Cam1", "Cam2", "Cam3", "Cam4" };

    [MenuItem("Tools/Isometric World/Setup L-Shape Global Camera")]
    public static void Setup()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var all = new List<Transform>();
        foreach (var root in scene.GetRootGameObjects())
            all.AddRange(root.GetComponentsInChildren<Transform>(true));

        var anchors = new Transform[4];
        var sizes = new float[4];
        Camera template = null;
        for (int i = 0; i < 4; i++)
        {
            var t = all.FirstOrDefault(x => x.name == CamNames[i]);
            if (t == null) { Debug.LogError("[LShapeCameraSetup] Missing " + CamNames[i]); return; }
            anchors[i] = t;
            var c = t.GetComponent<Camera>();
            sizes[i] = (c != null && c.orthographic) ? c.orthographicSize : 10.5f;
            if (c != null && template == null) template = c;
            Debug.Log($"[LShapeCameraSetup] {CamNames[i]} pos={t.position} rot={t.eulerAngles} ortho={sizes[i]}");
        }

        // one global camera, render settings copied from Cam1
        // (Unity fake-null means we must use explicit '== null' checks, not '??')
        var go = GameObject.Find("GlobalIsoCamera");
        if (go == null) go = new GameObject("GlobalIsoCamera");
        go.tag = "MainCamera";
        var cam = go.GetComponent<Camera>();
        if (cam == null) cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        if (template != null)
        {
            cam.clearFlags = template.clearFlags;
            cam.backgroundColor = template.backgroundColor;
            cam.nearClipPlane = template.nearClipPlane;
            cam.farClipPlane = template.farClipPlane;
            cam.cullingMask = template.cullingMask;
            cam.depth = template.depth;
        }
        if (go.GetComponent<UniversalAdditionalCameraData>() == null)
            go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        if (go.GetComponent<AudioListener>() == null)
            go.AddComponent<AudioListener>();

        var dir = go.GetComponent<IsoCameraDirector>();
        if (dir == null) dir = go.AddComponent<IsoCameraDirector>();
        dir.viewpoints = anchors;
        dir.orthoSizes = sizes;
        dir.transitionDuration = 0.5f;
        dir.startIndex = 0;

        go.transform.SetPositionAndRotation(anchors[0].position, anchors[0].rotation);
        cam.orthographicSize = sizes[0];

        // reduce Cam1..Cam4 to plain transform anchors so only one Camera remains
        foreach (var a in anchors)
        {
            var addl = a.GetComponent<UniversalAdditionalCameraData>();
            if (addl != null) Object.DestroyImmediate(addl, true);
            var al = a.GetComponent<AudioListener>();
            if (al != null) Object.DestroyImmediate(al, true);
            var c = a.GetComponent<Camera>();
            if (c != null) Object.DestroyImmediate(c, true);
            if (a.CompareTag("MainCamera")) a.tag = "Untagged";
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[LShapeCameraSetup] Done. Single GlobalIsoCamera wired to Cam1..Cam4 (Q/W/E/R).");
    }

    /// <summary>Verification: render the global camera at each of the 4 stops.</summary>
    public static void RenderViewpointsBatch()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var dir = Object.FindFirstObjectByType<IsoCameraDirector>();
        if (dir == null) { Debug.LogError("[LShapeCameraSetup] No director in scene."); return; }
        var cam = dir.GetComponent<Camera>();

        string dirOut = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Screenshots");
        Directory.CreateDirectory(dirOut);

        for (int i = 0; i < dir.viewpoints.Length; i++)
        {
            var vp = dir.viewpoints[i];
            if (vp == null) continue;
            cam.transform.SetPositionAndRotation(vp.position, vp.rotation);
            cam.orthographicSize = (dir.orthoSizes != null && i < dir.orthoSizes.Length && dir.orthoSizes[i] > 0f)
                                   ? dir.orthoSizes[i] : cam.orthographicSize;

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
            File.WriteAllBytes(Path.Combine(dirOut, $"iso_lshape_stop{i + 1}.png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
        Debug.Log("[LShapeCameraSetup] Rendered 4 viewpoint stops.");
    }

    public static void SetupAndVerifyBatch()
    {
        Setup();
        RenderViewpointsBatch();
    }
}

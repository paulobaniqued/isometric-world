using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Adds the runtime <see cref="SceneAnimator"/> to IsometricWorld-Lshape.unity
/// (one small controller object; no existing objects are touched) and can render
/// before/after frames per room to confirm the motion works headlessly.
/// </summary>
public static class SceneAnimatorSetup
{
    const string ScenePath = "Assets/Scenes/IsometricWorld-Lshape.unity";

    [MenuItem("Tools/Isometric World/Add Scene Animations")]
    public static void Setup()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var go = GameObject.Find("SceneAnimator");
        if (go == null) go = new GameObject("SceneAnimator");
        if (go.GetComponent<SceneAnimator>() == null) go.AddComponent<SceneAnimator>();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SceneAnimatorSetup] SceneAnimator added; play the scene to see the motion.");
    }

    /// <summary>Render before/after frames per room (does NOT save the stepped poses).</summary>
    public static void VerifyBatch()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var anim = Object.FindFirstObjectByType<SceneAnimator>();
        if (anim == null) { Debug.LogError("[SceneAnimatorSetup] No SceneAnimator in scene."); return; }
        anim.CollectTargets();

        var dir = Object.FindFirstObjectByType<IsoCameraDirector>();
        var cam = dir.GetComponent<Camera>();
        string outDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Screenshots");
        Directory.CreateDirectory(outDir);

        // (room index in director order: 0=Bedroom 1=Assembly 2=Lab 3=Storage)
        Frame(anim, cam, dir, 0, 0.0f, outDir, "anim_bedroom_a");
        Frame(anim, cam, dir, 0, 1.1f, outDir, "anim_bedroom_b");
        Frame(anim, cam, dir, 1, 0.0f, outDir, "anim_assembly_a");
        Frame(anim, cam, dir, 1, 0.8f, outDir, "anim_assembly_b");
        Frame(anim, cam, dir, 2, 0.0f, outDir, "anim_lab_a");
        Frame(anim, cam, dir, 2, 0.95f, outDir, "anim_lab_b");
        Frame(anim, cam, dir, 3, 0.0f, outDir, "anim_storage_a");
        Frame(anim, cam, dir, 3, 1.7f, outDir, "anim_storage_b");
        Debug.Log("[SceneAnimatorSetup] Rendered before/after animation frames.");
    }

    public static void SetupAndVerifyBatch()
    {
        Setup();
        VerifyBatch();
    }

    static void Frame(SceneAnimator anim, Camera cam, IsoCameraDirector dir, int room, float t, string dirOut, string name)
    {
        cam.transform.SetPositionAndRotation(dir.positions[room], Quaternion.Euler(dir.eulerAngles[room]));
        cam.aspect = 1280f / 720f;
        cam.orthographicSize = Mathf.Max(dir.halfHeights[room], dir.halfWidths[room] / cam.aspect) * dir.padding;
        anim.Step(t);

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
        File.WriteAllBytes(Path.Combine(dirOut, name + ".png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);
    }
}

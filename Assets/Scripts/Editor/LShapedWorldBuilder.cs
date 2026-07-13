using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Builds the ALTERNATIVE diorama "IsometricWorld-Lshaped": the same four rooms,
/// but each is its own roofless building sited along an L-shaped street with roads,
/// cars, trees and walkways. This is fully independent of IsometricWorldBuilder -
/// it writes a separate scene and shares only the FBX asset library + ClayMatte look.
///
///   Arm A (road along +X):   [Bedroom house]  [Assembly warehouse]
///   90-degree corner
///   Arm B (road along +Z):   [Robotics lab]   [Storage warehouse]
///
/// All buildings are axis-aligned with the two camera-far walls (N=+Z, E=+X) full
/// height and the two camera-near walls (S=-Z, W=-X) as low curbs, so the roofless
/// interiors stay readable from the SW isometric camera.
/// </summary>
public static class LShapedWorldBuilder
{
    const string ScenePath = "Assets/Scenes/IsometricWorld-Lshaped.unity";

    static Material clay, clayFloor, ground, road, walk, line;
    static System.Random rng;

    // ------------------------------------------------------------ menu items

    [MenuItem("Tools/Isometric World/Build L-Shaped World")]
    public static void BuildWorld()
    {
        AssetDatabase.Refresh();
        rng = new System.Random(7);

        clay      = EnsureMat("Assets/Materials/ClayMatte.mat", Hex("#E8EAF0"), 0.05f);
        clayFloor = EnsureMat("Assets/Materials/ClayFloor.mat", Hex("#DCE0EA"), 0.05f);
        ground    = EnsureMat("Assets/Materials/Ground.mat",   Hex("#E6EAF2"), 0.03f);
        road      = EnsureMat("Assets/Materials/Road.mat",     Hex("#C3CAD9"), 0.04f);
        walk      = EnsureMat("Assets/Materials/Walkway.mat",  Hex("#D4DAE6"), 0.05f);
        line      = EnsureMat("Assets/Materials/RoadLine.mat", Hex("#EEF0F6"), 0.05f);

        EnsureSSAO("Assets/Settings/PC_Renderer.asset");
        EnsureSSAO("Assets/Settings/Mobile_Renderer.asset");
        ExtendShadowDistance("Assets/Settings/PC_RPAsset.asset");
        ExtendShadowDistance("Assets/Settings/Mobile_RPAsset.asset");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("IsometricWorldLshaped").transform;

        BuildGroundAndRoads(Group("Streets", root));
        BuildBedroomHouse(Group("Bedroom_House", root));
        BuildAssemblyWarehouse(Group("Assembly_Warehouse", root));
        BuildRoboticsLab(Group("Robotics_Lab", root));
        BuildStorageWarehouse(Group("Storage_Warehouse", root));
        BuildLightingAndCamera();

        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[LShapedWorldBuilder] World built and saved to " + ScenePath);
    }

    [MenuItem("Tools/Isometric World/Render L-Shaped Screenshot")]
    public static void RenderScreenshot()
    {
        var cam = Camera.main;
        if (cam == null) cam = Object.FindFirstObjectByType<Camera>();
        if (cam == null) { Debug.LogError("No camera - build the L-shaped world first."); return; }
        RenderCameraTo(cam, "iso_world_lshaped.png");
    }

    static void RenderCameraTo(Camera cam, string filename)
    {
        const int w = 1920, h = 1080;
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        var prevTarget = cam.targetTexture;

        var request = new RenderPipeline.StandardRequest();
        if (RenderPipeline.SupportsRenderRequest(cam, request))
        {
            request.destination = rt;
            RenderPipeline.SubmitRenderRequest(cam, request);
        }
        else { cam.targetTexture = rt; cam.Render(); }

        var prevActive = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        RenderTexture.active = prevActive;
        cam.targetTexture = prevTarget;

        string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Screenshots");
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, filename), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);
        Debug.Log("[LShapedWorldBuilder] Screenshot saved to " + filename);
    }

    /// <summary>Batch entry: builds, then renders a wide overview of the community.</summary>
    public static void BuildAndScreenshotBatch()
    {
        BuildWorld();
        var cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        var rot = Quaternion.Euler(35.264f, 45f, 0);
        cam.transform.SetPositionAndRotation(new Vector3(15.5f, 0, 21f) - rot * Vector3.forward * 120f, rot);
        cam.orthographicSize = 17.5f;
        RenderCameraTo(cam, "iso_world_lshaped.png");
    }

    /// <summary>Diagnostic: build, then render the overview plus each of the 4 presets.</summary>
    public static void BuildAndVerifyViewsBatch()
    {
        BuildAndScreenshotBatch();   // overview -> iso_world_lshaped.png
        var cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        var dir = Object.FindFirstObjectByType<IsoCameraDirector>();
        if (dir == null) { Debug.LogError("No IsoCameraDirector found."); return; }
        for (int i = 0; i < dir.viewpoints.Length; i++)
        {
            var a = dir.viewpoints[i];
            if (a == null) continue;
            cam.transform.SetPositionAndRotation(a.position, a.rotation);
            cam.orthographicSize = dir.orthoSizes[i];
            RenderCameraTo(cam, $"iso_lshaped_cam{i + 1}.png");
        }
    }

    /// <summary>Diagnostic: reopen the SAVED scene and confirm the single-camera +
    /// director wiring survived serialization, then render one preset from it.</summary>
    public static void CheckPersistedBatch()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        int cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Length;
        var dir = Object.FindFirstObjectByType<IsoCameraDirector>();
        int vp = dir?.viewpoints?.Length ?? -1;
        int nonNull = dir?.viewpoints?.Count(v => v != null) ?? -1;
        Debug.Log($"[CheckPersisted] cameras={cams} director={(dir != null)} viewpoints={vp} nonNull={nonNull} sizes={(dir?.orthoSizes?.Length ?? -1)}");
        if (dir != null && vp >= 3 && dir.viewpoints[2] != null)
        {
            var cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            cam.transform.SetPositionAndRotation(dir.viewpoints[2].position, dir.viewpoints[2].rotation);
            cam.orthographicSize = dir.orthoSizes[2];
            RenderCameraTo(cam, "iso_persist_cam3.png");
        }
    }

    // ------------------------------------------------------- ground & streets

    // Road centrelines: arm A along X at z=0, arm B along Z at x=0, corner at origin.
    const float RoadHalf = 3.2f;   // half road width
    const float AxMin = -6f, AxMax = 40f;   // arm A extent (x)
    const float BzMin = -6f, BzMax = 45f;   // arm B extent (z)

    static void BuildGroundAndRoads(Transform parent)
    {
        // big ground plane
        Box(parent, "Ground", new Vector3(17f, -0.35f, 20f), new Vector3(74f, 0.5f, 74f), ground);

        // L-shaped road: two overlapping strips, corner square shared at origin
        Box(parent, "RoadA", new Vector3((AxMin + AxMax) / 2f, 0.02f, 0f),
            new Vector3(AxMax - AxMin, 0.06f, RoadHalf * 2f), road);
        Box(parent, "RoadB", new Vector3(0f, 0.02f, (BzMin + BzMax) / 2f),
            new Vector3(RoadHalf * 2f, 0.06f, BzMax - BzMin), road);

        // dashed centre lines (skip the corner square)
        for (float x = AxMin + 2f; x < AxMax; x += 3f)
        {
            if (x > -RoadHalf - 1f && x < RoadHalf + 1f) continue;
            Box(parent, "LaneA", new Vector3(x, 0.06f, 0f), new Vector3(1.3f, 0.03f, 0.16f), line);
        }
        for (float z = BzMin + 2f; z < BzMax; z += 3f)
        {
            if (z > -RoadHalf - 1f && z < RoadHalf + 1f) continue;
            Box(parent, "LaneB", new Vector3(0f, 0.06f, z), new Vector3(0.16f, 0.03f, 1.3f), line);
        }

        // sidewalk kerbs flanking the outer edges of each arm
        Box(parent, "KerbA", new Vector3((0f + AxMax) / 2f, 0.10f, RoadHalf + 0.35f),
            new Vector3(AxMax, 0.2f, 0.5f), walk);
        Box(parent, "KerbB", new Vector3(RoadHalf + 0.35f, 0.10f, (0f + BzMax) / 2f),
            new Vector3(0.5f, 0.2f, BzMax), walk);

        // a couple of cars travelling the streets
        Place(parent, "Vehicles/car", new Vector3(15f, 0, -1.4f), 90);
        Place(parent, "Vehicles/van", new Vector3(-1.4f, 0, 20f), 0);
        Place(parent, "Vehicles/car", new Vector3(1.5f, 0, 34f), 180);

        // street trees along the kerbs
        for (float x = 6f; x < AxMax - 2f; x += 7.5f) Tree(parent, new Vector3(x, 0, RoadHalf + 1.4f), 0.9f);
        for (float z = 9f; z < BzMax - 2f; z += 7.5f) Tree(parent, new Vector3(RoadHalf + 1.4f, 0, z), 0.9f);
    }

    static void Walkway(Transform parent, Vector3 a, Vector3 b, float width)
    {
        Vector3 c = (a + b) / 2f;
        bool alongZ = Mathf.Abs(b.z - a.z) > Mathf.Abs(b.x - a.x);
        Vector3 size = alongZ ? new Vector3(width, 0.08f, Mathf.Abs(b.z - a.z))
                              : new Vector3(Mathf.Abs(b.x - a.x), 0.08f, width);
        Box(parent, "Walkway", new Vector3(c.x, 0.04f, c.z), size, walk);
    }

    // ------------------------------------------------------------- buildings

    /// <summary>Roofless shell: floor, tall N+E walls, low S+W curbs, optional parapet.</summary>
    static Transform Building(Transform parent, Vector3 center, float w, float d, float wallH,
                              bool parapet = false)
    {
        var g = Group("Shell", parent);
        const float t = 0.3f, curb = 0.45f;
        float x0 = center.x - w / 2, x1 = center.x + w / 2, z0 = center.z - d / 2, z1 = center.z + d / 2;

        Box(g, "Floor", new Vector3(center.x, -0.06f, center.z), new Vector3(w + 0.3f, 0.2f, d + 0.3f), clayFloor);
        Box(g, "Wall_N", new Vector3(center.x, wallH / 2, z1), new Vector3(w + t, wallH, t), clay);
        Box(g, "Wall_E", new Vector3(x1, wallH / 2, center.z), new Vector3(t, wallH, d + t), clay);
        Box(g, "Curb_S", new Vector3(center.x, curb / 2, z0), new Vector3(w + t, curb, t), clay);
        Box(g, "Curb_W", new Vector3(x0, curb / 2, center.z), new Vector3(t, curb, d + t), clay);

        if (parapet)
        {
            Box(g, "Cap_N", new Vector3(center.x, wallH + 0.08f, z1), new Vector3(w + t + 0.14f, 0.16f, t + 0.14f), clay);
            Box(g, "Cap_E", new Vector3(x1, wallH + 0.08f, center.z), new Vector3(t + 0.14f, 0.16f, d + t + 0.14f), clay);
        }
        return g;
    }

    static void BuildBedroomHouse(Transform parent)
    {
        Vector3 c = new Vector3(9f, 0, 9.8f);
        float w = 7.4f, d = 7.0f;
        Building(parent, c, w, d, 2.7f);

        // decorative window recess on the tall N wall behind the bed
        Box(parent, "Window", new Vector3(c.x + 1.2f, 1.5f, c.z + d / 2 - 0.02f),
            new Vector3(1.7f, 1.0f, 0.12f), clayFloor);

        // sleeping + study against the tall N/E walls
        Place(parent, "Furniture/bed", new Vector3(c.x + 1.1f, 0, c.z + 1.7f), 180);
        Scaled(parent, "Furniture/unit_box_beveled", new Vector3(c.x + 2.5f, 0, c.z + 2.4f), 0,
               new Vector3(0.5f, 0.5f, 0.42f));
        Place(parent, "Furniture/desk", new Vector3(c.x - 2.0f, 0, c.z + 2.35f), 180);
        Place(parent, "Furniture/monitor_keyboard", new Vector3(c.x - 2.0f, 0.76f, c.z + 2.4f), 180);
        Place(parent, "Furniture/office_chair", new Vector3(c.x - 2.0f, 0, c.z + 1.4f), 5);
        Place(parent, "Furniture/cabinet", new Vector3(c.x + w / 2 - 0.6f, 0, c.z - 0.4f), -90);
        Place(parent, "Furniture/bookshelf", new Vector3(c.x - 0.4f, 0, c.z + d / 2 - 0.35f), 180);

        Cylinder(parent, "Rug", new Vector3(c.x - 0.3f, 0.02f, c.z - 0.9f), new Vector3(3.3f, 0.02f, 2.6f), clay);
        Place(parent, "Characters/person_vr", new Vector3(c.x - 0.3f, 0, c.z - 1.0f), 215);

        // front yard: walkway to arm A road + trees
        Walkway(parent, new Vector3(c.x, 0, c.z - d / 2), new Vector3(c.x, 0, RoadHalf), 1.8f);
        Tree(parent, new Vector3(c.x - w / 2 - 0.8f, 0, c.z - 2.2f), 0.85f);
        Tree(parent, new Vector3(c.x + w / 2 + 0.8f, 0, c.z - 2.6f), 0.75f);
        Tree(parent, new Vector3(c.x - 2.4f, 0, c.z - d / 2 - 1.6f), 0.7f);
    }

    static void BuildAssemblyWarehouse(Transform parent)
    {
        Vector3 c = new Vector3(28f, 0, 12f);
        float w = 13f, d = 10f;
        Building(parent, c, w, d, 3.4f);

        // production line runs along X, deep in the shed
        Place(parent, "Machines/conveyor_belt", new Vector3(c.x, 0, c.z + 2.4f), 0);
        Place(parent, "Robots/robot_arm", new Vector3(c.x - 1.4f, 0, c.z + 3.45f), 180);
        Place(parent, "Robots/robot_arm", new Vector3(c.x + 1.4f, 0, c.z + 1.35f), 0);
        Place(parent, "Machines/control_panel", new Vector3(c.x + w / 2 - 1.3f, 0, c.z + 2.4f), -90);

        // PC desk crew watching the line
        Place(parent, "Furniture/desk", new Vector3(c.x, 0, c.z - 1.4f), 0);
        Place(parent, "Furniture/monitor_keyboard", new Vector3(c.x - 0.4f, 0.76f, c.z - 1.45f), 0);
        Place(parent, "Furniture/monitor_keyboard", new Vector3(c.x + 0.5f, 0.76f, c.z - 1.38f), 8);
        Place(parent, "Furniture/office_chair", new Vector3(c.x, 0, c.z - 2.2f), 180);
        Place(parent, "Characters/person_sitting", new Vector3(c.x, 0.03f, c.z - 2.2f), 0);
        Place(parent, "Characters/person_standing_a", new Vector3(c.x - 1.1f, 0, c.z - 2.7f), 15);
        Place(parent, "Characters/person_standing_b", new Vector3(c.x + 1.2f, 0, c.z - 2.6f), -12);

        Place(parent, "Furniture/pallet", new Vector3(c.x + w / 2 - 1.2f, 0, c.z - 2.6f), 10);
        BoxStack(parent, new Vector3(c.x + w / 2 - 1.2f, 0.16f, c.z - 2.6f), 10, 2);
        Place(parent, "Furniture/crate", new Vector3(c.x - w / 2 + 1.0f, 0, c.z + 2.2f), 12);

        // parking lot in front + walkway
        Walkway(parent, new Vector3(c.x - 3.5f, 0, c.z - d / 2), new Vector3(c.x - 3.5f, 0, RoadHalf), 1.8f);
        Place(parent, "Vehicles/car", new Vector3(c.x + 1.0f, 0, c.z - d / 2 - 2.2f), 0);
        Place(parent, "Vehicles/van", new Vector3(c.x + 3.4f, 0, c.z - d / 2 - 2.3f), 0);
        Tree(parent, new Vector3(c.x + w / 2 + 0.9f, 0, c.z - 1.0f), 0.85f);
        Tree(parent, new Vector3(c.x - w / 2 - 0.9f, 0, c.z - 2.0f), 0.8f);
    }

    static void BuildRoboticsLab(Transform parent)
    {
        Vector3 c = new Vector3(11.5f, 0, 24f);
        float w = 12f, d = 10f;
        Building(parent, c, w, d, 3.3f, parapet: true);   // "nicer" building

        // hero: hanging humanoid on the gantry, studied by scientists
        Place(parent, "Furniture/gantry_frame", new Vector3(c.x + 0.5f, 0, c.z + 0.8f), 90);
        Place(parent, "Robots/robot_humanoid_hanging", new Vector3(c.x + 0.5f, 0, c.z + 0.8f), 285);

        Place(parent, "Furniture/lab_bench_terminal", new Vector3(c.x - 0.5f, 0, c.z + d / 2 - 0.7f), 180);
        Place(parent, "Furniture/lab_bench_equip", new Vector3(c.x + w / 2 - 0.8f, 0, c.z - 1.6f), -90);
        Place(parent, "Machines/control_panel", new Vector3(c.x - w / 2 + 1.2f, 0, c.z + 2.2f), 60);
        Place(parent, "Furniture/whiteboard", new Vector3(c.x + 3.4f, 0, c.z + d / 2 - 0.9f), 180);

        Place(parent, "Characters/scientist_pointing", new Vector3(c.x + 1.7f, 0, c.z + 1.4f), 250);
        Place(parent, "Characters/scientist_armscrossed", new Vector3(c.x - 0.7f, 0, c.z + 1.7f), 150);
        Place(parent, "Characters/scientist_tablet", new Vector3(c.x - 0.2f, 0, c.z - 1.6f), 20);

        // front (W) yard: a few cars, walkway to arm B road, trees
        Walkway(parent, new Vector3(c.x - w / 2, 0, c.z), new Vector3(RoadHalf, 0, c.z), 1.8f);
        Place(parent, "Vehicles/car", new Vector3(c.x - w / 2 - 2.1f, 0, c.z + 2.2f), 90);
        Place(parent, "Vehicles/car", new Vector3(c.x - w / 2 - 2.1f, 0, c.z - 2.0f), 90);
        Tree(parent, new Vector3(c.x - w / 2 - 1.0f, 0, c.z + d / 2 - 0.5f), 0.85f);
        Tree(parent, new Vector3(c.x - 2.0f, 0, c.z - d / 2 - 1.2f), 0.8f);
        Tree(parent, new Vector3(c.x + w / 2 + 0.9f, 0, c.z + 1.2f), 0.75f);
    }

    static void BuildStorageWarehouse(Transform parent)
    {
        Vector3 c = new Vector3(11.5f, 0, 38f);
        float w = 12f, d = 9f;
        Building(parent, c, w, d, 3.4f);

        // shelving along the tall N + E walls and a centre row
        var shelfSpots = new List<(Vector3 pos, float rot)>
        {
            (new Vector3(c.x - 3.4f, 0, c.z + d / 2 - 0.7f), 0),
            (new Vector3(c.x - 1.2f, 0, c.z + d / 2 - 0.7f), 0),
            (new Vector3(c.x + 1.0f, 0, c.z + d / 2 - 0.7f), 0),
            (new Vector3(c.x + w / 2 - 0.7f, 0, c.z - 1.4f), -90),
            (new Vector3(c.x - 0.6f, 0, c.z - 1.2f), 0),
            (new Vector3(c.x + 1.6f, 0, c.z - 1.2f), 0),
        };
        foreach (var (pos, rot) in shelfSpots)
        {
            Place(parent, "Furniture/shelf_unit", pos, rot);
            FillShelf(parent, pos, rot);
        }

        // the humanoid robot carries a crate out toward the road (W, -X)
        Place(parent, "Robots/robot_humanoid_carrying", new Vector3(c.x - 2.2f, 0, c.z - 2.6f), -75);

        Place(parent, "Furniture/pallet", new Vector3(c.x + 2.6f, 0, c.z - 3.0f), 8);
        BoxStack(parent, new Vector3(c.x + 2.6f, 0.16f, c.z - 3.0f), 8, 3);
        Place(parent, "Furniture/crate", new Vector3(c.x - 4.2f, 0, c.z - 2.8f), 14);
        Place(parent, "Furniture/crate", new Vector3(c.x - 3.6f, 0, c.z - 3.4f), -8);

        // loading yard: van + walkway to arm B road
        Walkway(parent, new Vector3(c.x - w / 2, 0, c.z - 1.5f), new Vector3(RoadHalf, 0, c.z - 1.5f), 1.8f);
        Place(parent, "Vehicles/van", new Vector3(c.x - w / 2 - 2.4f, 0, c.z + 1.5f), 90);
        Tree(parent, new Vector3(c.x - w / 2 - 1.0f, 0, c.z + d / 2 - 0.6f), 0.8f);
        Tree(parent, new Vector3(c.x + w / 2 + 0.9f, 0, c.z - 1.0f), 0.8f);
    }

    // --------------------------------------------------------------- props

    static void Tree(Transform parent, Vector3 pos, float s = 1f)
    {
        var g = Group("Tree", parent);
        Cylinder(g, "Trunk", pos + new Vector3(0, 0.55f * s, 0),
                 new Vector3(0.22f * s, 0.55f * s, 0.22f * s), clay);
        float cy = 1.35f * s;
        Sphere(g, "Canopy_a", pos + new Vector3(0, cy, 0), 1.5f * s, clay);
        Sphere(g, "Canopy_b", pos + new Vector3(0.45f * s, cy + 0.35f * s, 0.15f * s), 1.05f * s, clay);
        Sphere(g, "Canopy_c", pos + new Vector3(-0.42f * s, cy + 0.12f * s, -0.2f * s), 0.92f * s, clay);
    }

    static void FillShelf(Transform parent, Vector3 unitPos, float rotY)
    {
        float[] levels = { 0.122f, 0.822f, 1.522f };
        var rot = Quaternion.Euler(0, rotY, 0);
        foreach (float y in levels)
        {
            int n = rng.Next(2, 5);
            for (int i = 0; i < n; i++)
            {
                float s = 0.30f + (float)rng.NextDouble() * 0.22f;
                float x = -0.78f + i * (1.6f / Mathf.Max(n - 1, 1)) + (float)rng.NextDouble() * 0.12f - 0.06f;
                float zj = ((float)rng.NextDouble() - 0.5f) * 0.12f;
                Vector3 local = new Vector3(x, y, zj);
                Scaled(parent, "Furniture/unit_box_beveled", unitPos + rot * local,
                       rotY + rng.Next(-14, 14), new Vector3(s, s * (0.8f + (float)rng.NextDouble() * 0.4f), s));
            }
        }
    }

    static void BoxStack(Transform parent, Vector3 basePos, float rotY, int count)
    {
        float y = basePos.y, s0 = 0.72f;
        for (int i = 0; i < count; i++)
        {
            float s = s0 - i * 0.16f, h = s * 0.9f;
            Scaled(parent, "Furniture/unit_box_beveled",
                   new Vector3(basePos.x + ((float)rng.NextDouble() - 0.5f) * 0.08f, y,
                               basePos.z + ((float)rng.NextDouble() - 0.5f) * 0.08f),
                   rotY + rng.Next(-16, 16), new Vector3(s, h, s));
            y += h;
        }
    }

    // ------------------------------------------------------ lighting/camera

    static void BuildLightingAndCamera()
    {
        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
        light.color = Hex("#FFFDF7");
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.72f;
        light.transform.rotation = Quaternion.Euler(50, 128, 0);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Hex("#9AA6BC");
        RenderSettings.fog = false;

        // The ONE camera in the scene. It animates between four building
        // viewpoints (Q/W/E/R) via IsoCameraDirector - no free-orbit controller.
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 400f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Hex("#E7EBF2");
        camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;

        var rot = Quaternion.Euler(35.264f, 45f, 0);
        const float dist = 90f;

        // One anchor per building, all sharing the isometric rotation (any other
        // yaw would put the tall back walls in front and hide the interiors).
        var views = new (string name, Vector3 focus, float size)[]
        {
            ("Cam1_Bedroom",  new Vector3(9f,   1.6f, 9.8f), 8.6f),
            ("Cam2_Assembly", new Vector3(28f,  1.6f, 12f),  9.8f),
            ("Cam3_Lab",      new Vector3(11.5f, 1.6f, 24f), 9.2f),
            ("Cam4_Storage",  new Vector3(11.5f, 1.6f, 38f), 9.2f),
        };
        var rig = new GameObject("CameraRig").transform;
        var anchors = new Transform[4];
        var sizes = new float[4];
        for (int i = 0; i < views.Length; i++)
        {
            var a = new GameObject(views[i].name).transform;
            a.SetParent(rig, false);
            a.SetPositionAndRotation(views[i].focus - rot * Vector3.forward * dist, rot);
            anchors[i] = a;
            sizes[i] = views[i].size;
        }

        var director = camGo.AddComponent<IsoCameraDirector>();
        director.viewpoints = anchors;
        director.orthoSizes = sizes;
        director.transitionDuration = 0.5f;
        director.startIndex = 0;

        // start framed on Cam1 (bedroom); Q/W/E/R switch at runtime
        camGo.transform.SetPositionAndRotation(anchors[0].position, anchors[0].rotation);
        cam.orthographicSize = sizes[0];
    }

    // ------------------------------------------------------------- plumbing

    static Transform Group(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static GameObject Cylinder(Transform parent, string name, Vector3 center, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static GameObject Sphere(Transform parent, string name, Vector3 center, float dia, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        go.transform.localScale = Vector3.one * dia;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static GameObject Place(Transform parent, string modelPath, Vector3 pos, float rotY)
        => Scaled(parent, modelPath, pos, rotY, Vector3.one);

    static GameObject Scaled(Transform parent, string modelPath, Vector3 pos, float rotY, Vector3 scale)
    {
        string assetPath = "Assets/Models/" + modelPath + ".fbx";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogWarning("[LShapedWorldBuilder] Missing model: " + assetPath);
            return null;
        }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0, rotY, 0);
        go.transform.localScale = scale;
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
        {
            var mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = clay;
            r.sharedMaterials = mats;
        }
        return go;
    }

    static Material EnsureMat(string path, Color color, float smoothness)
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Materials"));
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }

    static void EnsureSSAO(string rendererPath)
    {
        var data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererPath);
        if (data == null) return;
        var feature = data.rendererFeatures
            .FirstOrDefault(f => f != null && f.GetType().Name == "ScreenSpaceAmbientOcclusion");
        if (feature == null) return;   // the main builder creates it; here we only ensure settings
        var fso = new SerializedObject(feature);
        SetFloat(fso, "m_Settings.Intensity", 1.7f);
        SetFloat(fso, "m_Settings.Radius", 0.5f);
        SetFloat(fso, "m_Settings.DirectLightingStrength", 0.55f);
        fso.ApplyModifiedProperties();
        EditorUtility.SetDirty(feature);
        AssetDatabase.SaveAssets();
    }

    static void SetFloat(SerializedObject so, string path, float value)
    {
        var p = so.FindProperty(path);
        if (p != null) p.floatValue = value;
    }

    static void ExtendShadowDistance(string rpAssetPath)
    {
        var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(rpAssetPath);
        if (rp == null) return;
        rp.shadowDistance = Mathf.Max(rp.shadowDistance, 220f);
        EditorUtility.SetDirty(rp);
    }
}

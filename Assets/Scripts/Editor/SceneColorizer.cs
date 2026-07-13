using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Applies a muted, solid, matte colour palette to IsometricWorld-Lshape.unity by
/// re-pointing renderer materials ONLY. No game objects are created, moved,
/// re-parented, deleted, or otherwise altered - only each renderer's material
/// slots are reassigned to shared palette materials.
///
/// People characters -> white matte. Robots -> yellow matte. Everything else gets
/// a low-saturation solid colour by object category.
/// </summary>
public static class SceneColorizer
{
    const string ScenePath = "Assets/Scenes/IsometricWorld-Lshape.unity";
    const string PaletteDir = "Assets/Materials/Palette";

    static readonly Dictionary<string, Material> _cache = new Dictionary<string, Material>();

    // ---- palette (all low-saturation, matte) ----
    static Material People => Mat("People_White", "#E9E8E3");
    static Material Robot => Mat("Robot_Yellow", "#CBB24A");
    static Material Foliage => Mat("Tree_Foliage", "#90A579");
    static Material Trunk => Mat("Tree_Trunk", "#877560");
    static Material Pot => Mat("Pot_Terracotta", "#B58C71");
    static Material Grass => Mat("Ground_Grass", "#ADB59F");
    static Material Asphalt => Mat("Road_Asphalt", "#71747B");
    static Material Concrete => Mat("Walkway_Concrete", "#C1C2BC");
    static Material Wall => Mat("Wall_Beige", "#D8D1C4");
    static Material Floor => Mat("Floor_Interior", "#C4BFB5");
    static Material Wood => Mat("Wood_Oak", "#BD9E7B");
    static Material Cardboard => Mat("Cardboard_Tan", "#CBB086");
    static Material MetalLight => Mat("Metal_Neutral", "#B6B9BF");
    static Material Steel => Mat("Metal_Steel", "#8B8F95");
    static Material Fabric => Mat("Fabric_Soft", "#9DA7B3");
    static Material Tire => Mat("Tire_Dark", "#2F2F31");
    static readonly string[] CarHex = { "#A9756C", "#7C8CA0", "#859581", "#C6BD9E", "#9EA1A6" };
    static Material Car(int i) => Mat("Car_" + i, CarHex[((i % CarHex.Length) + CarHex.Length) % CarHex.Length]);

    static readonly HashSet<string> Woody = new HashSet<string>
        { "desk", "shelf_unit", "crate", "pallet", "bookshelf", "coffee_table", "cabinet", "stool", "tool_cart" };
    static readonly HashSet<string> Soft = new HashSet<string> { "bed", "sofa", "rug" };
    static readonly HashSet<string> Machines = new HashSet<string>
        { "conveyor_belt", "control_panel", "server_rack", "lab_bench_equip", "lab_bench_terminal",
          "barrel", "gantry_frame", "pc_tower", "monitor_keyboard", "office_chair", "floor_lamp", "whiteboard" };

    [MenuItem("Tools/Isometric World/Colorize L-Shape Scene")]
    public static void Colorize()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Directory.CreateDirectory(PaletteDir);

        var rends = scene.GetRootGameObjects()
                         .SelectMany(r => r.GetComponentsInChildren<Renderer>(true))
                         .ToList();

        var carColor = new Dictionary<Transform, int>();
        int nextCar = 0;
        int changed = 0;

        foreach (var r in rends)
        {
            int slots = Mathf.Max(r.sharedMaterials.Length, 1);
            string n = r.gameObject.name.ToLowerInvariant();
            Transform root = r.transform; while (root.parent != null) root = root.parent;
            string rn = root.name.ToLowerInvariant();
            string pn = r.transform.parent != null ? r.transform.parent.name.ToLowerInvariant() : "";
            var curNames = r.sharedMaterials.Where(m => m != null).Select(m => m.name).ToArray();

            Material pick;

            if (n.StartsWith("person") || n.StartsWith("scientist"))
                pick = People;
            else if (n.StartsWith("robot_") || n.Contains("_arm_material") || n.Contains("_chest_material")
                     || n.Contains("_leg_material") || n.Contains("_waist_material")
                     || curNames.Any(m => m.StartsWith("Arm_material") || m.StartsWith("Leg_material") || m.StartsWith("Waist_material")))
                pick = Robot;
            else if (n.StartsWith("meshbody") || n.StartsWith("body") || curNames.Any(m => m.StartsWith("Steel")))
                pick = Steel;
            else if (rn.StartsWith("spring") || n.StartsWith("canopy"))
                pick = Foliage;
            else if (n == "trunk") pick = Trunk;
            else if (n == "pot") pick = Pot;
            else if (rn.StartsWith("road")) pick = Asphalt;
            else if (rn.StartsWith("car") || rn.StartsWith("van") || pn.StartsWith("car_") || n.StartsWith("car_") || rn == "car" || rn == "van")
            {
                if (n.Contains("wheel")) pick = Tire;
                else
                {
                    if (!carColor.TryGetValue(root, out int ci)) { ci = nextCar++; carColor[root] = ci; }
                    pick = Car(ci);
                }
            }
            else if (n == "ground" || curNames.Contains("Ground")) pick = Grass;
            else if (n == "floorslab") pick = Floor;
            else if (n.StartsWith("seg")) pick = Wall;
            else if (Soft.Contains(n)) pick = Fabric;
            else if (Woody.Contains(n)) pick = Wood;
            else if (n == "unit_box_beveled") pick = Cardboard;
            else if (Machines.Contains(n)) pick = MetalLight;
            else pick = Floor;   // neutral fallback

            var arr = new Material[slots];
            for (int i = 0; i < slots; i++) arr[i] = pick;
            r.sharedMaterials = arr;
            changed++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"[SceneColorizer] Colorized {changed} renderers with muted palette.");
    }

    public static void ColorizeAndVerifyBatch()
    {
        Colorize();
        RenderStops();
    }

    static void RenderStops()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var dir = Object.FindFirstObjectByType<IsoCameraDirector>();
        var cam = dir != null ? dir.GetComponent<Camera>() : Object.FindFirstObjectByType<Camera>();
        string outDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Screenshots");
        Directory.CreateDirectory(outDir);

        // per-room fill views
        if (dir != null)
        {
            for (int i = 0; i < dir.positions.Length; i++)
            {
                cam.transform.SetPositionAndRotation(dir.positions[i], Quaternion.Euler(dir.eulerAngles[i]));
                cam.aspect = 1280f / 720f;
                cam.orthographicSize = Mathf.Max(dir.halfHeights[i], dir.halfWidths[i] / cam.aspect) * dir.padding;
                Shoot(cam, Path.Combine(outDir, $"iso_lshape_color{i + 1}.png"));
            }
        }

        // overview of the whole community
        var all = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        if (all.Length > 0)
        {
            Bounds b = all[0].bounds;
            foreach (var r in all) b.Encapsulate(r.bounds);
            var rot = Quaternion.Euler(35.264f, 45f, 0);
            Vector3 fwd = rot * Vector3.forward, right = rot * Vector3.right, up = rot * Vector3.up;
            float hr = 0, hu = 0;
            foreach (var c in Corners(b)) { Vector3 d = c - b.center; hr = Mathf.Max(hr, Mathf.Abs(Vector3.Dot(d, right))); hu = Mathf.Max(hu, Mathf.Abs(Vector3.Dot(d, up))); }
            cam.transform.SetPositionAndRotation(b.center - fwd * 300f, rot);
            cam.aspect = 1280f / 720f;
            cam.orthographicSize = Mathf.Max(hu, hr / cam.aspect) * 1.05f;
            cam.farClipPlane = 800f;
            Shoot(cam, Path.Combine(outDir, "iso_lshape_color_overview.png"));
        }
        Debug.Log("[SceneColorizer] Rendered colorized stops + overview.");
    }

    static void Shoot(Camera cam, string path)
    {
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
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);
    }

    static IEnumerable<Vector3> Corners(Bounds b)
    {
        Vector3 c = b.center, e = b.extents;
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    yield return c + new Vector3(sx * e.x, sy * e.y, sz * e.z);
    }

    static Material Mat(string name, string hex)
    {
        if (_cache.TryGetValue(name, out var m) && m != null) return m;
        string path = PaletteDir + "/" + name + ".mat";
        m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        ColorUtility.TryParseHtmlString(hex, out var col);
        m.SetColor("_BaseColor", col);
        m.SetFloat("_Smoothness", 0.08f);
        m.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(m);
        _cache[name] = m;
        return m;
    }

    // (Inspect kept for reference / re-running.)
    public static void InspectBatch()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var rends = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Renderer>(true)).ToList();
        var sb = new StringBuilder();
        foreach (var g in rends.GroupBy(x => x.gameObject.name).OrderByDescending(g => g.Count()).Take(220))
            sb.AppendLine($"  {g.Count(),4}  {g.Key}");
        Debug.Log(sb.ToString());
    }
}

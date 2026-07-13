using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adds subtle, looping, procedural motion to the L-shape scene at runtime.
/// Every target is animated relative to its authored pose (captured once), so
/// nothing is permanently moved - stop Play mode and everything is back in place.
///
/// The characters/robots are single static meshes (no skeleton), so per-limb
/// motion isn't possible; each behaviour animates the whole object in a way that
/// reads as the intended action:
///   VR player      - gentle look-around sway + bob (playing)
///   Scientists     - small thinking / note-taking sway
///   Storage robots - slow wander ("walking around"), facing travel direction
///   Hanging robot  - pendulum swing from its top (legs dangle/wiggle)
///   Conveyor belt  - tiny lengthwise back-and-forth
///   Robot arms     - smooth random yaw/pitch about their base
///   Cars           - very slow forward glide
/// </summary>
public class SceneAnimator : MonoBehaviour
{
    enum Mode { Sway, Think, Walk, Swing, Oscillate, ArmWiggle, CarDrive }

    class Anim
    {
        public Transform t;
        public Vector3 basePos;
        public Quaternion baseRot;
        public Mode mode;
        public float seed;
        public Vector3 axis;   // local right/forward captured for oscillate/drive
        public float pivotH;   // for swing
    }

    readonly List<Anim> _items = new List<Anim>();
    bool _collected;

    void OnEnable()
    {
        if (!_collected) CollectTargets();
    }

    void Update() => Step(Time.time);

    // ------------------------------------------------------------ collection

    public void CollectTargets()
    {
        _items.Clear();
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var robotRoots = new HashSet<Transform>();

        float seed = 0f;
        foreach (var t in all)
        {
            string n = t.name.ToLowerInvariant();
            bool isRoot = t.parent == null;

            if (n.StartsWith("person_vr")) Add(t, Mode.Sway, ref seed);
            else if (n.StartsWith("scientist")) Add(t, Mode.Think, ref seed);
            else if (n.StartsWith("robot_humanoid_carrying") || n.StartsWith("robot_humanoid_standing"))
                Add(t, Mode.Walk, ref seed);
            else if (n.StartsWith("robot_humanoid_hanging")) Add(t, Mode.Swing, ref seed);
            else if (n == "conveyor_belt") Add(t, Mode.Oscillate, ref seed);
            else if (n == "robot_arm") Add(t, Mode.ArmWiggle, ref seed);
            else if (isRoot && (n.StartsWith("car") || n.StartsWith("van"))) Add(t, Mode.CarDrive, ref seed);
        }

        // detailed asset humanoid robot (parts share *_chest_material / Chest_material):
        // animate its ROOT only. Swing if it hangs (feet off floor), else walk.
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string rn = r.gameObject.name.ToLowerInvariant();
            bool isRobotPart = rn.Contains("_chest_material") || rn.Contains("_waist_material");
            if (!isRobotPart) continue;
            Transform root = r.transform; while (root.parent != null) root = root.parent;
            if (!robotRoots.Add(root)) continue;

            var b = Encapsulate(root);
            Mode m = (b.min.y > 0.25f) ? Mode.Swing : Mode.Walk;
            Add(root, m, ref seed, b);
        }

        _collected = true;
    }

    void Add(Transform t, Mode mode, ref float seed, Bounds? known = null)
    {
        var a = new Anim
        {
            t = t,
            basePos = t.position,
            baseRot = t.rotation,
            mode = mode,
            seed = seed,
            axis = mode == Mode.Oscillate ? t.right : t.forward,
        };
        if (mode == Mode.Swing)
        {
            var b = known ?? Encapsulate(t);
            a.pivotH = Mathf.Max(b.max.y - t.position.y, 0.6f);   // hang point above origin
        }
        _items.Add(a);
        seed += 1.37f;
    }

    static Bounds Encapsulate(Transform root)
    {
        var rs = root.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(root.position, Vector3.one);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    // ------------------------------------------------------------- animation

    public void Step(float t)
    {
        foreach (var a in _items)
        {
            if (a.t == null) continue;
            float s = a.seed;
            switch (a.mode)
            {
                case Mode.Sway:
                {
                    float yaw = Mathf.Sin(t * 1.1f + s) * 7f;
                    float lean = Mathf.Sin(t * 0.9f + s * 1.3f) * 3f;
                    float bob = Mathf.Sin(t * 2.2f + s) * 0.015f;
                    a.t.rotation = a.baseRot * Quaternion.Euler(lean, yaw, 0f);
                    a.t.position = a.basePos + Vector3.up * bob;
                    break;
                }
                case Mode.Think:
                {
                    float yaw = Mathf.Sin(t * 0.5f + s) * 4f;
                    float lean = Mathf.Sin(t * 0.72f + s) * 2.2f;
                    a.t.rotation = a.baseRot * Quaternion.Euler(lean, yaw, 0f);
                    break;
                }
                case Mode.Walk:
                {
                    const float rx = 0.9f, rz = 0.9f, w = 0.4f;
                    float ph = t * w + s;
                    Vector3 off = new Vector3(Mathf.Sin(ph) * rx, 0f, Mathf.Cos(ph) * rz);
                    Vector3 vel = new Vector3(Mathf.Cos(ph) * rx, 0f, -Mathf.Sin(ph) * rz);
                    a.t.position = a.basePos + off;
                    if (vel.sqrMagnitude > 1e-4f) a.t.rotation = Quaternion.LookRotation(vel.normalized, Vector3.up);
                    break;
                }
                case Mode.Swing:
                {
                    Vector3 pivot = a.basePos + Vector3.up * a.pivotH;
                    Quaternion sw = Quaternion.Euler(Mathf.Sin(t * 1.6f + s) * 7f, 0f, Mathf.Sin(t * 1.15f + s) * 3f);
                    a.t.position = pivot + sw * (a.basePos - pivot);
                    a.t.rotation = sw * a.baseRot;
                    break;
                }
                case Mode.Oscillate:
                {
                    a.t.position = a.basePos + a.axis * (Mathf.Sin(t * 2f + s) * 0.05f);
                    break;
                }
                case Mode.ArmWiggle:
                {
                    float yaw = (Mathf.PerlinNoise(t * 0.25f, s) - 0.5f) * 50f;
                    float pitch = (Mathf.PerlinNoise(s, t * 0.3f) - 0.5f) * 20f;
                    a.t.rotation = a.baseRot * Quaternion.Euler(pitch, yaw, 0f);
                    break;
                }
                case Mode.CarDrive:
                {
                    a.t.position = a.basePos + a.axis * (Mathf.Sin(t * 0.25f + s) * 1.6f);
                    break;
                }
            }
        }
    }
}

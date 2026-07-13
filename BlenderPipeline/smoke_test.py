"""Pipeline smoke test: validates headless render + FBX export on this machine."""
import os
import sys

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
import lib_common as L
import bpy

print("BLENDER VERSION:", bpy.app.version_string)
print("HAS export_scene.fbx:", hasattr(bpy.ops.export_scene, "fbx"))
print("HAS wm.fbx_export:", hasattr(bpy.ops.wm, "fbx_export"))


def build_test():
    parts = [
        L.rounded_box("body", (0.4, 0.3, 0.6), (0, 0, 0.5), bevel=0.06),
        L.sphere("head", 0.15, (0, 0, 0.95)),
        L.capsule("arm", (0.25, 0, 0.7), (0.35, -0.2, 0.4), 0.05),
        L.slab("lean", (0.3, 0.2, 0.0), (0.5, 0.2, 0.5), 0.1, 0.1, bevel=0.03),
        L.rod("wire", (-0.3, 0, 1.2), (-0.3, 0, 0.4), 0.008),
    ]
    return L.join(parts, "smoke")


L.build_and_ship([("_smoke_test", build_test)], "_SmokeTest")
print("SMOKE TEST OK")

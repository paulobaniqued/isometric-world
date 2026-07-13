"""Shared helpers for the isometric-world Blender asset pipeline.

Run any gen_*.py with:
  blender --background --python gen_xxx.py

Conventions:
  - 1 Blender unit = 1 metre.
  - Characters/robots face -Y in Blender (becomes +Z forward in Unity).
  - Every asset is exported as ONE joined mesh, origin at floor centre (z=0).
  - FBX output goes to Assets/Models/<Category>/, previews to BlenderPipeline/previews/.
"""

import math
import os
import sys

import bpy
from mathutils import Vector

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
MODELS_DIR = os.path.join(REPO_ROOT, "Assets", "Models")
PREVIEW_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "previews")

CLAY = (0.82, 0.84, 0.90, 1.0)


# ---------------------------------------------------------------- scene mgmt

def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    scene.unit_settings.scale_length = 1.0


def _clay_material():
    mat = bpy.data.materials.get("Clay")
    if mat is None:
        mat = bpy.data.materials.new("Clay")
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf is not None:
            bsdf.inputs["Base Color"].default_value = CLAY
            bsdf.inputs["Roughness"].default_value = 0.95
    return mat


def _finish_obj(obj):
    """Common post-setup: clay material, return object."""
    obj.data.materials.clear()
    obj.data.materials.append(_clay_material())
    return obj


# ---------------------------------------------------------------- primitives

def box(name, size, loc, rot=None):
    """Sharp box. size=(x,y,z) full dims, loc = centre."""
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc)
    obj = bpy.context.object
    obj.name = name
    obj.scale = (size[0], size[1], size[2])
    if rot:
        obj.rotation_euler = [math.radians(a) for a in rot]
    _apply_transform(obj, scale=True)
    return _finish_obj(obj)


def rounded_box(name, size, loc, rot=None, bevel=0.02, segments=3):
    """Box with uniform bevel (scale applied before bevel so it stays round)."""
    obj = box(name, size, loc, rot)
    mod = obj.modifiers.new("Bevel", 'BEVEL')
    mod.width = min(bevel, min(size) * 0.49)
    mod.segments = segments
    mod.limit_method = 'ANGLE'
    mod.angle_limit = math.radians(40)
    mod.use_clamp_overlap = True
    _shade_smooth(obj)
    return obj


def cyl(name, radius, depth, loc, rot=None, vertices=24):
    bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=depth,
                                        vertices=vertices, location=loc)
    obj = bpy.context.object
    obj.name = name
    if rot:
        obj.rotation_euler = [math.radians(a) for a in rot]
    _shade_smooth(obj)
    return _finish_obj(obj)


def sphere(name, radius, loc, segments=24, rings=16, scale=None):
    bpy.ops.mesh.primitive_uv_sphere_add(radius=radius, location=loc,
                                         segments=segments, ring_count=rings)
    obj = bpy.context.object
    obj.name = name
    if scale:
        obj.scale = scale
        _apply_transform(obj, scale=True)
    _shade_smooth(obj)
    return _finish_obj(obj)


def capsule(name, p1, p2, radius, vertices=20):
    """Capsule between two points: cylinder + two sphere caps, joined."""
    p1, p2 = Vector(p1), Vector(p2)
    d = p2 - p1
    length = max(d.length, 1e-4)
    bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=length,
                                        vertices=vertices,
                                        location=(p1 + p2) / 2)
    body = bpy.context.object
    body.name = name
    body.rotation_mode = 'QUATERNION'
    body.rotation_quaternion = d.to_track_quat('Z', 'Y')
    _shade_smooth(body)
    _finish_obj(body)
    caps = []
    for p in (p1, p2):
        caps.append(sphere(name + "_cap", radius, p, segments=16, rings=12))
    return join([body] + caps, name)


def slab(name, p1, p2, width, depth, bevel=0.05, extend=0.0, segments=3):
    """Rounded box whose local Z axis runs p1 -> p2 (torsos, angled links)."""
    p1, p2 = Vector(p1), Vector(p2)
    d = p2 - p1
    length = max(d.length + extend * 2, 1e-3)
    obj = rounded_box(name, (width, depth, length), (p1 + p2) / 2,
                      bevel=bevel, segments=segments)
    obj.rotation_mode = 'QUATERNION'
    obj.rotation_quaternion = d.to_track_quat('Z', 'Y')
    return obj


def rod(name, p1, p2, radius, vertices=12):
    """Plain cylinder between two points (no caps) - for wires, thin frames."""
    p1, p2 = Vector(p1), Vector(p2)
    d = p2 - p1
    length = max(d.length, 1e-4)
    bpy.ops.mesh.primitive_cylinder_add(radius=radius, depth=length,
                                        vertices=vertices,
                                        location=(p1 + p2) / 2)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = 'QUATERNION'
    obj.rotation_quaternion = d.to_track_quat('Z', 'Y')
    _shade_smooth(obj)
    return _finish_obj(obj)


# ------------------------------------------------------------------- helpers

def _apply_transform(obj, loc=False, rot=False, scale=False):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=loc, rotation=rot, scale=scale)


def _shade_smooth(obj):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    try:
        bpy.ops.object.shade_auto_smooth(angle=math.radians(35))
    except Exception:
        bpy.ops.object.shade_smooth()


def join(objs, name):
    # join() merges raw mesh data, silently dropping modifiers on non-active
    # objects - so apply modifiers (bevels) on every part first.
    fixed = []
    for o in objs:
        if o.modifiers:
            bpy.ops.object.select_all(action='DESELECT')
            o.select_set(True)
            bpy.context.view_layer.objects.active = o
            bpy.ops.object.convert(target='MESH')
            fixed.append(bpy.context.object)
        else:
            fixed.append(o)
    objs = fixed
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    obj.data.name = name
    return obj


def mirror_x(points):
    """Mirror a joint dict/point across X (for left limbs from right limbs)."""
    if isinstance(points, dict):
        return {k: (-v[0], v[1], v[2]) for k, v in points.items()}
    return (-points[0], points[1], points[2])


def smooth_union(obj, voxel=0.02, smooth_iter=1, smooth_factor=0.5):
    """Fuse a mesh of overlapping primitives into ONE smooth blended surface.

    Voxel-remesh takes the union volume, so limbs keep their shape but joints
    (shoulders, hips, elbows, neck) fillet together into a continuous clay skin -
    the sculpted-mannequin look of the reference figures. Applied in-place;
    returns the resulting single mesh object with the clay material.
    """
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    rem = obj.modifiers.new("Remesh", 'REMESH')
    rem.mode = 'VOXEL'
    rem.voxel_size = voxel
    rem.use_smooth_shade = True
    bpy.ops.object.convert(target='MESH')   # bake the remesh
    obj = bpy.context.object
    if smooth_iter:
        sm = obj.modifiers.new("Smooth", 'SMOOTH')
        sm.factor = smooth_factor
        sm.iterations = smooth_iter
        bpy.ops.object.convert(target='MESH')
        obj = bpy.context.object
    _shade_smooth(obj)
    _finish_obj(obj)
    return obj


def finalize(obj, name):
    """Apply modifiers + transforms, rename, ready for export."""
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target='MESH')  # applies modifiers
    obj = bpy.context.object
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    obj.name = name
    obj.data.name = name
    return obj


# ------------------------------------------------------------ export/preview

def export_fbx(obj, category, name):
    out_dir = os.path.join(MODELS_DIR, category)
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, name + ".fbx")
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        use_mesh_modifiers=True,
        bake_space_transform=True,
        object_types={'MESH'},
        add_leaf_bones=False,
        path_mode='AUTO',
    )
    print("EXPORTED:", path)
    return path


def preview(name, objs=None):
    """Render an isometric preview PNG of the given (or all) mesh objects."""
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    scene = bpy.context.scene

    meshes = objs or [o for o in scene.objects if o.type == 'MESH']
    lo = Vector((1e9, 1e9, 1e9))
    hi = Vector((-1e9, -1e9, -1e9))
    for o in meshes:
        for corner in o.bound_box:
            wc = o.matrix_world @ Vector(corner)
            lo = Vector(map(min, lo, wc))
            hi = Vector(map(max, hi, wc))
    center = (lo + hi) / 2
    extent = max((hi - lo).length, 0.5)

    cam_data = bpy.data.cameras.new("PreviewCam")
    cam_data.type = 'ORTHO'
    cam_data.ortho_scale = extent * 1.15
    cam_data.clip_end = 200
    cam = bpy.data.objects.new("PreviewCam", cam_data)
    scene.collection.objects.link(cam)
    direction = Vector((0.577, -0.577, 0.577))
    cam.location = center + direction * (extent * 4 + 5)
    cam.rotation_euler = (math.radians(54.736), 0, math.radians(45))
    scene.camera = cam

    # ground plane for shadow context
    bpy.ops.mesh.primitive_plane_add(size=extent * 12, location=(center.x, center.y, -0.001))
    plane = bpy.context.object
    plane.name = "_PreviewGround"
    _finish_obj(plane)

    sun_data = bpy.data.lights.new("PreviewSun", 'SUN')
    sun_data.energy = 3.0
    sun = bpy.data.objects.new("PreviewSun", sun_data)
    sun.rotation_euler = (math.radians(45), math.radians(-15), math.radians(30))
    scene.collection.objects.link(sun)

    if scene.world is None:
        scene.world = bpy.data.worlds.new("PreviewWorld")
    scene.world.use_nodes = True
    bg = scene.world.node_tree.nodes.get("Background")
    if bg is not None:
        bg.inputs[0].default_value = (0.9, 0.92, 0.96, 1.0)
        bg.inputs[1].default_value = 1.0

    scene.render.resolution_x = 900
    scene.render.resolution_y = 900
    scene.render.filepath = os.path.join(PREVIEW_DIR, name + ".png")
    scene.render.image_settings.file_format = 'PNG'

    try:
        scene.render.engine = 'BLENDER_WORKBENCH'
        sd = scene.display
        sd.shading.light = 'STUDIO'
        sd.shading.color_type = 'SINGLE'
        sd.shading.single_color = (0.85, 0.87, 0.92)
        sd.shading.show_cavity = True
        sd.shading.cavity_type = 'BOTH'
        sd.shading.show_shadows = True
        sd.render_aa = '8'
        bpy.ops.render.render(write_still=True)
    except Exception as exc:
        print("Workbench render failed (%s), falling back to Cycles CPU" % exc)
        scene.render.engine = 'CYCLES'
        scene.cycles.samples = 32
        scene.cycles.device = 'CPU'
        bpy.ops.render.render(write_still=True)

    print("PREVIEW:", scene.render.filepath)

    # clean up preview-only objects so they never leak into exports
    for o in (cam, sun, plane):
        bpy.data.objects.remove(o, do_unlink=True)


def build_and_ship(builders, category):
    """builders: list of (name, fn) where fn() returns the final joined object."""
    for name, fn in builders:
        reset_scene()
        obj = fn()
        obj = finalize(obj, name)
        preview(name, [obj])
        export_fbx(obj, category, name)
    print("DONE:", category, [n for n, _ in builders])

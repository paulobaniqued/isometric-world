"""Generates robot assets -> Assets/Models/Robots/*.fbx

- robot_humanoid_hanging: limp pose suspended ~0.35 m above floor; includes the
  wire bundle whose tops end at z=2.37 (the gantry_frame hoist block bottom).
  Place at the same position as gantry_frame in Unity and they line up.
- robot_humanoid_standing: same design, upright, one arm reaching to a shelf.
- robot_arm: industrial arm reaching forward (-Y); place base ~0.6 m from the
  conveyor centre line so the gripper hovers over the belt.
"""
import os
import sys

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
import lib_common as L
from mathutils import Vector

WIRE_TOP_Z = 2.37  # bottom of gantry hoist block


def robot_pose(dz=0.0):
    """Joint set for the mechanical humanoid; dz lifts the whole body."""
    p = {
        'pelvis': (0, 0, 0.99),
        'neck':   (0, 0, 1.44),
        'head':   (0, 0, 1.60),
        'sh_r': (0.225, 0.0, 1.38),  'el_r': (0.26, 0.03, 1.10),  'wr_r': (0.27, 0.02, 0.86),
        'sh_l': (-0.225, 0.0, 1.38), 'el_l': (-0.26, 0.03, 1.10), 'wr_l': (-0.27, 0.02, 0.86),
        'hip_r': (0.10, 0, 0.94),    'kn_r': (0.105, -0.01, 0.50), 'an_r': (0.105, 0.0, 0.09),
        'hip_l': (-0.10, 0, 0.94),   'kn_l': (-0.105, -0.01, 0.50), 'an_l': (-0.105, 0.0, 0.09),
        'head_shift': (0, 0, 0),
        'foot_rot': 0,  # X rotation of feet (deg); ~ -55 for hanging toes-down
    }
    if dz:
        for k, v in p.items():
            if isinstance(v, tuple) and len(v) == 3 and k not in ('head_shift',):
                p[k] = (v[0], v[1], v[2] + dz)
    return p


def build_robot(name, p, wires=False, extras=None):
    V = lambda k: Vector(p[k])
    parts = []

    # torso stack: chest / abdomen / pelvis with visible gaps between segments
    neck, pelvis = V('neck'), V('pelvis')
    up = (neck - pelvis).normalized()
    chest_c = neck - up * 0.17
    parts.append(L.rounded_box("chest", (0.37, 0.24, 0.34), chest_c, bevel=0.06))
    parts.append(L.rounded_box("chest_panel", (0.16, 0.255, 0.11),
                               chest_c + Vector((0, 0, 0.02)), bevel=0.02))
    parts.append(L.rounded_box("abdomen", (0.24, 0.18, 0.15),
                               pelvis + up * 0.10, bevel=0.05))
    parts.append(L.rounded_box("pelvis", (0.31, 0.21, 0.17),
                               pelvis - up * 0.045, bevel=0.055))

    head = V('head') + Vector(p['head_shift'])
    parts.append(L.rod("neck", V('neck'), head, 0.045))
    parts.append(L.rounded_box("head", (0.18, 0.20, 0.23), head + Vector((0, 0, 0.015)),
                               bevel=0.055))
    parts.append(L.rounded_box("visor", (0.14, 0.03, 0.055),
                               head + Vector((0, -0.10, 0.03)), bevel=0.012))

    for s in ('r', 'l'):
        sh, el, wr = V('sh_' + s), V('el_' + s), V('wr_' + s)
        parts.append(L.sphere("shoulder_" + s, 0.080, sh))
        parts.append(L.rod("uparm_" + s, sh, el, 0.050))
        parts.append(L.sphere("elbow_" + s, 0.062, el))
        parts.append(L.rod("forearm_" + s, el, wr, 0.044))
        hand_end = wr + (wr - el).normalized() * 0.13
        parts.append(L.slab("hand_" + s, wr, hand_end, 0.075, 0.04, bevel=0.015))

        hip, kn, an = V('hip_' + s), V('kn_' + s), V('an_' + s)
        parts.append(L.sphere("hipj_" + s, 0.075, hip))
        parts.append(L.rod("thigh_" + s, hip, kn, 0.058))
        parts.append(L.sphere("knee_" + s, 0.068, kn))
        parts.append(L.rod("shin_" + s, kn, an, 0.050))
        parts.append(L.rounded_box("foot_" + s, (0.11, 0.26, 0.10),
                                   (an.x, an.y - 0.05, an.z - 0.045),
                                   rot=(p['foot_rot'], 0, 0), bevel=0.03))

    if wires:
        tops = [(0.10, -0.05, WIRE_TOP_Z), (-0.10, -0.05, WIRE_TOP_Z),
                (0.10, 0.05, WIRE_TOP_Z), (-0.10, 0.05, WIRE_TOP_Z)]
        anchors = [V('sh_r') + Vector((0, -0.03, 0.05)),
                   V('sh_l') + Vector((0, -0.03, 0.05)),
                   V('sh_r') + Vector((-0.02, 0.05, 0.04)),
                   V('sh_l') + Vector((0.02, 0.05, 0.04))]
        for i, (t, a) in enumerate(zip(tops, anchors)):
            parts.append(L.rod("wire_%d" % i, t, a, 0.008, vertices=8))
        # slack data cable drooping from the head to mid-air
        parts.append(L.rod("cable", V('head') + Vector((0, 0.10, 0.06)),
                           (0.05, 0.28, WIRE_TOP_Z), 0.012, vertices=8))

    if extras:
        parts.extend(extras(p))

    return L.join(parts, name)


# -------------------------------------------------------------------- assets

def robot_hanging():
    p = robot_pose(dz=0.35)
    # limp: arms slightly out and slack, knees a touch bent, head drooped
    p['el_r'] = (0.30, 0.05, 1.42);  p['wr_r'] = (0.33, 0.09, 1.18)
    p['el_l'] = (-0.30, 0.05, 1.42); p['wr_l'] = (-0.33, 0.09, 1.18)
    p['kn_r'] = (0.11, -0.06, 0.86); p['an_r'] = (0.115, -0.02, 0.47)
    p['kn_l'] = (-0.11, -0.06, 0.86); p['an_l'] = (-0.115, -0.02, 0.47)
    p['head_shift'] = (0, -0.035, -0.03)
    p['foot_rot'] = -55
    return build_robot("robot_humanoid_hanging", p, wires=True)


def robot_standing():
    p = robot_pose()
    # right arm reaching up/forward toward a shelf
    p['el_r'] = (0.27, -0.09, 1.28)
    p['wr_r'] = (0.26, -0.32, 1.46)
    return build_robot("robot_humanoid_standing", p)


def robot_carrying():
    """Upright, mid-stride, both arms bent forward holding a crate at the chest."""
    p = robot_pose()
    p['sh_r'] = (0.225, 0.06, 1.31);  p['sh_l'] = (-0.225, 0.06, 1.31)
    p['el_r'] = (0.255, -0.16, 1.12); p['el_l'] = (-0.255, -0.16, 1.12)
    p['wr_r'] = (0.185, -0.42, 1.15); p['wr_l'] = (-0.185, -0.42, 1.15)
    # slight walking stride: right leg forward (-Y), left leg trailing (+Y)
    p['hip_r'] = (0.10, -0.03, 0.94); p['kn_r'] = (0.11, -0.17, 0.51); p['an_r'] = (0.115, -0.26, 0.09)
    p['hip_l'] = (-0.10, 0.03, 0.94); p['kn_l'] = (-0.11, 0.11, 0.52); p['an_l'] = (-0.115, 0.22, 0.09)
    p['head_shift'] = (0, -0.02, -0.02)

    def extras(p):
        box = L.rounded_box("carry_box", (0.52, 0.44, 0.42), (0, -0.42, 1.21), bevel=0.03)
        lip = L.rounded_box("carry_lip", (0.46, 0.38, 0.05), (0, -0.42, 1.44), bevel=0.02)
        return [box, lip]

    return build_robot("robot_humanoid_carrying", p, extras=extras)


def robot_arm():
    parts = [
        L.cyl("pedestal", 0.24, 0.14, (0, 0, 0.07)),
        L.cyl("turret", 0.175, 0.34, (0, 0, 0.38)),
        L.sphere("shoulder", 0.135, (0, 0, 0.58)),
        L.slab("upper_link", (0, 0, 0.58), (0, -0.20, 1.16), 0.16, 0.16, bevel=0.045),
        L.sphere("elbow", 0.115, (0, -0.20, 1.16)),
        L.slab("fore_link", (0, -0.20, 1.16), (0, -0.55, 1.02), 0.13, 0.13, bevel=0.04),
        L.sphere("wrist", 0.085, (0, -0.55, 1.02)),
        L.cyl("gripper_base", 0.055, 0.10, (0, -0.55, 0.94)),
        L.rounded_box("finger_a", (0.025, 0.05, 0.15), (0.045, -0.55, 0.85), bevel=0.008),
        L.rounded_box("finger_b", (0.025, 0.05, 0.15), (-0.045, -0.55, 0.85), bevel=0.008),
        L.rod("cable", (0, 0.15, 0.45), (0, -0.14, 1.12), 0.02, vertices=10),
    ]
    return L.join(parts, "robot_arm")


BUILDERS = [
    ("robot_humanoid_hanging", robot_hanging),
    ("robot_humanoid_standing", robot_standing),
    ("robot_humanoid_carrying", robot_carrying),
    ("robot_arm", robot_arm),
]

L.build_and_ship(BUILDERS, "Robots")

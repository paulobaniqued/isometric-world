"""Generates the human character poses -> Assets/Models/Characters/*.fbx

Mannequin style matches the clay reference renders: smooth featureless figures,
~1.72 m tall, facing -Y in Blender (imports facing +Z in Unity).
"""
import os
import sys

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
import lib_common as L
from mathutils import Vector


# ------------------------------------------------------------------ skeleton

def base_pose():
    """Neutral standing pose, arms hanging. All joints in metres, facing -Y."""
    return {
        'pelvis': (0, 0, 0.98),
        'neck':   (0, 0, 1.40),
        'head':   (0, 0, 1.56),
        'sh_r': (0.19, 0.0, 1.35),  'el_r': (0.22, 0.02, 1.10),  'wr_r': (0.235, 0.0, 0.88),
        'sh_l': (-0.19, 0.0, 1.35), 'el_l': (-0.22, 0.02, 1.10), 'wr_l': (-0.235, 0.0, 0.88),
        'hip_r': (0.09, 0, 0.95),   'kn_r': (0.10, -0.01, 0.52), 'an_r': (0.10, 0.0, 0.075),
        'hip_l': (-0.09, 0, 0.95),  'kn_l': (-0.10, -0.01, 0.52), 'an_l': (-0.10, 0.0, 0.075),
        'head_shift': (0, 0, 0),
    }


def build_human(name, p, coat=False, extras=None):
    """Smooth anatomical mannequin: overlapping primitives fused via voxel remesh
    into one continuous clay surface (see L.smooth_union). Hard accessories
    (VR headset, tablet) are added AFTER the remesh so they stay crisp."""
    V = lambda k: Vector(p[k])
    parts = []

    pelvis, neck = V('pelvis'), V('neck')
    up = (neck - pelvis)
    up = up.normalized() if up.length > 1e-5 else Vector((0, 0, 1))

    # Tapered torso: a narrower waist slab + a wider chest block that overlap so
    # the union reads as a chest-to-waist taper rather than a straight tube.
    waist_w, waist_d = (0.33, 0.215) if coat else (0.295, 0.19)
    chest_w, chest_d = (0.41, 0.24) if coat else (0.375, 0.215)
    parts.append(L.slab("waist", pelvis + up * 0.02, pelvis + up * 0.26,
                        waist_w, waist_d, bevel=0.07, extend=0.02))
    # chest reaches up to ~1.44 so the neck has a thick base to fuse onto
    parts.append(L.rounded_box("chest", (chest_w, chest_d, 0.36), neck - up * 0.11, bevel=0.10))
    parts.append(L.rounded_box("pelvis", (0.31, 0.205, 0.21), pelvis - up * 0.05, bevel=0.075))
    if coat:
        skirt_bot = Vector((p['pelvis'][0], p['pelvis'][1] + 0.02, 0.60))
        parts.append(L.slab("coat_skirt", pelvis + up * 0.02, skirt_bot, 0.38, 0.25,
                            bevel=0.05, extend=0.02))

    # Deltoids blend the arms smoothly into the torso.
    parts.append(L.sphere("delt_r", 0.078, V('sh_r')))
    parts.append(L.sphere("delt_l", 0.078, V('sh_l')))

    # Thick, short neck with generous overlap into both chest and head so the
    # voxel remesh keeps the head firmly attached.
    head = V('head') + Vector(p['head_shift'])
    parts.append(L.capsule("neck", V('neck') - Vector((0, 0, 0.09)),
                           head - Vector((0, 0, 0.04)), 0.072))
    parts.append(L.sphere("head", 0.108, head, scale=(0.97, 1.03, 1.18)))

    for s in ('r', 'l'):
        sh, el, wr = V('sh_' + s), V('el_' + s), V('wr_' + s)
        parts.append(L.capsule("uparm_" + s, sh, el, 0.051))
        parts.append(L.capsule("forearm_" + s, el, wr, 0.043))
        d = (wr - el)
        d = d.normalized() if d.length > 1e-5 else Vector((0, 0, -1))
        parts.append(L.slab("hand_" + s, wr + d * 0.01, wr + d * 0.12, 0.086, 0.044, bevel=0.02))

        hip, kn, an = V('hip_' + s), V('kn_' + s), V('an_' + s)
        parts.append(L.sphere("hipj_" + s, 0.086, hip))
        parts.append(L.capsule("thigh_" + s, hip, kn, 0.079))
        parts.append(L.capsule("shin_" + s, kn, an, 0.055))
        parts.append(L.rounded_box("foot_" + s, (0.10, 0.26, 0.085),
                                   (an.x, an.y - 0.06, 0.047), bevel=0.03))

    # smooth_iter=0: the voxel remesh + smooth shading already gives the clay
    # surface; an extra Smooth modifier pinches thin features (neck) and can
    # sever the head, so we skip it and use a slightly finer voxel instead.
    body = L.smooth_union(L.join(parts, name + "_body"), voxel=0.018, smooth_iter=0)

    if extras:
        return L.join([body] + list(extras(p)), name)
    body.name = name
    body.data.name = name
    return body


# --------------------------------------------------------------------- poses

def person_vr():
    p = base_pose()
    p['kn_r'], p['an_r'] = (0.125, 0.0, 0.52), (0.13, 0.02, 0.075)
    p['kn_l'], p['an_l'] = (-0.13, -0.05, 0.52), (-0.14, -0.08, 0.075)
    p['el_r'], p['wr_r'] = (0.26, -0.10, 1.12), (0.20, -0.30, 1.32)
    p['el_l'], p['wr_l'] = (-0.26, -0.12, 1.10), (-0.17, -0.28, 1.24)
    p['head_shift'] = (0, -0.01, 0.015)

    def extras(p):
        head = Vector(p['head']) + Vector(p['head_shift'])
        headset = L.rounded_box("headset", (0.21, 0.115, 0.105),
                                head + Vector((0, -0.095, -0.005)), bevel=0.032)
        strap = L.rounded_box("strap", (0.225, 0.16, 0.04),
                              head + Vector((0, -0.01, 0.0)), bevel=0.015)
        ctl_r = L.rounded_box("ctl_r", (0.06, 0.10, 0.06),
                              Vector(p['wr_r']) + Vector((0, -0.06, 0.03)), bevel=0.02)
        ctl_l = L.rounded_box("ctl_l", (0.06, 0.10, 0.06),
                              Vector(p['wr_l']) + Vector((0, -0.06, 0.03)), bevel=0.02)
        return [headset, strap, ctl_r, ctl_l]

    return build_human("person_vr", p, extras=extras)


def person_sitting():
    p = base_pose()
    p['pelvis'] = (0, 0, 0.50)
    p['neck'] = (0, -0.03, 0.96)
    p['head'] = (0, -0.05, 1.12)
    p['sh_r'], p['sh_l'] = (0.19, -0.03, 0.91), (-0.19, -0.03, 0.91)
    p['el_r'], p['wr_r'] = (0.21, -0.20, 0.70), (0.15, -0.40, 0.74)
    p['el_l'], p['wr_l'] = (-0.21, -0.20, 0.70), (-0.15, -0.40, 0.74)
    p['hip_r'], p['kn_r'], p['an_r'] = (0.09, -0.02, 0.48), (0.10, -0.46, 0.49), (0.10, -0.50, 0.075)
    p['hip_l'], p['kn_l'], p['an_l'] = (-0.09, -0.02, 0.48), (-0.10, -0.46, 0.49), (-0.10, -0.50, 0.075)
    return build_human("person_sitting", p)


def person_standing_a():
    p = base_pose()
    p['el_r'], p['wr_r'] = (0.23, -0.10, 1.22), (0.21, -0.33, 1.35)
    p['kn_l'], p['an_l'] = (-0.105, -0.03, 0.52), (-0.11, -0.05, 0.075)
    return build_human("person_standing_a", p)


def person_standing_b():
    p = base_pose()
    p['el_l'], p['wr_l'] = (-0.27, 0.0, 1.08), (-0.14, -0.02, 1.00)
    p['kn_r'], p['an_r'] = (0.115, 0.01, 0.52), (0.12, 0.03, 0.075)
    return build_human("person_standing_b", p)


def scientist_pointing():
    p = base_pose()
    p['el_r'], p['wr_r'] = (0.24, -0.08, 1.24), (0.22, -0.30, 1.48)
    p['head_shift'] = (0, -0.01, 0.02)
    return build_human("scientist_pointing", p, coat=True)


def scientist_armscrossed():
    p = base_pose()
    p['el_r'], p['wr_r'] = (0.25, -0.04, 1.10), (-0.05, -0.16, 1.14)
    p['el_l'], p['wr_l'] = (-0.25, -0.04, 1.10), (0.05, -0.16, 1.10)
    return build_human("scientist_armscrossed", p, coat=True)


def scientist_tablet():
    p = base_pose()
    p['el_r'], p['wr_r'] = (0.23, -0.05, 1.06), (0.10, -0.22, 1.04)
    p['el_l'], p['wr_l'] = (-0.23, -0.05, 1.06), (-0.10, -0.22, 1.04)
    p['head_shift'] = (0, -0.025, -0.012)

    def extras(p):
        return [L.rounded_box("tablet", (0.26, 0.18, 0.017),
                              (0, -0.27, 1.07), rot=(-32, 0, 0), bevel=0.006)]

    return build_human("scientist_tablet", p, coat=True, extras=extras)


BUILDERS = [
    ("person_vr", person_vr),
    ("person_sitting", person_sitting),
    ("person_standing_a", person_standing_a),
    ("person_standing_b", person_standing_b),
    ("scientist_pointing", scientist_pointing),
    ("scientist_armscrossed", scientist_armscrossed),
    ("scientist_tablet", scientist_tablet),
]

L.build_and_ship(BUILDERS, "Characters")

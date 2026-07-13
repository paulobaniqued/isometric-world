"""Generates furniture/prop assets -> Assets/Models/Furniture/*.fbx

All assets: origin at floor centre (z=0), "front" faces -Y where relevant.
monitor_keyboard has its origin at the *desktop surface* - place at table height.
"""
import os
import sys

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
import lib_common as L
from mathutils import Vector


# ------------------------------------------------------------ shared clusters

def add_monitor(parts, at, scale=1.0, tag=""):
    """Desk monitor + stand; 'at' is the point on the desk surface."""
    x, y, z = at
    s = scale
    parts.append(L.rounded_box("mon_base" + tag, (0.24 * s, 0.17 * s, 0.02 * s),
                               (x, y, z + 0.01 * s), bevel=0.008))
    parts.append(L.box("mon_stalk" + tag, (0.05 * s, 0.04 * s, 0.15 * s),
                       (x, y + 0.03 * s, z + 0.095 * s)))
    parts.append(L.rounded_box("mon_screen" + tag, (0.58 * s, 0.035 * s, 0.34 * s),
                               (x, y + 0.02 * s, z + 0.33 * s),
                               rot=(8, 0, 0), bevel=0.012))


def add_keyboard(parts, at, tag=""):
    x, y, z = at
    parts.append(L.rounded_box("keyboard" + tag, (0.43, 0.15, 0.025),
                               (x, y, z + 0.0125), bevel=0.008))


# ------------------------------------------------------------------- assets

def bed():
    parts = [
        L.rounded_box("frame", (1.65, 2.10, 0.26), (0, 0, 0.19), bevel=0.03),
        L.rounded_box("headboard", (1.65, 0.09, 0.62), (0, 1.005, 0.45), bevel=0.03),
        L.rounded_box("mattress", (1.50, 1.92, 0.16), (0, -0.03, 0.40), bevel=0.05),
        L.rounded_box("pillow_a", (0.55, 0.36, 0.13), (-0.38, 0.72, 0.53), rot=(0, 0, 4), bevel=0.05),
        L.rounded_box("pillow_b", (0.55, 0.36, 0.13), (0.36, 0.70, 0.53), rot=(0, 0, -6), bevel=0.05),
        L.rounded_box("duvet", (1.58, 1.25, 0.09), (0, -0.42, 0.50), bevel=0.04),
    ]
    for sx in (-0.72, 0.72):
        for sy in (-0.93, 0.93):
            parts.append(L.box("leg", (0.08, 0.08, 0.12), (sx, sy, 0.06)))
    return L.join(parts, "bed")


def desk():
    parts = [
        L.rounded_box("top", (1.50, 0.70, 0.05), (0, 0, 0.735), bevel=0.015),
        L.box("side_a", (0.045, 0.62, 0.71), (0.71, 0.0, 0.355)),
        L.box("side_b", (0.045, 0.62, 0.71), (-0.71, 0.0, 0.355)),
        L.box("back", (1.38, 0.03, 0.38), (0, 0.30, 0.52)),
    ]
    return L.join(parts, "desk")


def monitor_keyboard():
    parts = []
    add_monitor(parts, (0, 0.10, 0))
    add_keyboard(parts, (0, -0.16, 0))
    parts.append(L.rounded_box("mouse", (0.06, 0.10, 0.03), (0.31, -0.16, 0.015), bevel=0.012))
    return L.join(parts, "monitor_keyboard")


def pc_tower():
    parts = [
        L.rounded_box("case", (0.20, 0.46, 0.44), (0, 0, 0.22), bevel=0.02),
        L.rounded_box("front", (0.16, 0.03, 0.36), (0, -0.235, 0.23), bevel=0.008),
    ]
    return L.join(parts, "pc_tower")


def office_chair():
    parts = [
        L.rounded_box("seat", (0.48, 0.46, 0.09), (0, 0, 0.47), bevel=0.04),
        L.rounded_box("backrest", (0.46, 0.07, 0.52), (0, 0.245, 0.80), rot=(-6, 0, 0), bevel=0.045),
        L.cyl("column", 0.032, 0.26, (0, 0, 0.32)),
        L.cyl("hub", 0.055, 0.06, (0, 0, 0.17)),
    ]
    import math
    for i in range(5):
        a = math.radians(i * 72 + 18)
        end = (0.26 * math.cos(a), 0.26 * math.sin(a), 0.05)
        parts.append(L.rod("leg_%d" % i, (0, 0, 0.15), end, 0.028))
        parts.append(L.sphere("caster_%d" % i, 0.035, (end[0], end[1], 0.035), segments=12, rings=8))
    return L.join(parts, "office_chair")


def _bench_base(parts):
    parts.append(L.rounded_box("top", (2.00, 0.75, 0.05), (0, 0, 0.895), bevel=0.012))
    parts.append(L.rounded_box("cabinet", (1.90, 0.65, 0.76), (0, 0.02, 0.46), bevel=0.02))
    # drawer seams
    for x in (-0.633, 0.0, 0.633):
        parts.append(L.rounded_box("drawer_%0.1f" % x, (0.56, 0.02, 0.30),
                                   (x, -0.31, 0.62), bevel=0.006))
        parts.append(L.box("handle_%0.1f" % x, (0.18, 0.025, 0.03), (x, -0.335, 0.70)))


def lab_bench_equip():
    parts = []
    _bench_base(parts)
    top = 0.92
    # flask
    parts.append(L.sphere("flask", 0.075, (-0.75, 0.12, top + 0.075), scale=(1, 1, 0.9)))
    parts.append(L.cyl("flask_neck", 0.022, 0.10, (-0.75, 0.12, top + 0.17)))
    # tube rack
    parts.append(L.rounded_box("rack", (0.32, 0.13, 0.05), (-0.30, 0.15, top + 0.025), bevel=0.01))
    for i in range(4):
        parts.append(L.cyl("tube_%d" % i, 0.016, 0.16, (-0.42 + i * 0.08, 0.15, top + 0.10), vertices=10))
    # microscope-ish scope
    parts.append(L.rounded_box("scope_base", (0.16, 0.22, 0.03), (0.28, 0.10, top + 0.015), bevel=0.01))
    parts.append(L.slab("scope_arm", (0.28, 0.19, top + 0.03), (0.28, 0.10, top + 0.34), 0.05, 0.05, bevel=0.015))
    parts.append(L.cyl("scope_tube", 0.035, 0.18, (0.28, 0.06, top + 0.26), rot=(30, 0, 0)))
    # sample cases
    parts.append(L.rounded_box("case_a", (0.30, 0.22, 0.12), (0.75, 0.12, top + 0.06), bevel=0.02))
    parts.append(L.rounded_box("case_b", (0.24, 0.18, 0.09), (0.72, -0.14, top + 0.045), rot=(0, 0, 14), bevel=0.02))
    return L.join(parts, "lab_bench_equip")


def lab_bench_terminal():
    parts = []
    _bench_base(parts)
    top = 0.92
    add_monitor(parts, (-0.45, 0.14, top), tag="a")
    add_monitor(parts, (0.35, 0.16, top), scale=0.85, tag="b")
    add_keyboard(parts, (-0.40, -0.14, top), tag="a")
    parts.append(L.rounded_box("unit", (0.30, 0.30, 0.14), (0.80, 0.05, top + 0.07), bevel=0.02))
    return L.join(parts, "lab_bench_terminal")


def gantry_frame():
    parts = [
        L.rounded_box("beam", (2.75, 0.15, 0.15), (0, 0, 2.55), bevel=0.03),
        L.rounded_box("hoist", (0.32, 0.24, 0.18), (0, 0, 2.46), bevel=0.03),
    ]
    for sx in (-1.25, 1.25):
        for sy in (-0.62, 0.62):
            parts.append(L.slab("leg", (sx, sy, 0), (sx, 0, 2.50), 0.13, 0.13, bevel=0.03))
            parts.append(L.rounded_box("pad", (0.26, 0.26, 0.05), (sx, sy, 0.025), bevel=0.012))
        parts.append(L.rod("brace", (sx, -0.34, 1.15), (sx, 0.34, 1.15), 0.035))
    return L.join(parts, "gantry_frame")


def stool():
    parts = [
        L.cyl("seat", 0.17, 0.05, (0, 0, 0.60)),
        L.cyl("column", 0.026, 0.42, (0, 0, 0.37)),
        L.cyl("base", 0.16, 0.035, (0, 0, 0.14)),
    ]
    return L.join(parts, "stool")


def pallet():
    parts = []
    for i in range(5):
        x = -0.52 + i * 0.26
        parts.append(L.rounded_box("deck_%d" % i, (0.16, 0.80, 0.022), (x, 0, 0.134), bevel=0.006, segments=2))
    for y in (-0.325, 0, 0.325):
        parts.append(L.rounded_box("bottom_%0.2f" % y, (1.20, 0.10, 0.022), (0, y, 0.011), bevel=0.006, segments=2))
        for x in (-0.525, 0, 0.525):
            parts.append(L.rounded_box("block_%0.2f_%0.2f" % (x, y), (0.10, 0.10, 0.09),
                                       (x, y, 0.068), bevel=0.008, segments=2))
    return L.join(parts, "pallet")


def barrel():
    parts = [
        L.cyl("body", 0.29, 0.85, (0, 0, 0.425)),
        L.cyl("ring_a", 0.302, 0.035, (0, 0, 0.26)),
        L.cyl("ring_b", 0.302, 0.035, (0, 0, 0.60)),
        L.cyl("lid", 0.27, 0.025, (0, 0, 0.862)),
    ]
    return L.join(parts, "barrel")


def shelf_unit():
    parts = []
    for sx in (-0.97, 0.97):
        for sy in (-0.245, 0.245):
            parts.append(L.box("post", (0.06, 0.06, 2.20), (sx, sy, 1.10)))
    for z in (0.10, 0.80, 1.50, 2.18):
        parts.append(L.rounded_box("shelf_%0.2f" % z, (2.00, 0.55, 0.045), (0, 0, z), bevel=0.012))
    return L.join(parts, "shelf_unit")


def unit_box_beveled():
    return L.rounded_box("unit_box_beveled", (1, 1, 1), (0, 0, 0.5), bevel=0.045)


# ---------------------------------------------------- extra props (busy rooms)

def cabinet():
    """Tall two-door storage cabinet / wardrobe. Front faces -Y."""
    w, d, h = 0.94, 0.50, 1.80
    parts = [L.rounded_box("body", (w, d, h), (0, 0, h / 2), bevel=0.025)]
    for sx, tag in ((-w / 4, "l"), (w / 4, "r")):
        parts.append(L.rounded_box("door_" + tag, (w / 2 - 0.03, 0.02, h - 0.12),
                                   (sx, -d / 2, h / 2), bevel=0.006))
    for sx in (-0.05, 0.05):
        parts.append(L.box("handle_%.2f" % sx, (0.03, 0.05, 0.20), (sx, -d / 2 - 0.02, h / 2)))
    parts.append(L.box("plinth", (w - 0.06, d - 0.06, 0.06), (0, 0, 0.03)))
    return L.join(parts, "cabinet")


def bookshelf():
    """Open bookshelf with a row of books on each level. Front faces -Y."""
    w, d, h = 0.90, 0.30, 1.86
    parts = []
    for sx in (-w / 2 + 0.03, w / 2 - 0.03):
        parts.append(L.box("side", (0.04, d, h), (sx, 0, h / 2)))
    levels = [0.02, 0.40, 0.78, 1.16, 1.54, 1.84]
    for z in levels:
        parts.append(L.rounded_box("shelf_%.2f" % z, (w, d, 0.03), (0, 0, z), bevel=0.006))
    for li, z in enumerate(levels[:-1]):
        n = 7
        for i in range(n):
            if (i * 5 + li * 3) % 7 == 0:
                continue  # a gap or two per shelf
            bx = -w / 2 + 0.10 + i * ((w - 0.20) / (n - 1))
            bh = 0.21 + ((i * 7 + li * 5) % 5) * 0.028
            tilt = ((i + li) % 3 - 1) * 7
            parts.append(L.rounded_box("book_%d_%d" % (li, i), (0.052, 0.21, bh),
                                       (bx, 0.0, z + 0.03 + bh / 2), rot=(0, tilt, 0), bevel=0.004))
    return L.join(parts, "bookshelf")


def sofa():
    """Two-seat sofa. Backrest faces +Y (seat opens toward -Y)."""
    w, d = 1.98, 0.92
    parts = [L.rounded_box("base", (w, d, 0.30), (0, 0, 0.20), bevel=0.05)]
    parts.append(L.rounded_box("back", (w, 0.20, 0.55), (0, 0.36, 0.48), bevel=0.06))
    for sx in (-w / 2 + 0.12, w / 2 - 0.12):
        parts.append(L.rounded_box("arm", (0.22, d, 0.44), (sx, 0, 0.32), bevel=0.06))
    for i, cx in enumerate((-0.5, 0.5)):
        parts.append(L.rounded_box("seat_%d" % i, (0.78, 0.62, 0.17), (cx, -0.06, 0.40), bevel=0.06))
        parts.append(L.rounded_box("bcush_%d" % i, (0.78, 0.16, 0.40), (cx, 0.28, 0.56), bevel=0.06))
    return L.join(parts, "sofa")


def coffee_table():
    parts = [L.rounded_box("top", (1.06, 0.56, 0.05), (0, 0, 0.38), bevel=0.015)]
    parts.append(L.rounded_box("shelf", (0.94, 0.46, 0.03), (0, 0, 0.12), bevel=0.01))
    for sx in (-0.46, 0.46):
        for sy in (-0.22, 0.22):
            parts.append(L.box("leg", (0.05, 0.05, 0.36), (sx, sy, 0.18)))
    return L.join(parts, "coffee_table")


def floor_lamp():
    parts = [
        L.cyl("base", 0.17, 0.04, (0, 0, 0.02)),
        L.cyl("pole", 0.022, 1.48, (0, 0, 0.76)),
        L.cyl("shade", 0.20, 0.30, (0, 0, 1.56)),
    ]
    return L.join(parts, "floor_lamp")


def crate():
    """Open-top slatted crate ~0.62 m."""
    s, t = 0.62, 0.05
    parts = []
    for tag, yy in (("front", -s / 2), ("back", s / 2)):
        for k, z in enumerate((0.09, 0.31, 0.53)):
            parts.append(L.rounded_box("%s_%d" % (tag, k), (s, t, 0.16), (0, yy, z), bevel=0.006, segments=2))
    for tag, xx in (("left", -s / 2), ("right", s / 2)):
        for k, z in enumerate((0.09, 0.31, 0.53)):
            parts.append(L.rounded_box("%s_%d" % (tag, k), (t, s, 0.16), (xx, 0, z), bevel=0.006, segments=2))
    for xx in (-s / 2, s / 2):
        for yy in (-s / 2, s / 2):
            parts.append(L.box("post", (0.06, 0.06, 0.62), (xx, yy, 0.31)))
    parts.append(L.box("bottom", (s, s, t), (0, 0, 0.035)))
    return L.join(parts, "crate")


def tool_cart():
    w, d = 0.70, 0.46
    parts = [
        L.rounded_box("top", (w, d, 0.04), (0, 0, 0.82), bevel=0.012),
        L.rounded_box("mid", (w, d, 0.03), (0, 0, 0.50), bevel=0.01),
        L.rounded_box("bot", (w, d, 0.03), (0, 0, 0.18), bevel=0.01),
    ]
    for sx in (-w / 2 + 0.04, w / 2 - 0.04):
        for sy in (-d / 2 + 0.04, d / 2 - 0.04):
            parts.append(L.box("post", (0.04, 0.04, 0.80), (sx, sy, 0.42)))
            parts.append(L.sphere("caster", 0.04, (sx, sy, 0.045), segments=10, rings=8))
    parts.append(L.slab("handle", (-w / 2, 0.16, 0.98), (-w / 2, -0.16, 0.98), 0.03, 0.03, bevel=0.01))
    parts.append(L.rounded_box("toolbox", (0.30, 0.20, 0.14), (0.12, 0, 0.91), bevel=0.02))
    parts.append(L.cyl("canister", 0.06, 0.16, (-0.18, 0.05, 0.90)))
    return L.join(parts, "tool_cart")


def server_rack():
    w, d, h = 0.62, 0.74, 1.86
    parts = [L.rounded_box("body", (w, d, h), (0, 0, h / 2), bevel=0.02)]
    for i in range(8):
        z = 0.20 + i * 0.20
        parts.append(L.rounded_box("unit_%d" % i, (w - 0.06, 0.02, 0.15), (0, -d / 2, z), bevel=0.005))
        parts.append(L.box("led_%d" % i, (0.04, 0.03, 0.04), (w / 2 - 0.13, -d / 2 - 0.01, z)))
    return L.join(parts, "server_rack")


def whiteboard():
    parts = [
        L.rounded_box("board", (1.5, 0.05, 0.95), (0, 0, 1.38), bevel=0.02),
        L.box("tray", (1.4, 0.11, 0.03), (0, -0.07, 0.88)),
    ]
    for sx in (-0.72, 0.72):
        parts.append(L.rod("leg", (sx, 0.0, 0.02), (sx, 0.0, 0.92), 0.03))
        parts.append(L.rod("foot", (sx, -0.36, 0.03), (sx, 0.36, 0.03), 0.03))
    return L.join(parts, "whiteboard")


BUILDERS = [
    ("bed", bed),
    ("desk", desk),
    ("monitor_keyboard", monitor_keyboard),
    ("pc_tower", pc_tower),
    ("office_chair", office_chair),
    ("lab_bench_equip", lab_bench_equip),
    ("lab_bench_terminal", lab_bench_terminal),
    ("gantry_frame", gantry_frame),
    ("stool", stool),
    ("pallet", pallet),
    ("barrel", barrel),
    ("shelf_unit", shelf_unit),
    ("unit_box_beveled", unit_box_beveled),
    ("cabinet", cabinet),
    ("bookshelf", bookshelf),
    ("sofa", sofa),
    ("coffee_table", coffee_table),
    ("floor_lamp", floor_lamp),
    ("crate", crate),
    ("tool_cart", tool_cart),
    ("server_rack", server_rack),
    ("whiteboard", whiteboard),
]

L.build_and_ship(BUILDERS, "Furniture")

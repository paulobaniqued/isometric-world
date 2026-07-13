"""Generates machine assets -> Assets/Models/Machines/*.fbx

conveyor_belt runs along X (5.4 m), belt surface at z=0.78.
"""
import os
import sys

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
import lib_common as L


def conveyor_belt():
    parts = [
        L.rounded_box("rail_a", (5.40, 0.07, 0.18), (0, 0.335, 0.77), bevel=0.015),
        L.rounded_box("rail_b", (5.40, 0.07, 0.18), (0, -0.335, 0.77), bevel=0.015),
        L.box("belt", (5.30, 0.60, 0.05), (0, 0, 0.755)),
        L.cyl("roller_a", 0.06, 0.60, (2.68, 0, 0.755), rot=(90, 0, 0)),
        L.cyl("roller_b", 0.06, 0.60, (-2.68, 0, 0.755), rot=(90, 0, 0)),
    ]
    for x in (-2.2, 0.0, 2.2):
        for y in (-0.28, 0.28):
            parts.append(L.box("leg_%0.1f_%0.2f" % (x, y), (0.08, 0.08, 0.70), (x, y, 0.35)))
        parts.append(L.box("cross_%0.1f" % x, (0.08, 0.56, 0.06), (x, 0, 0.12)))
    # items riding the belt
    parts.append(L.rounded_box("box_a", (0.42, 0.40, 0.34), (-1.60, 0.02, 0.95), rot=(0, 0, 8), bevel=0.03))
    parts.append(L.rounded_box("box_b", (0.36, 0.34, 0.28), (0.25, -0.04, 0.92), rot=(0, 0, -12), bevel=0.03))
    parts.append(L.rounded_box("box_c", (0.30, 0.30, 0.24), (1.55, 0.03, 0.90), rot=(0, 0, 4), bevel=0.028))
    return L.join(parts, "conveyor_belt")


def control_panel():
    parts = [
        L.rounded_box("pedestal", (0.36, 0.30, 0.95), (0, 0, 0.475), bevel=0.02),
        L.rounded_box("console", (0.46, 0.36, 0.06), (0, -0.04, 1.00), rot=(-18, 0, 0), bevel=0.015),
        L.rounded_box("screen", (0.32, 0.025, 0.20), (0, 0.10, 1.16), rot=(-12, 0, 0), bevel=0.01),
    ]
    return L.join(parts, "control_panel")


BUILDERS = [
    ("conveyor_belt", conveyor_belt),
    ("control_panel", control_panel),
]

L.build_and_ship(BUILDERS, "Machines")

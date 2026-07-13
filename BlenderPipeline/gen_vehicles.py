"""Generates vehicle assets -> Assets/Models/Vehicles/*.fbx

Clay-styled low-poly cars for the L-shaped community scene.
Length runs along Y (front at -Y), same convention as the other assets.
"""
import os
import sys

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
import lib_common as L


def _wheels(parts, halfx, front_y, back_y, r=0.34, width=0.26):
    for sx in (-halfx, halfx):
        for sy in (front_y, back_y):
            parts.append(L.cyl("wheel", r, width, (sx, sy, r), rot=(0, 90, 0)))


def car():
    parts = [
        L.rounded_box("chassis", (1.82, 4.05, 0.40), (0, 0, 0.50), bevel=0.10),
        L.rounded_box("body", (1.74, 3.66, 0.46), (0, 0, 0.74), bevel=0.18),
        L.rounded_box("cabin", (1.60, 2.05, 0.50), (0, -0.18, 1.14), bevel=0.22),
        L.rounded_box("hood", (1.66, 0.95, 0.16), (0, 1.34, 0.90), bevel=0.07),
        L.rounded_box("trunk", (1.66, 0.70, 0.18), (0, -1.55, 0.92), bevel=0.07),
    ]
    _wheels(parts, 0.82, -1.28, 1.30)
    return L.join(parts, "car")


def van():
    parts = [
        L.rounded_box("chassis", (1.94, 4.70, 0.46), (0, 0, 0.56), bevel=0.09),
        L.rounded_box("body", (1.86, 4.35, 1.10), (0, -0.12, 1.20), bevel=0.16),
        L.rounded_box("hood", (1.82, 1.05, 0.52), (0, 1.95, 0.74), bevel=0.10),
        L.rounded_box("windshield", (1.66, 0.10, 0.62), (0, 1.42, 1.35), rot=(24, 0, 0), bevel=0.02),
    ]
    _wheels(parts, 0.86, -1.55, 1.60, r=0.38, width=0.30)
    return L.join(parts, "van")


BUILDERS = [
    ("car", car),
    ("van", van),
]

L.build_and_ship(BUILDERS, "Vehicles")

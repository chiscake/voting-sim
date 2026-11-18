"""Report (and optionally expand) near-zero UV islands on the active mesh.

Edit CONFIG, then execute via Blender MCP. Must run with the object selectable;
uses edit-mode bmesh APIs (no to_mesh while editing).
"""

import bmesh
import bpy
from mathutils import Vector

CONFIG = {
    "object_name": None,  # None = active object
    "uv_layer": None,  # None = active render UV
    "epsilon": 1e-5,
    "fix": False,  # True: inflate zero-width islands using face tangent basis
    "inflate": 0.02,  # UV size along collapsed axis after fix
}


def _target_object():
    name = CONFIG["object_name"]
    obj = bpy.data.objects.get(name) if name else bpy.context.view_layer.objects.active
    if obj is None or obj.type != "MESH":
        raise RuntimeError("Active/target must be a mesh object")
    return obj


def _uv_area(uvs):
    # Shoelace on loop UVs for a face
    a = 0.0
    n = len(uvs)
    for i in range(n):
        u1, v1 = uvs[i]
        u2, v2 = uvs[(i + 1) % n]
        a += u1 * v2 - u2 * v1
    return abs(a) * 0.5


def main():
    obj = _target_object()
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    bm = bmesh.from_edit_mesh(obj.data)
    bm.faces.ensure_lookup_table()

    uv_name = CONFIG["uv_layer"]
    if uv_name:
        uv_layer = bm.loops.layers.uv.get(uv_name)
    else:
        uv_layer = bm.loops.layers.uv.active
    if uv_layer is None:
        raise RuntimeError("No UV layer")

    eps = CONFIG["epsilon"]
    bad = []
    for face in bm.faces:
        uvs = [loop[uv_layer].uv.copy() for loop in face.loops]
        xs = [uv.x for uv in uvs]
        ys = [uv.y for uv in uvs]
        w = max(xs) - min(xs)
        h = max(ys) - min(ys)
        area = _uv_area([(uv.x, uv.y) for uv in uvs])
        if w < eps or h < eps or area < eps:
            bad.append((face.index, w, h, area))

    print(f"object={obj.name} collapsed_or_tiny_faces={len(bad)}")
    for idx, w, h, area in bad[:50]:
        print(f"  face={idx} w={w:.8f} h={h:.8f} area={area:.8f}")

    if CONFIG["fix"] and bad:
        inflate = CONFIG["inflate"]
        bad_set = {i for i, _, _, _ in bad}
        for face in bm.faces:
            if face.index not in bad_set:
                continue
            # Face-aligned UV from local tangent/bitangent
            n = face.normal.normalized()
            tangent = Vector((1, 0, 0))
            if abs(n.dot(tangent)) > 0.9:
                tangent = Vector((0, 1, 0))
            tangent = (tangent - n * tangent.dot(n)).normalized()
            bitangent = n.cross(tangent).normalized()
            center = face.calc_center_median()
            coords = []
            for loop in face.loops:
                rel = loop.vert.co - center
                coords.append((rel.dot(tangent), rel.dot(bitangent)))
            us = [c[0] for c in coords]
            vs = [c[1] for c in coords]
            min_u, max_u = min(us), max(us)
            min_v, max_v = min(vs), max(vs)
            du = max(max_u - min_u, 1e-8)
            dv = max(max_v - min_v, 1e-8)
            # Fit into inflate x inflate box centered in 0..1
            for loop, (su, sv) in zip(face.loops, coords):
                u = ((su - min_u) / du) * inflate
                v = ((sv - min_v) / dv) * inflate
                loop[uv_layer].uv = Vector((u, v))
        bmesh.update_edit_mesh(obj.data)
        print(f"fixed_faces={len(bad)} inflate={inflate}")

    bpy.ops.object.mode_set(mode="OBJECT")


main()

"""Bake active object's material to a PNG (Diffuse Color by default).

Edit CONFIG, ensure object selected/active, export material uses Image→Principled
or assembly is assigned for bake. Execute via Blender MCP.
"""

import math
import os

import bpy

CONFIG = {
    "object_name": None,  # None = active
    "material_name": None,  # material to bake from (slot or name)
    "image_name": "BakeTarget",
    "output_path": "Assets/Art/Shared/Textures/T_Example.png",  # repo-relative or absolute
    "repo_root": None,  # None = derive from blend path parents
    "width": 512,
    "height": 512,
    "samples": 8,
    "margin": 16,
    "bake_type": "DIFFUSE",  # DIFFUSE or NORMAL
    "pass_filter": {"COLOR"},  # for DIFFUSE: COLOR only (no direct/indirect)
}


def _repo_root():
    if CONFIG["repo_root"]:
        return CONFIG["repo_root"]
    blend = bpy.data.filepath
    if not blend:
        raise RuntimeError("Save the .blend first or set CONFIG['repo_root']")
    # Art/Source/.../file.blend → climb until voting-sim root (has Assets/)
    cur = os.path.dirname(blend)
    for _ in range(8):
        if os.path.isdir(os.path.join(cur, "Assets")):
            return cur
        parent = os.path.dirname(cur)
        if parent == cur:
            break
        cur = parent
    raise RuntimeError("Could not find repo root with Assets/; set repo_root")


def _abs_out(path: str) -> str:
    if os.path.isabs(path):
        return path
    return os.path.normpath(os.path.join(_repo_root(), path.replace("\\", "/")))


def _pot(n: int) -> int:
    return 1 << max(0, int(math.ceil(math.log2(max(1, n)))))


def main():
    name = CONFIG["object_name"]
    obj = bpy.data.objects.get(name) if name else bpy.context.view_layer.objects.active
    if obj is None or obj.type != "MESH":
        raise RuntimeError("Need a mesh object")

    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

    mat_name = CONFIG["material_name"]
    if mat_name:
        mat = bpy.data.materials.get(mat_name)
        if mat is None:
            raise KeyError(mat_name)
        if obj.data.materials:
            obj.data.materials[0] = mat
        else:
            obj.data.materials.append(mat)
    else:
        if not obj.data.materials:
            raise RuntimeError("Object has no materials")
        mat = obj.data.materials[0]

    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    img_node = nodes.new("ShaderNodeTexImage")
    img_node.name = "BakeReceiver"
    w, h = _pot(CONFIG["width"]), _pot(CONFIG["height"])
    image = bpy.data.images.get(CONFIG["image_name"])
    if image is None:
        image = bpy.data.images.new(
            CONFIG["image_name"], width=w, height=h, alpha=False
        )
    else:
        image.scale(w, h)
    img_node.image = image
    nodes.active = img_node

    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = CONFIG["samples"]
    scene.cycles.bake_type = CONFIG["bake_type"]
    bake = scene.render.bake
    bake.margin = CONFIG["margin"]
    bake.use_selected_to_active = False
    if CONFIG["bake_type"] == "DIFFUSE":
        bake.use_pass_direct = False
        bake.use_pass_indirect = False
        bake.use_pass_color = True

    bpy.ops.object.bake(type=CONFIG["bake_type"])

    out = _abs_out(CONFIG["output_path"])
    os.makedirs(os.path.dirname(out), exist_ok=True)
    image.filepath_raw = out
    image.file_format = "PNG"
    image.save()
    # Force Single Image semantics for later export
    image.source = "FILE"
    image.reload()
    print(f"OK baked {out} size={w}x{h} type={CONFIG['bake_type']}")


main()

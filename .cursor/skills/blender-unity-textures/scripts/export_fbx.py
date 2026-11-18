"""Export selected (or named) mesh objects to Assets/Art/.../Models as FBX.

voting-sim conventions: Path Mode COPY, Forward -Z, Up Y, Apply Transform.
Materials on export should already be Image→Principled (*Image / M_* names).
"""

import os

import bpy

CONFIG = {
    "object_names": None,  # None = current selection; list of names otherwise
    "output_path": "Assets/Art/Interior/Home/Models/Room.fbx",
    "repo_root": None,
    "apply_scale": True,
    "embed_textures": False,  # PNGs already under Assets/Art; Copy path mode still
}


def _repo_root():
    if CONFIG["repo_root"]:
        return CONFIG["repo_root"]
    blend = bpy.data.filepath
    if not blend:
        raise RuntimeError("Save the .blend first or set CONFIG['repo_root']")
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


def main():
    bpy.ops.object.mode_set(mode="OBJECT")
    names = CONFIG["object_names"]
    if names:
        bpy.ops.object.select_all(action="DESELECT")
        for name in names:
            obj = bpy.data.objects.get(name)
            if obj is None:
                raise KeyError(f"Missing object: {name}")
            obj.select_set(True)
            bpy.context.view_layer.objects.active = obj
    selected = [o for o in bpy.context.selected_objects if o.type == "MESH"]
    if not selected:
        raise RuntimeError("No mesh objects selected for FBX export")

    if CONFIG["apply_scale"]:
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    out = _abs_out(CONFIG["output_path"])
    os.makedirs(os.path.dirname(out), exist_ok=True)

    # Path Mode COPY = 'COPY'; Forward -Z, Up Y — Unity-friendly
    bpy.ops.export_scene.fbx(
        filepath=out,
        use_selection=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z",
        axis_up="Y",
        apply_unit_scale=True,
        bake_space_transform=True,
        path_mode="COPY",
        embed_textures=CONFIG["embed_textures"],
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
    )
    print(f"OK exported FBX {out} objects={[o.name for o in selected]}")


main()

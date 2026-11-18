"""Ensure assembly material exists and create/update a baked *Image pair.

Run via Blender MCP by executing this file's contents after editing CONFIG.
Looks up nodes by type, never by localized name.
"""

import bpy

CONFIG = {
    "assembly_name": "Walnut",
    "image_name": None,  # default: {assembly_name}Image
    "create_assembly_if_missing": True,
}


def _principled(nodes):
    return next(n for n in nodes if n.type == "BSDF_PRINCIPLED")


def _output(nodes):
    return next(n for n in nodes if n.type == "OUTPUT_MATERIAL")


def ensure_assembly(name: str) -> bpy.types.Material:
    mat = bpy.data.materials.get(name)
    if mat is None:
        if not CONFIG["create_assembly_if_missing"]:
            raise KeyError(f"Assembly material missing: {name}")
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        return mat
    mat.use_nodes = True
    return mat


def ensure_image_pair(assembly_name: str, image_name: str) -> bpy.types.Material:
    mat = bpy.data.materials.get(image_name)
    if mat is None:
        mat = bpy.data.materials.new(image_name)
    mat.use_nodes = True
    nt = mat.node_tree
    nodes = nt.nodes
    links = nt.links
    nodes.clear()

    out = nodes.new("ShaderNodeOutputMaterial")
    out.location = (300, 0)
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.location = (0, 0)
    img = nodes.new("ShaderNodeTexImage")
    img.location = (-300, 0)
    img.label = "BakeOrAlbedo"

    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    links.new(img.outputs["Color"], bsdf.inputs["Base Color"])
    return mat


def main():
    assembly = CONFIG["assembly_name"]
    image = CONFIG["image_name"] or f"{assembly}Image"
    ensure_assembly(assembly)
    ensure_image_pair(assembly, image)
    print(f"OK assembly={assembly} image_mat={image}")


main()

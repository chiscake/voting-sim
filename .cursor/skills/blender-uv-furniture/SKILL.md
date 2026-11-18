---
name: blender-uv-furniture
description: >
  Fixes and authors UVs for furniture and interior meshes in voting-sim Blender
  files: tile grain alignment, atlas packing, collapsed islands, and stretch.
  Use when wood grain is wrong, Minimum Stretch overflows, UV islands collapse,
  or before baking tile/atlas textures for chairs, tables, sofas, walls, cabinets.
---

# Blender UV for furniture

UV work for bake readiness. Texture resolution and bake steps belong to
`blender-unity-textures` / [`docs/agents/blender-unity-textures.md`](../../../docs/agents/blender-unity-textures.md).

## When to use

- Grain rotated wrong on a tabletop or plank
- Texture stretches on one axis (often zero-width UV islands)
- Choosing tile UV vs unique atlas unwrap
- User mentions Minimum Stretch, Smart Project, or pack islands

**Not this skill:** node assembly, Cycles samples, Unity materials.

## Mode choice

| Mode      | UV goal                                                                                         | Bake                                                   |
| --------- | ----------------------------------------------------------------------------------------------- | ------------------------------------------------------ |
| **Tile**  | Each face (or island) maps into 0–1 of the **physical tile**; rotate for grain; Repeat in Unity | Bake on LookdevGrid / tile plane, not per-object atlas |
| **Atlas** | All islands packed into 0–1 for **this** mesh; Clamp in Unity                                   | Bake on the object UV                                  |

Do not hang a door/wall atlas PNG on a different mesh.

## Rules

1. Apply object scale before unwrap/bake.
2. Tile surfaces: prefer even islands + rotate 90° for grain; avoid packing as a unique atlas.
3. Atlas: Smart Project or manual seams → average island scale → pack into 0–1 with margin.
4. Reject collapsed islands (zero width/height in UV). Re-project those faces on a tangent/bitangent plane, then repack.
5. Minimum Stretch: OK for organic/cloth; bad default for plank tabletops (grain + overflow outside 0–1). Prefer Follow Active Quads / cube/project + rotate for wood planks.
6. Edit-mode UV edits: use `bmesh.from_edit_mesh` / `bmesh.update_edit_mesh` — do not `bmesh.to_mesh()` while in edit mode.

## Workflow

1. Identify tile vs atlas from the material plan / manifest.
2. Fix scale; enter Edit Mode; select relevant faces.
3. Unwrap; rotate islands for grain; pack if atlas.
4. Validate: no zero-area islands; for atlas, `u,v` roughly in `[0,1]`.
5. Hand off to bake skill.

## Helper

- `scripts/fix_uv_islands.py` — report and optionally expand near-zero UV islands on the active object.

---
name: blender-unity-textures
description: >
  Bakes Blender procedural materials to PNG tiles or atlases, writes T_* textures
  and M_* URP Lit materials for voting-sim, and exports FBX when a Unity model is
  missing. Use when baking textures, setting Max Size/Wrap/sRGB, creating Shared
  materials, remapping FBX slots, or running bake_to_png / export_fbx helpers.
---

# Blender → Unity textures

Agent workflow for bake + Unity import. Full rules live in
[`docs/agents/blender-unity-textures.md`](../../../docs/agents/blender-unity-textures.md) — **read it** before baking.

Room-wide batch and manifest: `art-bake-orchestrator`. Shader graphs: `blender-shader-assembly`. UV fixes: `blender-uv-furniture`.

## When to use

- Bake tile or atlas albedo/normal from an assembly
- Create/update `T_*.png` and `M_*.mat` (URP Lit)
- Single-prop needs an FBX under `Assets/Art/` and none exists
- Configure TextureImporter (sRGB, Wrap, Max Size, Aniso)

## Core formula

`resolution_px = physical_size_m × density_px_per_m` → round each axis to power of two.

| Kind  | Wrap   | Notes                                                    |
| ----- | ------ | -------------------------------------------------------- |
| Tile  | Repeat | Bake on plane = tile size; Unity Tiling = surface / tile |
| Atlas | Clamp  | Object UV packed 0–1                                     |
| Flat  | n/a    | No bake; color + Smoothness + Metallic only              |

`Smoothness = 1 − Roughness`. Procedurals do not survive FBX — export materials must be Image → Principled only.

## Workflow

1. Confirm UV ready (`blender-uv-furniture` if stretch/grain issues).
2. Cycles; Object Mode; active bake Image Texture unconnected receiver; samples 1–16; margin 16.
3. Save PNG to `Assets/Art/.../Textures/T_Name.png` (Source = Single Image).
4. Unity MCP: set importer flags; create/update `M_Name` URP Lit; assign maps.
5. If the prop/room FBX is missing under the planned `Assets/Art/.../Models/` path, run `scripts/export_fbx.py` (same conventions as the doc FBX section).
6. Remapper binds slots whose names exactly match Shared `M_*` mats.

## Helpers (Blender MCP)

Paste/execute script contents in the live Blender session:

- `scripts/bake_to_png.py` — create image, bake Diffuse Color, save PNG
- `scripts/export_fbx.py` — export selected objects to `Assets/Art/.../Models/`

Unity checklist: `scripts/unity_import_material.md`.

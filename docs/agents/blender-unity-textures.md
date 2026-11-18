# Blender → Unity textures and materials

Agent-facing pipeline for voting-sim. Source Blender files live in `Art/Source/` (outside `Assets/`). Runtime art lives under `Assets/Art/`.

## Core rules

1. Procedural nodes do not survive FBX export. Only Principled BSDF with Image Texture nodes connected **directly** to its inputs is exported. Bake procedurals to images or rebuild in Unity (Shader Graph).
2. Resolution: `resolution_px = physical_size_m × density_px_per_m`. Round each axis to a power of two. Never pick resolution “just in case”.
3. A baked image must exist as a **file on disk** (Image Source = Single Image) before Unity import. Generated-only images have nothing to copy.
4. Roughness, Metallic, IOR, Coat, etc. are **not** in baked images and are not transferred by FBX. Set them again on the Unity URP Lit material (`Smoothness = 1 − Roughness`).

## Material classes

| Kind | LookdevGrid / bake | Unity |
| --- | --- | --- |
| Flat (paint, plastic, metal, glass) | Small preview plane; **no bake** | Color + Smoothness + Metallic |
| Repeating pattern | Plane = **physical tile size**; bake tile | `T_*` Wrap Repeat; Tiling = surface / tile |
| Unique (mask, flag) | Object UV or mask; not a library tile | Clamp; optional Shader Graph later |

## Naming

- Materials: `M_PascalCase` (Unity `.mat` name **and** Blender material name must match for `SharedMaterialRemapper`).
- Textures: `T_PascalCase`.
- Shared materials: `Assets/Art/Shared/Materials/`. Shared / pilot tiles: `Assets/Art/Shared/Textures/`.
- Location-unique texture **content** may live under `Assets/Art/Interior/<Loc>/Textures/`. Prefer Shared-first for materials.
- Blender sources: `Art/Source/Shared/MaterialsLibrary.blend`, `Art/Source/Interior/<Loc>/…`.

## Densities (starting)

| Viewing distance | Density (px/m) |
| --- | --- |
| Far / background | ~100 |
| Mid-range | ~200 |
| Near (1–2 m) | 300–500 |
| Hero close-up | 500–1000+ |

Fine detail needs ≥2–3 px. Example: 3 mm seam → ≥700–1000 px/m via a small high-density tile, not a whole-room bake.

Aspect ratio of the image must match the baked surface/tile; keep density equal on both axes. Prefer adjusting tile physical size for exact power-of-two ratios.

Large surfaces: small tile + low-res mask, not one giant albedo.

```
Tiling (per axis) = surface_size / tile_size
```

## Baking (Blender)

- Engine: **Cycles**. Object Mode; bake object selected and active. Apply scale before UV/bake.
- Active unconnected Image Texture node receives the bake. UV: face fills 0–1 (Reset).
- Samples: 1–16 for Diffuse Color / Normal (no lighting noise). Margin: 16 px, Adjacent Faces.
- Map types: Albedo = Diffuse Color only (sRGB); Normal = Tangent (Non-Color); Roughness (Non-Color); channel masks via Emit (Non-Color).
- Save As PNG; Source must become Single Image.
- Export material: only Image Texture → Principled (+ Normal Map node). Strip procedurals from the export version.

### LookdevGrid in MaterialsLibrary

Collections: `Materials`, `LookdevGrid`, `BakeSetup`.

- Flat mats: uniform small preview planes (lookdev only).
- Tile mats: plane mesh size = physical tile meters; image size from formula.
- Grid layout is for visibility; resolution comes from the Image datablock, not plane size in the viewport.

## FBX export

Object Mode, selected objects; apply scale. Path Mode Copy (embed textures or ship PNGs beside FBX). FBX Units Scale; Forward `-Z`; Up `Y`; Apply Transform. Move `.fbx` and images into `Assets/Art/…` together.

## Unity import

| Texture | sRGB | Type | Wrap |
| --- | --- | --- | --- |
| Albedo unique | on | Default | Clamp |
| Albedo tile | on | Default | Repeat |
| Normal | n/a | Normal map | Repeat for tiles |
| Channel mask | **off** | Default | Clamp |

- **Max Size** must be ≥ source resolution (default 2048 silently downsamples).
- Mip Maps on; Aniso 4–9 for floors. High Quality compression when fine detail matters.
- Model: Scale Factor 1, Convert Units on. Prefer Shared materials via remapper (`Assets/Art/Shared/Materials`, exact name match) over Extract Materials.

## MCP automation runbook

```text
Spec size_m + density
  → Blender MCP execute_blender_code / Unity blender_bridge run_python
  → bake + save Assets/Art/.../Textures/T_Name.png
  → Unity MCP: TextureImporter flags (sRGB, Wrap, Max Size)
  → Unity MCP: create/update M_Name URP Lit (maps, Smoothness, Metallic, Tiling)
  → SharedMaterialRemapper binds FBX slots named M_Name
```

Inside `Assets/`: rename/move only via Unity (`AssetDatabase` / Unity MCP) so GUID/`.meta` stay valid; then `git add -A`. Do not `git mv` the same files in parallel.

`.blend` out of `Assets/`: filesystem/git into `Art/Source/`; delete leftover Unity `.meta` for those blends.

## Pilot example (canonical)

Tile: **0.24 × 0.96 m** at ~2133 px/m → image **512 × 2048**.

1. In `MaterialsLibrary.blend`, LookdevGrid plane at 0.24 × 0.96 m; procedural wood → bake Diffuse Color to `T_PilotWoodTile.png` under `Assets/Art/Shared/Textures/`.
2. Export-ready material `M_PilotWoodTile`: Image Texture → Base Color only; record Roughness/Metallic for Unity.
3. Unity: albedo sRGB, Wrap Repeat, Max Size ≥ 2048; URP Lit Base Map = texture; Smoothness = `1 − Roughness`; Tiling as needed for the target surface.

Do not rebake the whole library in routine chores; extend tile-by-tile when a material needs a pattern.

## Validation checklist

**Blender**

- [ ] Images saved (Single Image)
- [ ] Size = planned size × density, power of two, correct aspect
- [ ] Albedo sRGB; normal/masks Non-Color
- [ ] Tile seams checked side-by-side
- [ ] Export material = Image Texture → Principled only
- [ ] Scale applied

**Unity**

- [ ] Textures assigned (Base Map / Normal Map)
- [ ] Max Size / sRGB / Wrap match table above
- [ ] Smoothness / Metallic set from Blender numbers
- [ ] Tiling = surface / tile
- [ ] Checked at real camera distance

## Common failures

| Symptom | Cause | Fix |
| --- | --- | --- |
| No valid selected objects | Nothing selected / hidden | Select in viewport; enable visibility |
| Default color, no image | Generated never saved / not copied | Save PNG; ship with FBX; assign Base Map |
| Flat albedo | Pattern only in Bump/Normal | Bake Normal or drive Base Color |
| Slow bake | High samples | Max Samples 1–16 |
| Blurry in Unity | Max Size 2048 or low density | Raise Max Size / density |
| Seams vanish | Detail &lt; 2 px | Smaller high-density tile |

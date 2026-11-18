---
name: blender-shader-assembly
description: >
  Builds and edits Blender procedural material node graphs (wood, fabric, plaster)
  for voting-sim, keeping an assembly vs baked *Image pair. Use when creating or
  tweaking Principled BSDF shaders in MaterialsLibrary or location .blend files,
  or when the user mentions node assembly, Wave/Noise wood, fabric weave, or
  Shared-first materials before bake.
---

# Blender shader assembly

Procedural materials live as **assemblies**. Export/Unity uses a paired **baked**
material (`*Image` or `M_*`) — never ship raw procedurals via FBX.

Canonical texture/bake rules: [`docs/agents/blender-unity-textures.md`](../../../docs/agents/blender-unity-textures.md).
Bake/export: skill `blender-unity-textures`. Room batch: `art-bake-orchestrator`.

## When to use

- New wood / fabric / paint / plaster look in Blender
- Edit Wave, Noise, ColorRamp, Mapping on an existing assembly
- Add material to `Art/Source/Shared/MaterialsLibrary.blend` (Shared-first)

**Not this skill:** resolution formula, Cycles bake, Unity importer, UV stretch fixes.

## Contract

| Role | Blender name | Purpose |
| --- | --- | --- |
| Assembly | `Walnut`, `Wall`, … | Procedural graph for lookdev / bake source |
| Baked | `WalnutImage` / prefer Unity slot `M_Walnut` | Image Texture → Principled → Output only |

Keep both. Do not delete the assembly after bake.

## Rules

1. Shared-first: new reusable mats go in `MaterialsLibrary.blend`; Unity `.mat` under `Assets/Art/Shared/Materials` as `M_PascalCase`. Blender slot names for remapper must match Unity mat names exactly when exporting.
2. Find nodes by `n.type` (e.g. `BSDF_PRINCIPLED`, `TEX_IMAGE`), never by localized UI names.
3. Prefer **Object** coordinates for procedural wood/plaster on furniture; **UV** when the bake target is a repeating tile plane or atlas.
4. Flat colors (plastic, metal, glass): Principled numbers only — no bake; Unity sets Base Color + Smoothness + Metallic.
5. Run helpers via Blender MCP by pasting/executing script file contents (not as add-ons).

## Workflow

1. Open/target the correct `.blend` (`MaterialsLibrary` or location file).
2. Create or edit the assembly graph (see [reference.md](reference.md)).
3. Ensure a Principled BSDF feeds Material Output.
4. For lookdev tiles, use LookdevGrid planes sized to physical tile meters.
5. Stop here unless the user asked to bake — then hand off to `blender-unity-textures`.

## Helper

- `scripts/assemble_principled_pair.py` — ensure assembly exists and create/update empty `*Image` pair (Image → Principled → Output).

# Art bake pitfalls (Home / Room lessons)

Lessons from baking `Art/Source/Interior/Home/Room.blend` into Unity.
Read when a bake looks black, Unity shows smeared atlas strips, remaps miss
location mats, or `check_fbx_gate.py` keeps asking to re-export.

## 1. Library-linked assemblies cannot receive bake

**Symptom:** Blender logs `No active and selected image texture node found`;
`bpy.ops.object.bake` returns `CANCELLED`; saved PNG is tiny / all black;
`img_node.image = image` leaves `node.image is None`.

**Cause:** Room slots often use materials **linked** from
`Art/Source/Shared/MaterialsLibrary.blend`. Linked node trees reject Image
assignments for the bake receiver.

**Do this:**

1. `bake_mat = src.copy()` — local copy (`library is None`).
2. Assign the copy to the bake plane / temporary object slots.
3. Create BakeReceiver on the **copy**, bake, save PNG.
4. Restore original slots; delete `*_BakeCopy` mats when done.

Never bake on the linked assembly datablock itself.

## 2. Treat bake success as observable, not as “save returned”

**Symptom:** Script prints `OK baked …` while the texture is black / flat.

**Do this after every bake:**

1. Require `'FINISHED' in bpy.ops.object.bake(...)`.
2. Sample pixels (stride OK): albedo should have meaningful min/max (near-black
   plastic can be dark but not all zeros unless that is intended).
3. Confirm file size on disk is not a few KB for a 512–2048 map (except tiny
   flat dark tiles).

`bake_to_png.py` does not yet enforce this — agents must check manually until the
helper is hardened.

## 3. BakeReceiver setup (Blender 4 / 5)

**Do this on the bake material (local copy):**

1. Object Mode; mesh selected and active; material on slot 0.
2. Remove leftover nodes named/labeled `BakeReceiver`.
3. Deselect all nodes; new Image Texture; `select = True`; `nodes.active = img_node`.
4. Assign a real Image datablock **before** bake; confirm `img_node.image is not None`.
5. Cycles; Diffuse Color-only (no direct/indirect) or Normal; margin 16; samples 1–16.
6. Save PNG; set `image.source = 'FILE'`; reload.

## 4. Atlas UV overlap destroys the main surface

**Symptom:** In Unity, most of a floor/wall looks fine, but a strip or “cross”
is smeared / wrong grain. UV Editor shows a large island filling 0–1 **and**
small islands sitting on top of it.

**Cause:** Atlas bake writes every face into the same PNG by UV. Overlapping
islands **overwrite** each other. A top face mapped to full 0–1 plus sides
packed into the middle = middle of the top surface samples side-face bake.

**Do this (thin slabs: Floor, Roof, similar boxes):**

1. Apply scale.
2. Dominant face (+Z top): one large island, **aspect ≈ physical size**
   (e.g. 4.5×6.2 m → UV width/height ≈ 0.726), with margin.
3. Sides + bottom: pack into the leftover strip; **zero bbox overlaps**.
4. Validate AABB overlap between face UV bounds before bake.
5. Rebake atlas albedo (+ normal if Bump linked); re-export FBX.

Do not rely on Smart Project alone for thin boxes — it often leaves overlaps
or uneven scale. See `blender-uv-furniture`.

## 5. SharedMaterialRemapper only sees Shared

**Code:** `Assets/Editor/SharedMaterialRemapper.cs` loads only
`Assets/Art/Shared/Materials`.

**Symptom:** After FBX import, `M_Walnut` etc. remap correctly, but
`M_Wallpaper` / `M_Floor` stay embedded FBX materials (wrong/missing maps).

**Do this:**

- Location-unique mats from the manifest (`Assets/Art/Interior/<Loc>/Materials/`)
  need an explicit `ModelImporter.AddRemap` for those slot names after import,
  **or** move the `.mat` into Shared if it is truly shared.
- Spot-check: for each renderer, `AssetDatabase.GetAssetPath(sharedMaterial)`
  must point at the intended `.mat`, not `(embedded)`.

## 6. FBX export naming (assembly vs Image)

Procedurals do not survive FBX. Remapper / Unity slots need **exact** `M_*` names.

**Do this:**

1. Keep lookdev assemblies as `M_*` (procedural).
2. Keep export pairs as `M_*Image` (Image → Principled [→ Normal Map]).
3. For export: temporarily assign `*Image` to slots; rename so exported material
   names are exactly `M_*` (stash assemblies if names collide, including after
   `make_local` on linked libs).
4. Export selection with `export_fbx.py` conventions.
5. Restore assembly names and object slots; save `.blend`.

Exporting while slots still point at Wave/Noise assemblies yields pink/default
or wrong look in Unity even when PNGs exist.

## 7. FBX gate vs save order

`check_fbx_gate.py` (`export_fbx: auto`): export if FBX missing **or**
`.blend` mtime **newer** than FBX.

**Symptom:** You exported correctly, then `save_mainfile()` after restoring
slots → gate reports `NEED_EXPORT` again though FBX content is fine.

**Do this (pick one):**

- Preferred: finish Blender naming/slot restore → `save_mainfile` → export FBX
  last (so FBX mtime ≥ blend), **or**
- Export, then restore/save, then bump FBX mtime slightly after the save if the
  mesh/material export payload did not change.

Do not re-export with procedural slots just to silence the gate.

## 8. Manifest paths beat leftover files

**Symptom:** Agent updates `Home/Textures/T_HoneyOak.png` while the manifest
points at `Shared/Textures/T_HoneyOak.png`, or rebakes `T_Wall` while Unity
uses `T_Wallpaper`.

**Do this:**

- Always write `texture:` / `unity_mat:` from `bake_manifest.yaml`.
- After a location rebake, treat old names (`T_Wall`, duplicate tiles under Home)
  as stale; do not prefer “whatever PNG is largest in the folder”.

## 9. Unity MCP `execute_code` limits

**Symptom:** Compilation failed — unexpected `using`, local functions, tuples,
Roslyn unavailable.

**Do this:**

- Assume **CodeDom** (C# 6-ish): no `using` at top (UnityEngine/UnityEditor in
  scope); no local functions; prefer `string[]` / simple loops over tuples.
- Prefer `compiler: codedom` unless Roslyn is known available.
- Keep TextureImporter + Material setup in small sequential scripts.

## Quick triage

| What you see                         | Check first                          |
| ------------------------------------ | ------------------------------------ |
| Black / tiny PNG                     | Linked mat? Bake FINISHED? Pixels?   |
| Smear strip on floor/wall in Unity   | Atlas UV overlaps                    |
| Wrong/missing maps on one mat only   | Shared vs location remapper          |
| Pink / flat after FBX                | Exported Image→Principled `M_*`?     |
| Gate always NEED_EXPORT              | Save/export mtime order              |
| Unity MCP C# will not compile        | CodeDom-safe script                  |

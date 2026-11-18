---
name: art-bake-orchestrator
description: >
  Orchestrates location-wide Blender→Unity art baking from a bake_manifest.yaml:
  UV fixes, tile/atlas bakes, M_* materials, and automatic FBX export when the
  Unity model is missing or forced. Use when the user asks to bake a room,
  location, or whole blend; run a bake manifest; or export Room.fbx after textures.
---

# Art bake orchestrator

Conductor for a whole location. Does **not** re-teach shader or UV details —
invoke the specialized skills and scripts below.

| Step              | Skill / script                                                               |
| ----------------- | ---------------------------------------------------------------------------- |
| Assembly gaps     | `blender-shader-assembly`                                                    |
| UV / grain        | `blender-uv-furniture`                                                       |
| Bake + Unity mats | `blender-unity-textures`                                                     |
| FBX gate          | `scripts/check_fbx_gate.py` → `blender-unity-textures/scripts/export_fbx.py` |
| Manifest check    | `scripts/validate_manifest.py`                                               |

Detail doc: [`docs/agents/blender-unity-textures.md`](../../../docs/agents/blender-unity-textures.md).
Schema + Home example: [reference.md](reference.md).

## When to use

- “Bake the room / Home / PollingStation art”
- Manifest-driven batch (tile + atlas mix)
- User finished geometry + assemblies; may or may not have exported FBX

**Not this skill:** hand-authoring node graphs from scratch; prefab/scene placement.

## Checklist (copy and track)

```text
Orchestrator progress:
- [ ] 1. Load / write bake_manifest.yaml; validate_manifest.py
- [ ] 2. Open source_blend in Blender (MCP connected)
- [ ] 3. Per material: UV ready (blender-uv-furniture if needed)
- [ ] 4. Per material: bake T_* + Unity M_* (blender-unity-textures)
- [ ] 5. FBX gate: check_fbx_gate.py → export_fbx.py if needed
- [ ] 6. Unity import; remapper names = M_*; spot-check console
- [ ] 7. Do not touch Prefabs/gameplay scripts unless asked
```

## FBX gate (mandatory)

After bakes, before treating the job as done:

1. Read `unity_fbx` and `export_fbx` (`auto` | `always` | `never`) from the manifest.
2. Run `check_fbx_gate.py` logic:
   - `never` → skip export
   - `always` → export
   - `auto` → export if file missing **or** `.blend` mtime newer than FBX
3. Export with `export_fbx.py`: materials on export set must be Image→Principled (`*Image` / `M_*` names). Never export procedural assemblies as the final slots.
4. Write under `Assets/Art/.../Models/`; let Unity create `.meta`; rely on `SharedMaterialRemapper` exact name match.

Home today: textures may exist while `Assets/Art/Interior/Home/Models/Room.fbx` does not — `auto` must export.

## Idempotency

- Re-running should overwrite the same `T_*` / `M_*` / FBX paths from the manifest
- Skip flat materials (no bake)
- Do not invent Shared materials without naming them `M_*` in the manifest
- Do not `git mv` inside `Assets/` after Unity import

## Order of MCP calls

```text
validate manifest
  → Blender: UV fixes (as needed)
  → Blender: bake_to_png per material
  → Unity: importer + M_* mats
  → check_fbx_gate → maybe export_fbx
  → Unity: refresh / confirm remaps
```

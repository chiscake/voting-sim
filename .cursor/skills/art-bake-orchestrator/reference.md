# bake_manifest schema

Paths are repo-relative with forward slashes. Manifest files are optional until a
location bake is requested; keep examples here, commit a real manifest next to
the location when the team starts batching that room.

## Fields

| Field            | Required | Description                                                       |
| ---------------- | -------- | ----------------------------------------------------------------- |
| `source_blend`   | yes      | Path under `Art/Source/…`                                         |
| `unity_fbx`      | yes      | Target under `Assets/Art/…/Models/*.fbx`                          |
| `export_fbx`     | no       | `auto` (default) \| `always` \| `never`                           |
| `export_objects` | no       | List of Blender object names; default = all mesh objects in scene |
| `materials`      | yes      | List of material bake entries                                     |

### Material entry

| Field       | Required   | Description                                        |
| ----------- | ---------- | -------------------------------------------------- |
| `assembly`  | yes        | Procedural material name in Blender                |
| `mode`      | yes        | `tile` \| `atlas` \| `flat`                        |
| `size_m`    | tile       | `[width, height]` meters                           |
| `density`   | tile       | px per meter                                       |
| `object`    | atlas      | Mesh object to bake                                |
| `texture`   | bake modes | Output `Assets/Art/…/Textures/T_*.png`             |
| `normal`    | no         | Optional `T_*_N.png`                               |
| `unity_mat` | yes        | `Assets/Art/…/M_*.mat` (Shared preferred)          |
| `roughness` | no         | Blender roughness for Smoothness = 1 − R           |
| `metallic`  | no         | Default 0                                          |
| `image_mat` | no         | Blender baked mat name (default `{assembly}Image`) |

## Home example

```yaml
source_blend: Art/Source/Interior/Home/Room.blend
unity_fbx: Assets/Art/Interior/Home/Models/Room.fbx
export_fbx: auto
export_objects:
  - Walls
  - Door
  - Chair
  - Table
  - Cabinet
  - Sofa
materials:
  - assembly: Walnut
    mode: tile
    size_m: [0.24, 0.96]
    density: 2133
    texture: Assets/Art/Interior/Home/Textures/T_Walnut.png
    unity_mat: Assets/Art/Shared/Materials/M_Walnut.mat
    roughness: 0.45
    metallic: 0
  - assembly: Wall
    mode: atlas
    object: Walls
    texture: Assets/Art/Interior/Home/Textures/T_Wall.png
    normal: Assets/Art/Interior/Home/Textures/T_Wall_N.png
    unity_mat: Assets/Art/Interior/Home/Materials/M_Wall.mat
    roughness: 0.7
  - assembly: Matte
    mode: flat
    unity_mat: Assets/Art/Shared/Materials/M_Matte.mat
    roughness: 0.6
    metallic: 0
```

## Validation rules (`validate_manifest.py`)

- `source_blend` and `unity_fbx` present
- `export_fbx` in `auto|always|never` if set
- Each material has `assembly`, `mode`, `unity_mat`
- `tile` requires `size_m` (len 2) and `density` and `texture`
- `atlas` requires `object` and `texture`
- `flat` must not require bake texture (texture optional/ignored)

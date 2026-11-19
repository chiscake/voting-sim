# Shader assembly templates

Node lookup must use `type`, not display names.

## Principled pair skeleton

```text
Assembly (procedural):
  [Noise/Wave/…] → ColorRamp / Mix → Principled BSDF → Material Output

Baked (*Image):
  Image Texture → Principled BSDF → Material Output
  (optional) Image Texture → Normal Map → Principled Normal
```

## Wood (assembly)

Typical chain:

1. `TEX_COORD` (Object) → `MAPPING` (scale along grain axis)
2. `TEX_NOISE` and/or `TEX_WAVE` → `VALTORGB` (ColorRamp) for rings/fibers
3. Mix into Principled Base Color; set Roughness ~0.35–0.55, Metallic 0
4. Optional Bump from noise into Normal (bake Normal separately if needed)

Grain direction: rotate Mapping or UV islands later (`blender-uv-furniture`), do not bake a wrong grain into a shared tile.

## Fabric

1. UV or Object coords → fine Noise / Musgrave for weave
2. ColorRamp for thread contrast; low Roughness variation
3. Prefer tile bake if the same cloth is shared (sofa + chair)

## Plaster / wall paint

1. Large-scale Noise for blotch + fine Noise for grit
2. Slight Roughness variation; Metallic 0
3. Unique architecture with cutouts → atlas bake on the mesh UV, not a library tile

## Flat (no bake)

Set Principled Base Color, Roughness, Metallic only. Unity: `Smoothness = 1 - Roughness`.

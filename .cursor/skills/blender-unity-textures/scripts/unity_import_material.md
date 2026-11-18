# Unity MCP checklist — texture + URP Lit

After PNG lands under `Assets/Art/.../Textures/`:

1. `refresh_unity` / wait for import.
2. Configure `TextureImporter` via Unity MCP `execute_code`:
   - Albedo tile: `sRGB = true`, `wrapMode = Repeat`, `maxTextureSize >=` image size, aniso 4–9 for floors
   - Albedo atlas: Wrap `Clamp`
   - Normal: `textureType = NormalMap`
3. Create or update material `Assets/Art/Shared/Materials/M_Name.mat` (or location path from manifest):
   - Shader: URP Lit
   - Base Map = albedo
   - Normal Map if present
   - `Smoothness = 1f - roughness`
   - Metallic from manifest
   - Tiling for tiles: `surface_size / tile_size` per axis when known
4. FBX under `Assets/Art/`: slot names must equal `M_*` material asset names for `SharedMaterialRemapper`.
5. Do not hand-edit `.meta`. Moves/renames only via `AssetDatabase` / Unity MCP.

Example rough C# for MCP `execute_code` (adapt paths):

```csharp
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

string texPath = "Assets/Art/Shared/Textures/T_PilotWoodTile.png";
var importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
importer.sRGBTexture = true;
importer.wrapMode = TextureWrapMode.Repeat;
importer.maxTextureSize = 2048;
importer.anisoLevel = 8;
importer.SaveAndReimport();

string matPath = "Assets/Art/Shared/Materials/M_PilotWoodTile.mat";
var shader = Shader.Find("Universal Render Pipeline/Lit");
var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
if (mat == null) {
    mat = new Material(shader);
    AssetDatabase.CreateAsset(mat, matPath);
}
mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
mat.SetFloat("_Smoothness", 0.55f);
mat.SetFloat("_Metallic", 0f);
EditorUtility.SetDirty(mat);
AssetDatabase.SaveAssets();
```

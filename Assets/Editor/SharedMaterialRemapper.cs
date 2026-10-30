using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// On Art model import, remaps material slots to assets in
/// <c>Assets/Art/Shared/Materials</c> when names match exactly (Blender ↔ Unity).
/// </summary>
public class SharedMaterialRemapper : AssetPostprocessor
{
    const string SharedMaterialsFolder = "Assets/Art/Shared/Materials";
    const string ArtRoot = "Assets/Art/";

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(ArtRoot, StringComparison.Ordinal))
            return;

        if (assetImporter is not ModelImporter importer)
            return;

        Dictionary<string, Material> shared = LoadSharedMaterialsByName();
        if (shared.Count == 0)
            return;

        Dictionary<AssetImporter.SourceAssetIdentifier, UnityEngine.Object> existing =
            importer.GetExternalObjectMap();

        foreach (KeyValuePair<string, Material> pair in shared)
        {
            var identifier = new AssetImporter.SourceAssetIdentifier(typeof(Material), pair.Key);
            if (existing.TryGetValue(identifier, out UnityEngine.Object mapped) && mapped == pair.Value)
                continue;

            importer.AddRemap(identifier, pair.Value);
        }
    }

    /// <summary>
    /// Legacy external-materials path: return a Shared material instead of creating one.
    /// </summary>
    Material OnAssignMaterialModel(Material material, Renderer renderer)
    {
        if (material == null)
            return null;

        if (!assetPath.StartsWith(ArtRoot, StringComparison.Ordinal))
            return null;

        Dictionary<string, Material> shared = LoadSharedMaterialsByName();
        return shared.TryGetValue(material.name, out Material replacement) ? replacement : null;
    }

    static Dictionary<string, Material> LoadSharedMaterialsByName()
    {
        var result = new Dictionary<string, Material>(StringComparer.Ordinal);
        if (!AssetDatabase.IsValidFolder(SharedMaterialsFolder))
            return result;

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { SharedMaterialsFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
                continue;

            result[mat.name] = mat;
        }

        return result;
    }
}

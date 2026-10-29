using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Builds a static Liberation Sans TMP Font Asset with Cyrillic glyphs baked in,
/// and wires it as the first fallback on the default LiberationSans SDF.
/// Menu: Voting Sim → Rebuild Cyrillic TMP Font Asset
/// Batch: -executeMethod TmpCyrillicFontBuilder.Build
/// </summary>
public static class TmpCyrillicFontBuilder
{
    const string SourceFontPath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";
    const string OutputPath =
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF Cyrillic.asset";
    const string MainFontPath =
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    const string DynamicFallbackPath =
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset";

    const int SamplingPointSize = 86;
    const int AtlasPadding = 9;
    const int AtlasWidth = 1024;
    const int AtlasHeight = 1024;

    [MenuItem("Voting Sim/Rebuild Cyrillic TMP Font Asset")]
    public static void Build()
    {
        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            Debug.LogError($"[TmpCyrillicFontBuilder] Source font not found: {SourceFontPath}");
            return;
        }

        if (TMP_Settings.instance == null)
        {
            Debug.LogError("[TmpCyrillicFontBuilder] TMP Settings missing. Import TMP Essential Resources.");
            return;
        }

        // Create dynamic, bake glyphs, then lock to Static so Editor use won't rewrite the atlas.
        var fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            SamplingPointSize,
            AtlasPadding,
            GlyphRenderMode.SDFAA,
            AtlasWidth,
            AtlasHeight,
            AtlasPopulationMode.Dynamic,
            enableMultiAtlasSupport: true);

        if (fontAsset == null)
        {
            Debug.LogError("[TmpCyrillicFontBuilder] CreateFontAsset failed.");
            return;
        }

        fontAsset.name = "LiberationSans SDF Cyrillic";

        uint[] unicodes = BuildCyrillicUnicodes();
        bool allAdded = fontAsset.TryAddCharacters(unicodes, out uint[] missing, includeFontFeatures: true);
        if (!allAdded && missing != null && missing.Length > 0)
        {
            Debug.LogWarning(
                $"[TmpCyrillicFontBuilder] {missing.Length} glyphs missing from Liberation Sans " +
                $"(first U+{missing[0]:X4}). Continuing with available characters.");
        }

        fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;

        if (AssetDatabase.LoadAssetAtPath<Object>(OutputPath) != null)
            AssetDatabase.DeleteAsset(OutputPath);

        AssetDatabase.CreateAsset(fontAsset, OutputPath);
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        foreach (Texture2D atlas in fontAsset.atlasTextures)
        {
            if (atlas != null)
                AssetDatabase.AddObjectToAsset(atlas, fontAsset);
        }

        WireAsPrimaryFallback(fontAsset);

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int charCount = fontAsset.characterTable != null ? fontAsset.characterTable.Count : 0;
        Debug.Log(
            $"[TmpCyrillicFontBuilder] Wrote {OutputPath} ({charCount} characters) " +
            "and set it as first fallback on LiberationSans SDF.");
    }

    static void WireAsPrimaryFallback(TMP_FontAsset cyrillic)
    {
        var main = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MainFontPath);
        if (main == null)
        {
            Debug.LogError($"[TmpCyrillicFontBuilder] Main font not found: {MainFontPath}");
            return;
        }

        var fallbacks = new List<TMP_FontAsset> { cyrillic };

        var dynamicFallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DynamicFallbackPath);
        if (dynamicFallback != null && dynamicFallback != cyrillic)
            fallbacks.Add(dynamicFallback);

        main.fallbackFontAssetTable = fallbacks;
        EditorUtility.SetDirty(main);
    }

    /// <summary>
    /// Cyrillic + Cyrillic Supplement (covers Russian, Belarusian, Ukrainian extras in this range).
    /// </summary>
    static uint[] BuildCyrillicUnicodes()
    {
        var list = new List<uint>(0x200);
        for (uint c = 0x0400; c <= 0x04FF; c++)
            list.Add(c);
        for (uint c = 0x0500; c <= 0x052F; c++)
            list.Add(c);
        return list.ToArray();
    }
}

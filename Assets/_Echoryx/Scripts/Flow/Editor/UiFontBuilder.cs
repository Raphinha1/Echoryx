using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Echoryx.Flow.Editor
{
    /// <summary>
    /// Creates static TMP font assets (title: Orbitron, text: Exo 2) from Assets/_Echoryx/Fonts, plus the title
    /// glow material. Static atlases keep Play Mode from modifying the assets. Existing assets are kept.
    /// </summary>
    public static class UiFontBuilder
    {
        private const string Sources = "Assets/_Echoryx/Fonts/";
        private const string Output = "Assets/_Echoryx/Resources/Fonts/";

        [MenuItem("Echoryx/Fonts/Create TMP Fonts")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Echoryx/Resources/Fonts"))
                AssetDatabase.CreateFolder("Assets/_Echoryx/Resources", "Fonts");
            var title = Create("Orbitron-VariableFont_wght.ttf", "TitleFont");
            Create("Exo2-VariableFont_wght.ttf", "BodyFont");
            CreateGlow(title);
            AssetDatabase.SaveAssets();
            Debug.Log("FONTS ready.");
        }

        private static TMP_FontAsset Create(string file, string name)
        {
            var path = Output + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;
            var source = AssetDatabase.LoadAssetAtPath<Font>(Sources + file)
                ?? throw new InvalidOperationException("Missing font: " + Sources + file);
            var asset = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, false)
                ?? throw new InvalidOperationException("Could not load " + file);
            asset.name = name;
            // Printable ASCII, Latin-1 (Spanish accents, inverted marks, middle dot) and common punctuation.
            var characters = new string(Enumerable.Range(32, 95).Concat(Enumerable.Range(160, 96))
                .Concat(new[] { 0x2013, 0x2014, 0x2018, 0x2019, 0x201C, 0x201D, 0x2022, 0x2026 })
                .Select(code => (char)code).ToArray());
            asset.TryAddCharacters(characters, out var missing);
            if (!string.IsNullOrEmpty(missing)) Debug.Log("FONTS " + name + " lacks " + missing.Length + " characters");
            asset.atlasPopulationMode = AtlasPopulationMode.Static;
            var fallback = TMP_Settings.defaultFontAsset;
            if (fallback != null) asset.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };

            AssetDatabase.CreateAsset(asset, path);
            asset.atlasTexture.name = name + " Atlas";
            asset.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("FONTS " + name + ": " + asset.characterTable.Count + " characters, " + asset.atlasTextures.Length + " atlas");
            return asset;
        }

        // The mobile SDF shader has no glow, so the title uses the full distance-field shader.
        private static void CreateGlow(TMP_FontAsset font)
        {
            const string path = Output + "TitleGlow.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            var material = new Material(font.material)
            {
                shader = Shader.Find("TextMeshPro/Distance Field") ?? throw new InvalidOperationException("TMP SDF shader missing.")
            };
            material.EnableKeyword("GLOW_ON");
            material.SetColor(ShaderUtilities.ID_GlowColor, new Color(0.76f, 0.52f, 1f, 0.8f));
            material.SetFloat(ShaderUtilities.ID_GlowOffset, 0f);
            material.SetFloat(ShaderUtilities.ID_GlowInner, 0.08f);
            material.SetFloat(ShaderUtilities.ID_GlowOuter, 0.55f);
            material.SetFloat(ShaderUtilities.ID_GlowPower, 0.4f);
            AssetDatabase.CreateAsset(material, path);
        }
    }
}

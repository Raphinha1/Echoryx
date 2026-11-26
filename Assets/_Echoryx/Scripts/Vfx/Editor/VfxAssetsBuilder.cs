using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echoryx.Vfx.Editor
{
    /// <summary>Creates the particle materials (URP Particles/Unlit), the ring texture and the VfxLibrary.</summary>
    public static class VfxAssetsBuilder
    {
        private const string Folder = "Assets/_Echoryx/Resources/Vfx";
        private const string Glow = "Assets/_Echoryx/Art/Effects/BeaconGlow.png"; // own radial falloff
        private const string Ring = "Assets/_Echoryx/Art/Effects/VfxRing.png";

        [MenuItem("Echoryx/VFX/Create Materials")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Echoryx/Resources", "Vfx");
            var glowTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Glow) ?? throw new InvalidOperationException("Missing " + Glow);
            var library = AssetDatabase.LoadAssetAtPath<VfxLibrary>(Folder + "/VfxLibrary.asset");
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<VfxLibrary>();
                AssetDatabase.CreateAsset(library, Folder + "/VfxLibrary.asset");
            }
            library.glow = Material("VfxGlow", glowTexture, additive: true);
            library.dust = Material("VfxDust", glowTexture, additive: false);
            library.ring = Material("VfxRing", RingTexture(), additive: true);
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log("VFX assets ready.");
        }

        private static Material Material(string name, Texture2D texture, bool additive)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? throw new InvalidOperationException("URP Particles/Unlit unavailable.");
            var path = Folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", additive ? 2f : 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D RingTexture()
        {
            if (!File.Exists(Ring))
            {
                const int size = 128;
                var generated = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (var y = 0; y < size; y++)
                    for (var x = 0; x < size; x++)
                    {
                        var d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), Vector2.one * (size * 0.5f)) / (size * 0.5f);
                        var a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.14f);
                        generated.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                    }
                File.WriteAllBytes(Ring, generated.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(generated);
                AssetDatabase.ImportAsset(Ring);
                var importer = (TextureImporter)AssetImporter.GetAtPath(Ring);
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(Ring);
        }
    }
}

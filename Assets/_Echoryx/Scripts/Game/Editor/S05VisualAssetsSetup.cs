using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echoryx.Game.Editor
{
    /// <summary>Local Standard assets only. Never opens or changes gameplay scenes.</summary>
    public static class S05VisualAssetsSetup
    {
        private const string Root = "Assets/_Echoryx/Art/Environment/Quaternius/ModularSciFiMegaKitStandard";
        private const string Materials = "Assets/_Echoryx/Materials/Environment";
        private const string ScenePath = "Assets/_Echoryx/Scenes/VisualAssetLab.unity";
        private static readonly string[] Models =
        {
            "Platforms/Platform_Metal", "Platforms/Platform_Ramp_2",
            "Walls/WallAstra_Straight_Flat", "Walls/WallAstra_Corner_Square_Inner",
            "Walls/WallAstra_Corner_Square_Outer", "Columns/Column_Simple",
            "Platforms/Door_DarkMetal", "Platforms/Door_Frame_Square",
            "Walls/BottomSimple_Straight", "Walls/TopSimple_Straight"
        };

        [MenuItem("Echoryx/S05-A1/Prepare Visual Assets")]
        public static void Prepare()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EnsureFolder(Materials);
            ConfigureTextures();
            var palette = new[]
            {
                MakeMaterial("Graphite", "#252C38", 0.45f, 0.35f),
                MakeMaterial("Titanium", "#A7B2C2", 0.65f, 0.45f),
                MakeMaterial("EchoCyan", "#29DCEB", 0.15f, 0.4f, true),
                MakeMaterial("RewindViolet", "#995CF2", 0.15f, 0.4f, true)
            };
            var trim01 = MakeMaterial("Trim01Titanium", "#A7B2C2", 0.65f, 0.4f);
            var trim02 = MakeMaterial("Trim02Graphite", "#697586", 0.45f, 0.3f);
            var trim03 = MakeMaterial("Trim03Titanium", "#A7B2C2", 0.55f, 0.35f);
            var cables = MakeMaterial("Trim03GraphiteCables", "#697586", 0.4f, 0.3f);
            var decal = MakeMaterial("Decals", "#FFFFFF", 0f, 0.2f);
            TextureMaps(trim01, "T_Trim_01_BaseColor", "T_Trim_01_Normal");
            TextureMaps(trim02, "T_Trim_02_BaseColor", "T_Trim_02_Normal");
            TextureMaps(trim03, "T_Trim_03_BaseColor", "T_Trim_03_Normal");
            TextureMaps(cables, "T_Trim_03_Cables", "T_Trim_03_Normal");
            TextureMaps(decal, "T_Decals", null);
            decal.SetFloat("_AlphaClip", 1f);
            decal.SetFloat("_Cutoff", 0.35f);
            decal.EnableKeyword("_ALPHATEST_ON");
            decal.SetOverrideTag("RenderType", "TransparentCutout");
            decal.renderQueue = (int)RenderQueue.AlphaTest;
            EditorUtility.SetDirty(decal);
            var remaps = new Dictionary<string, Material>
            {
                { "MI_Trim_01", trim01 }, { "MI_Trim_01_Floor", trim01 },
                { "MI_Trim_02", trim02 }, { "MI_Trim_03", trim03 },
                { "MI_Trim_03_Dark", cables }, { "MI_Decal_Red", decal },
                { "MI_Decal_Grey", decal }, { "M_Decal_White", decal }
            };
            foreach (var name in Models)
            {
                var path = Root + "/Models/" + name + ".fbx";
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) throw new InvalidOperationException("Missing selected FBX: " + path);
                importer.importAnimation = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.addCollider = false;
                importer.globalScale = 1f;
                importer.useFileScale = true;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                // Embedded source material names remain available even after remapping/reimport.
                var sources = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().Select(m => m.name)
                    .Concat(importer.GetExternalObjectMap().Keys.Where(k => k.type == typeof(Material)).Select(k => k.name))
                    .Distinct().ToArray();
                if (sources.Length == 0) throw new InvalidOperationException("No source material slots: " + path);
                foreach (var source in sources)
                {
                    if (!remaps.TryGetValue(source, out var material))
                        throw new InvalidOperationException("Unmapped source material: " + source + " in " + path);
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), source), material);
                }
                importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) CreateLab(palette);
            Validate();
        }

        // Explicit rebuild entry point for this generated display scene only.
        // Prepare itself preserves an existing lab, including manual presentation changes.
        public static void RebuildLab()
        {
            CreateLab(new[] { "Graphite", "Titanium", "EchoCyan", "RewindViolet" }
                .Select(n => AssetDatabase.LoadAssetAtPath<Material>(Materials + "/" + n + ".mat")).ToArray());
        }

        private static void ConfigureTextures()
        {
            foreach (var path in Directory.GetFiles(Root + "/Textures", "*.png").OrderBy(p => p))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                var normal = path.EndsWith("_Normal.png", StringComparison.Ordinal);
                importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = !normal;
                importer.alphaSource = normal ? TextureImporterAlphaSource.None : TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = path.EndsWith("T_Decals.png", StringComparison.Ordinal);
                importer.maxTextureSize = 2048;
                importer.mipmapEnabled = true;
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
        }

        private static Material MakeMaterial(string name, string hex, float metallic, float smoothness, bool emissive = false)
        {
            var path = Materials + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit is unavailable.");
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            ColorUtility.TryParseHtmlString(hex, out var color);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 0.6f);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void TextureMaps(Material material, string albedo, string normal)
        {
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/" + albedo + ".png"));
            if (normal != null)
            {
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/" + normal + ".png"));
                material.SetFloat("_BumpScale", 1f);
                material.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(material);
        }

        private static void CreateLab(Material[] palette)
        {
            if (palette.Any(m => m == null)) throw new InvalidOperationException("Prepare the visual materials before building the lab.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var samples = new GameObject("Standard Asset Samples").transform;
            for (var i = 0; i < Models.Length; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Models/" + Models[i] + ".fbx");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
                instance.transform.SetParent(samples, true);
                // These surfaces are intentionally one-sided: show their front, not the back.
                // This rotates gallery instances only; source axes and pivots stay intact.
                if (Models[i] == "Walls/WallAstra_Straight_Flat"
                    || Models[i] == "Walls/WallAstra_Corner_Square_Inner"
                    || Models[i] == "Walls/TopSimple_Straight")
                    instance.transform.Rotate(Vector3.up, 180f, Space.World);
                var bounds = BoundsOf(instance);
                instance.transform.position += new Vector3((i % 4) * 8f - bounds.center.x,
                    -bounds.min.y, (i / 4) * 8f - bounds.center.z);
                var label = new GameObject("Label " + instance.name).AddComponent<TextMesh>();
                label.transform.SetParent(samples, false);
                label.transform.position = new Vector3((i % 4) * 8f, 0.03f, (i / 4) * 8f - 3f);
                label.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                label.text = instance.name;
                label.anchor = TextAnchor.MiddleCenter;
                label.characterSize = 0.1f;
                label.fontSize = 48;
                label.color = Color.white;
            }
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Display ground (no gameplay collider)";
            floor.transform.position = new Vector3(12f, -0.25f, 8f);
            floor.transform.localScale = new Vector3(35f, 0.5f, 29f);
            UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
            floor.GetComponent<Renderer>().sharedMaterial = palette[0];
            for (var i = 0; i < palette.Length; i++)
            {
                var swatch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                swatch.name = "Palette " + palette[i].name;
                swatch.transform.position = new Vector3(3f + i * 3f, 0.5f, -6f);
                swatch.GetComponent<Renderer>().sharedMaterial = palette[i];
                UnityEngine.Object.DestroyImmediate(swatch.GetComponent<Collider>());
            }
            var reference = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            reference.name = "Two metre scale reference (not Player)";
            reference.transform.position = new Vector3(20f, 1f, -6f);
            reference.GetComponent<Renderer>().sharedMaterial = palette[1];
            UnityEngine.Object.DestroyImmediate(reference.GetComponent<Collider>());
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2f;
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            var fill = new GameObject("Fill Light").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.7f;
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(35f, 135f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.4f, 0.48f);
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 22f;
            camera.transform.position = new Vector3(37f, 36f, -28f);
            camera.transform.LookAt(new Vector3(12f, 0f, 7f));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.07f);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Cannot save visual lab.");
        }

        [MenuItem("Echoryx/S05-A1/Validate Visual Assets")]
        public static void Validate()
        {
            foreach (var path in Directory.GetFiles(Root + "/Textures", "*.png"))
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                var normal = path.EndsWith("_Normal.png", StringComparison.Ordinal);
                if (texture == null || importer.sRGBTexture == normal
                    || importer.textureType != (normal ? TextureImporterType.NormalMap : TextureImporterType.Default))
                    throw new InvalidOperationException("Unexpected texture import: " + path);
            }
            foreach (var name in new[] { "Trim01Titanium", "Trim02Graphite", "Trim03Titanium", "Trim03GraphiteCables", "Decals" })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(Materials + "/" + name + ".mat");
                if (material == null || material.GetTexture("_BaseMap") == null
                    || (name != "Decals" && material.GetTexture("_BumpMap") == null))
                    throw new InvalidOperationException("Missing trim/atlas texture: " + name);
            }
            var metrics = new List<ModelMetric>();
            foreach (var name in Models)
            {
                var path = Root + "/Models/" + name + ".fbx";
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null) throw new InvalidOperationException("Model did not import: " + path);
                var meshes = asset.GetComponentsInChildren<MeshFilter>(true).Select(f => f.sharedMesh).ToArray();
                if (meshes.Length == 0 || meshes.Any(m => m == null || m.vertexCount == 0))
                    throw new InvalidOperationException("Empty geometry: " + path);
                var slots = asset.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).ToArray();
                if (slots.Length == 0 || slots.Any(m => m == null || m.shader == null
                    || m.shader.name != "Universal Render Pipeline/Lit" || !AssetDatabase.GetAssetPath(m).StartsWith(Materials + "/")))
                    throw new InvalidOperationException("Material is missing or not explicitly remapped to URP: " + path);
                if (asset.GetComponentsInChildren<Collider>(true).Length != 0
                    || asset.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                    throw new InvalidOperationException("Visual models must not contain gameplay components: " + path);
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                if (importer.importAnimation || importer.globalScale != 1f || !importer.useFileScale)
                    throw new InvalidOperationException("Unexpected model import settings: " + path);
                var bounds = BoundsOf(asset);
                metrics.Add(new ModelMetric
                {
                    path = path, size = bounds.size, center = bounds.center,
                    rootScale = asset.transform.localScale, rootRotation = asset.transform.localEulerAngles,
                    fileScale = importer.fileScale,
                    vertices = meshes.Sum(m => m.vertexCount),
                    triangles = meshes.Sum(m => Enumerable.Range(0, m.subMeshCount).Sum(s => (int)(m.GetIndexCount(s) / 3))),
                    materialNames = slots.Select(m => m.name).Distinct().OrderBy(n => n).ToArray()
                });
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new InvalidOperationException("Visual lab is missing.");
            File.WriteAllText("docs/visual-assets-import-report.json", JsonUtility.ToJson(new ImportReport
            {
                unityVersion = Application.unityVersion, models = metrics.ToArray(),
                renderPipeline = GraphicsSettings.defaultRenderPipeline != null ? GraphicsSettings.defaultRenderPipeline.name : "NONE"
            }, true) + "\n");
            Debug.Log("S05-A1 validated: 10 FBX models; all material slots mapped to URP Lit; no gameplay components. Metrics in docs/visual-assets-import-report.json.");
        }

        [MenuItem("Echoryx/S05-A1/Render Visual Lab Preview")]
        public static void RenderPreview()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                throw new InvalidOperationException("Preview needs a graphics device; run Unity without -nographics.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var path in Directory.GetFiles(Materials, "*.mat"))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || !material.shader.isSupported)
                    throw new InvalidOperationException("Shader unsupported on this graphics device: " + path);
            }
            var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            var target = new RenderTexture(1600, 1000, 24);
            var pixels = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                target.Create();
                var request = new RenderPipeline.StandardRequest { destination = target };
                if (!RenderPipeline.SupportsRenderRequest(camera, request)) throw new InvalidOperationException("Pipeline cannot render the preview request.");
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0);
                pixels.Apply();
                Directory.CreateDirectory("docs/images");
                File.WriteAllBytes("docs/images/s05-a1-visual-lab.png", pixels.EncodeToPNG());
                Debug.Log("S05-A1 preview rendered with " + SystemInfo.graphicsDeviceName + "; saved in docs/images.");
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(pixels);
            }
        }

        private static Bounds BoundsOf(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("No renderers: " + model.name);
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }

        [Serializable] private sealed class ModelMetric
        {
            public string path;
            public Vector3 size, center, rootScale, rootRotation;
            public float fileScale;
            public int vertices, triangles;
            public string[] materialNames;
        }
        [Serializable] private sealed class ImportReport
        {
            public string unityVersion, renderPipeline;
            public ModelMetric[] models;
        }
    }
}

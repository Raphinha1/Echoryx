using System;
using System.IO;
using System.Linq;
using Echoryx.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echoryx.Game.Editor
{
    /// <summary>Isolated Humanoid import/animation check; never opens gameplay or earlier character scenes.</summary>
    public static class S05SuperSoldierValidation
    {
        private const string Root = "Assets/_Echoryx/Art/Characters/SihaGamesSuperSoldier";
        private const string Model = Root + "/Idle.fbx";
        private const string Mats = "Assets/_Echoryx/Materials/Characters/SihaGamesSuperSoldier";
        private const string ScenePath = "Assets/_Echoryx/Scenes/SuperSoldierValidationLab.unity";
        private const string ControllerPath = "Assets/_Echoryx/Art/Characters/SihaGamesSuperSoldier/SuperSoldierPreview.controller";
        private static readonly string[] SourceMaterials =
        {
            "Material.002", "Ch44_Body", "Ch44_body1", "Material.005", "Material.001", "Material.003", "Material.004"
        };

        [MenuItem("Echoryx/S05-B2/Import and Validate SuperSoldier")]
        public static void ImportAndValidate()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EnsureFolder(Mats);
            ConfigureTextures();
            var importer = AssetImporter.GetAtPath(Model) as ModelImporter;
            Require(importer != null, "Candidate FBX is missing.");
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.isReadable = false;
            foreach (var source in SourceMaterials)
            {
                var material = CreateUrpMaterial(source);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), source), material);
            }
            importer.SaveAndReimport();
            AssetDatabase.SaveAssets();
            var result = Inspect();
            File.WriteAllText("docs/supersoldier-import-report.json", JsonUtility.ToJson(result, true) + "\n");
            if (result.avatarValid && result.avatarHuman && result.clips.Length > 0)
            {
                CreatePreviewController(result.clips[0].name);
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) BuildLab(result.avatarName);
                OrientPreviewToCamera();
                ValidateLab();
            }
            Debug.Log("S05-B2: " + result.vertices + " vertices, " + result.triangles + " triangles, "
                + result.boneCount + " bones, Humanoid=" + result.avatarValid + "/" + result.avatarHuman
                + ", clips=" + result.clips.Length + ".");
        }

        [MenuItem("Echoryx/S05-B2/Validate SuperSoldier")]
        public static void Validate() => WriteReport(Inspect());

        private static ImportReport Inspect()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Model);
            Require(asset != null, "Candidate did not import as a model.");
            var subassets = AssetDatabase.LoadAllAssetsAtPath(Model);
            var meshes = asset.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r => r.sharedMesh).Where(m => m != null).Distinct().ToArray();
            var avatars = subassets.OfType<Avatar>().ToArray();
            var clips = subassets.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            var mats = asset.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct().ToArray();
            return new ImportReport
            {
                unity = Application.unityVersion,
                modelPath = Model,
                importerAnimationType = ((ModelImporter)AssetImporter.GetAtPath(Model)).animationType.ToString(),
                globalScale = ((ModelImporter)AssetImporter.GetAtPath(Model)).globalScale,
                fileScale = ((ModelImporter)AssetImporter.GetAtPath(Model)).fileScale,
                bounds = BoundsOf(asset).size,
                meshCount = meshes.Length,
                skinnedRendererCount = asset.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length,
                materialSlotCount = asset.GetComponentsInChildren<Renderer>(true).Sum(r => r.sharedMaterials.Length),
                vertices = meshes.Sum(m => m.vertexCount),
                triangles = meshes.Sum(m => Enumerable.Range(0, m.subMeshCount).Sum(i => (long)m.GetIndexCount(i) / 3)),
                boneCount = asset.GetComponentsInChildren<SkinnedMeshRenderer>(true).SelectMany(r => r.bones).Where(b => b != null).Distinct().Count(),
                bindPoseCount = meshes.Sum(m => m.bindposeCount),
                avatarCount = avatars.Length,
                avatarName = avatars.FirstOrDefault(a => a.isValid && a.isHuman)?.name,
                avatarValid = avatars.Any(a => a.isValid),
                avatarHuman = avatars.Any(a => a.isHuman && a.isValid),
                clips = clips.Select(c => new ClipReport { name = c.name, lengthSeconds = c.length, frameRate = c.frameRate, legacy = c.legacy }).ToArray(),
                materialCount = mats.Length,
                materials = mats.Select(m => m.name + " (" + (m.shader == null ? "missing" : m.shader.name) + ")").ToArray(),
                textures = new[] { "file2", "file4", "file8", "file10" }.Select(n =>
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/" + n + ".png");
                    var ti = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex));
                    return new TextureReport { name = n, width = tex.width, height = tex.height,
                        format = tex.format.ToString(), normal = ti.textureType == TextureImporterType.NormalMap, sRGB = ti.sRGBTexture };
                }).ToArray()
            };
        }

        private static Material CreateUrpMaterial(string sourceName)
        {
            var safe = sourceName.Replace('.', '_');
            var path = Mats + "/" + safe + ".mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result == null)
            {
                var source = AssetDatabase.LoadAllAssetsAtPath(Model).OfType<Material>().FirstOrDefault(m => m.name == sourceName);
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                Require(shader != null, "URP Lit is unavailable.");
                result = new Material(shader) { name = safe };
                if (source != null && source.HasProperty("_Color")) result.SetColor("_BaseColor", source.GetColor("_Color"));
                var baseName = sourceName == "Ch44_Body" ? "file2" : sourceName == "Ch44_body1" ? "file8" : null;
                var normalName = sourceName == "Ch44_Body" ? "file4" : sourceName == "Ch44_body1" ? "file10" : null;
                if (baseName != null)
                    result.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/" + baseName + ".png"));
                if (normalName != null)
                {
                    result.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/" + normalName + ".png"));
                    result.EnableKeyword("_NORMALMAP");
                }
                if (source != null && source.HasProperty("_Metallic")) result.SetFloat("_Metallic", source.GetFloat("_Metallic"));
                if (source != null && source.HasProperty("_Glossiness")) result.SetFloat("_Smoothness", source.GetFloat("_Glossiness"));
                AssetDatabase.CreateAsset(result, path);
            }
            return result;
        }

        private static void ConfigureTextures()
        {
            foreach (var name in new[] { "file2", "file4", "file8", "file10" })
            {
                var path = Root + "/Textures/" + name + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Require(importer != null, "Missing candidate texture " + name);
                var normal = name == "file4" || name == "file10";
                importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = !normal;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = false;
                importer.maxTextureSize = 2048;
                importer.mipmapEnabled = true;
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
        }

        private static void CreatePreviewController(string clipName)
        {
            var previous = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (previous != null && previous.layers[0].stateMachine.states.Length > 0) return;
            if (previous != null) AssetDatabase.DeleteAsset(ControllerPath);
            var clips = AssetDatabase.LoadAllAssetsAtPath(Model).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            var clip = clips.FirstOrDefault(c => c.name == clipName);
            Require(clip != null, "Preview animation clip is missing.");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var state = controller.layers[0].stateMachine.AddState("IdlePreview");
            state.motion = clip;
            state.writeDefaultValues = true;
        }

        private static void BuildLab(string avatarName)
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model), scene);
            model.name = "SuperSoldier Animated Preview";
            var bounds = BoundsOf(model);
            var scale = 2f / bounds.size.y;
            model.transform.localScale = Vector3.one * scale;
            bounds = BoundsOf(model);
            model.transform.position += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            // The source faces away from the preview camera; keep source FBX axes untouched.
            model.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.avatar = AssetDatabase.LoadAllAssetsAtPath(Model).OfType<Avatar>().First(a => a.name == avatarName && a.isValid && a.isHuman);
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Two metre height reference (not Player)";
            capsule.transform.position = new Vector3(2f, 1f, 0f);
            capsule.GetComponent<Renderer>().sharedMaterial = Palette("Titanium");
            UnityEngine.Object.DestroyImmediate(capsule.GetComponent<Collider>());
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Neutral graphite floor";
            floor.transform.position = new Vector3(0f, -0.15f, 0f);
            floor.transform.localScale = new Vector3(6f, 0.3f, 5f);
            floor.GetComponent<Renderer>().sharedMaterial = Palette("Graphite");
            UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
            var fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fill.name = "Cyan and violet palette markers";
            fill.transform.position = new Vector3(-2.8f, 0.025f, 0f);
            fill.transform.localScale = new Vector3(0.06f, 0.05f, 1.5f);
            fill.GetComponent<Renderer>().sharedMaterial = Palette("EchoCyan");
            UnityEngine.Object.DestroyImmediate(fill.GetComponent<Collider>());
            Light("Neutral key", Quaternion.Euler(45f, -30f, 0f), 1.8f);
            Light("Soft fill", Quaternion.Euler(30f, 140f, 0f), 0.65f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.38f, 0.43f, 0.51f);
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.fieldOfView = 40f;
            camera.transform.position = new Vector3(0f, 2.4f, -6.5f);
            camera.transform.LookAt(new Vector3(0f, 1f, 0f));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.035f, 0.055f);
            Require(EditorSceneManager.SaveScene(scene, ScenePath), "Could not save validation scene.");
        }

        private static void OrientPreviewToCamera()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var model = GameObject.Find("SuperSoldier Animated Preview");
            Require(model != null, "Preview character is missing.");
            model.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            Require(EditorSceneManager.SaveScene(scene), "Could not save preview orientation.");
        }

        private static void Light(string name, Quaternion rotation, float intensity)
        {
            var light = new GameObject(name).AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = rotation;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }

        private static Material Palette(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/_Echoryx/Materials/Environment/" + name + ".mat");
        private static Bounds BoundsOf(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            Require(renderers.Length > 0, "Candidate has no renderers.");
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

        [MenuItem("Echoryx/S05-B2/Validate Preview Scene")]
        public static void ValidateLab()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var model = GameObject.Find("SuperSoldier Animated Preview");
            var animator = model.GetComponent<Animator>();
            Require(animator.avatar != null && animator.avatar.isValid && animator.avatar.isHuman, "Invalid Humanoid avatar in preview.");
            Require(animator.runtimeAnimatorController != null, "No preview animation controller.");
            Require(Mathf.Abs(BoundsOf(model).size.y - 2f) < 0.02f, "Preview model must be approximately two metres tall.");
            Require(model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0, "No skinned renderers in preview.");
            Require(UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Length == 0, "Preview scene must remain outside gameplay physics.");
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            Require(renderers.SelectMany(r => r.sharedMaterials).All(m => m != null && m.shader.name == "Universal Render Pipeline/Lit"), "Non-URP character material.");
            Debug.Log("S05-B2 preview valid: Avatar Humanoid, default animation controller, 2m scale, no gameplay colliders.");
        }

        [MenuItem("Echoryx/S05-B2/Render Preview")]
        public static void RenderPreview()
        {
            Require(SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null, "Graphics device required.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            var target = new RenderTexture(1600, 1000, 24);
            var pixels = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                target.Create();
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0);
                pixels.Apply();
                Directory.CreateDirectory("docs/images");
                File.WriteAllBytes("docs/images/s05-b2-supersoldier.png", pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(pixels);
            }
            Debug.Log("S05-B2 rendered on " + SystemInfo.graphicsDeviceName);
        }

        [Serializable] private sealed class ImportReport
        {
            public string unity, modelPath, importerAnimationType, avatarName;
            public float globalScale, fileScale; public Vector3 bounds;
            public int meshCount, skinnedRendererCount, materialSlotCount, boneCount, bindPoseCount, avatarCount, materialCount;
            public long vertices, triangles; public bool avatarValid, avatarHuman;
            public string[] materials; public ClipReport[] clips; public TextureReport[] textures;
        }
        [Serializable] private sealed class ClipReport { public string name; public float lengthSeconds, frameRate; public bool legacy; }
        [Serializable] private sealed class TextureReport { public string name, format; public int width, height; public bool normal, sRGB; }
        private static void WriteReport(ImportReport report) => File.WriteAllText("docs/supersoldier-import-report.json", JsonUtility.ToJson(report, true) + "\n");
    }
}

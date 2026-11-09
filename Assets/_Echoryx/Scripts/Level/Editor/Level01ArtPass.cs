using System;
using Echoryx.Interactions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Echoryx.Level.Editor
{
    /// <summary>
    /// Visual-only pass over Level_01_Tutorial: Kenney kit pieces, URP materials, sky, fog and post.
    /// Colliders, scripts and gameplay dimensions are untouched; gameplay renderers are only disabled.
    /// Re-running is safe: the art root is rebuilt and prefab visuals are replaced.
    /// </summary>
    public static class Level01ArtPass
    {
        private const string Modular = "Assets/_Echoryx/Art/Environment/kenney_modular-space-kit_1.0/Models/FBX format/";
        private const string Station = "Assets/_Echoryx/Art/Environment/kenney_space-station-kit/Models/FBX format/";
        private const string MaterialFolder = "Assets/_Echoryx/Materials/Art";
        private const string ProfilePath = "Assets/_Echoryx/Settings/Level01VolumeProfile.asset";
        private const string SkyShaderName = "Echoryx/Gradient Skybox";
        private const string ArtRoot = "Level Art";
        private const string VisualName = "Art";

        private sealed class Mats
        {
            public Material Wall, Floor, Door, Hull, Accent, Platform, Flat, Hazard, GoalPad, GoalField, Sky;
        }

        [MenuItem("Echoryx/Level 01/Apply Art Pass")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/_Echoryx/Materials", "Art");
            var m = BuildMaterials();
            UpdatePrefabs(m);
            UpdateScene(m);
            AssetDatabase.SaveAssets();
            Debug.Log("Level 01 art pass applied.");
        }

        // ------------------------------------------------------------ materials

        private static Mats BuildMaterials()
        {
            var modMap = AssetDatabase.LoadAssetAtPath<Texture2D>(Modular + "Textures/colormap.png");
            var staMap = AssetDatabase.LoadAssetAtPath<Texture2D>(Station + "Textures/colormap.png");
            if (modMap == null || staMap == null) throw new InvalidOperationException("Kenney colormap textures not found.");
            var hologram = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Echoryx/Materials/EchoHologram.mat");
            return new Mats
            {
                Wall = Lit("ArtWall", modMap, new Color(0.82f, 0.74f, 1f)),
                Floor = Lit("ArtFloor", modMap, new Color(0.58f, 0.64f, 0.86f)),
                Door = Lit("ArtDoor", modMap, new Color(1f, 0.80f, 0.58f)),
                Hull = Lit("ArtHull", staMap, new Color(0.50f, 0.56f, 0.80f)),
                Accent = Lit("ArtAccent", staMap, new Color(1f, 0.97f, 1f)),
                Platform = Lit("ArtPlatform", modMap, new Color(0.9f, 0.82f, 1f), emission: true),
                Flat = Lit("ArtFlat", null, Color.white),
                GoalPad = Lit("ArtGoalPad", null, new Color(0.25f, 0.9f, 0.4f), emission: true, emissionColor: new Color(0.2f, 1.2f, 0.4f)),
                Hazard = Transparent("ArtHazard", hologram, new Color(1f, 0.28f, 0.12f, 0.5f), new Color(2.2f, 0.35f, 0.1f)),
                GoalField = Transparent("ArtGoalField", hologram, new Color(0.3f, 1f, 0.5f, 0.3f), new Color(0.3f, 1.4f, 0.5f)),
                Sky = Sky(),
            };
        }

        private static Material Lit(string name, Texture map, Color tint, bool emission = false, Color? emissionColor = null)
        {
            var path = MaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")
                    ?? throw new InvalidOperationException("URP Lit unavailable.")) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = Shader.Find("Universal Render Pipeline/Lit");
            material.SetTexture("_BaseMap", map);
            material.SetColor("_BaseColor", tint);
            // Matte cartoon look: low smoothness, no metal, no specular/reflections.
            material.SetFloat("_Smoothness", 0.08f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_SpecularHighlights", 0f);
            material.SetFloat("_EnvironmentReflections", 0f);
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            if (emission)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColor ?? Color.black);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material Transparent(string name, Material template, Color color, Color emission)
        {
            var path = MaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                // Clone the project's working transparent URP material instead of re-deriving blend setup.
                material = new Material(template) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material Sky()
        {
            var path = MaterialFolder + "/ArtSky.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(SkyShaderName)
                    ?? throw new InvalidOperationException("Skybox shader missing.")) { name = "ArtSky" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = Shader.Find(SkyShaderName);
            material.SetColor("_TopColor", new Color(0.05f, 0.04f, 0.20f));
            material.SetColor("_HorizonColor", new Color(0.50f, 0.27f, 0.64f));
            material.SetColor("_BottomColor", new Color(0.08f, 0.06f, 0.22f));
            EditorUtility.SetDirty(material);
            return material;
        }

        // ------------------------------------------------------------ prefabs

        private static void UpdatePrefabs(Mats m)
        {
            Edit("PressurePlate", root =>
            {
                var oldSurface = root.transform.Find("Plate Surface");
                Hide(oldSurface);
                ClearArt(root.transform);
                var art = NewChild(root.transform, VisualName).transform;
                Spawn(Modular, "template-floor-layer", art, "Plate Bezel", new Vector3(0f, -0.1f, 0f),
                    Vector3.zero, new Vector3(0.62f, 0.32f, 0.62f), m.Floor);
                var pad = Spawn(Station, "floor-panel", art, "Plate Top", new Vector3(0f, 0.03f, 0f),
                    Vector3.zero, new Vector3(1.8f, 0.4f, 1.8f), m.Flat);
                Set(root.GetComponent<PressurePlate>(), "indicator", pad.GetComponentInChildren<Renderer>());
            });

            Edit("Door", root =>
            {
                var panel = root.transform.Find("Door Panel");
                Hide(panel);
                ClearArt(root.transform);
                var art = NewChild(root.transform, VisualName).transform;
                Spawn(Modular, "gate", art, "Door Frame", Vector3.zero, Vector3.zero, new Vector3(1.55f, 1.3f, 0.45f), m.Wall);
                // The leaf follows the logical panel with easing; the panel/collider keep their linear motion.
                var leaf = Spawn(Modular, "template-wall", art, "Door Leaf", new Vector3(0f, 0f, 0.92f),
                    Vector3.zero, new Vector3(1.5f, 1.41f, 0.6f), m.Door);
                var follow = leaf.AddComponent<SmoothVisualFollow>();
                Set(follow, "source", panel);
                SetVector(follow, "offset", new Vector3(0f, -3f, 0.92f));
                follow.Snap();
            });

            Edit("Hazard", root =>
            {
                var surface = root.transform.Find("Hazard Surface");
                // Thin glowing field on the top face of the (unchanged) trigger volume.
                surface.localPosition = new Vector3(0f, 0.5f, 0f);
                surface.localScale = new Vector3(1f, 0.02f, 1f);
                surface.GetComponent<Renderer>().sharedMaterial = m.Hazard;
                surface.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            });

            Edit("Goal", root =>
            {
                Hide(root.transform.Find("Goal Pad"));
                Hide(root.transform.Find("Goal Pillar"));
                ClearArt(root.transform);
                var art = NewChild(root.transform, VisualName).transform;
                Spawn(Station, "floor-panel", art, "Goal Pad", new Vector3(0f, 0.015f, 0f), Vector3.zero, new Vector3(3.6f, 0.4f, 3f), m.GoalPad);
                Spawn(Modular, "gate", art, "Goal Frame", new Vector3(0f, 0f, 1.6f), new Vector3(0f, 180f, 0f), new Vector3(1.0f, 0.95f, 0.8f), m.Wall);
                var field = GameObject.CreatePrimitive(PrimitiveType.Quad);
                field.name = "Goal Field";
                UnityEngine.Object.DestroyImmediate(field.GetComponent<Collider>());
                field.transform.SetParent(art, false);
                field.transform.localPosition = new Vector3(0f, 1.9f, 1.55f);
                field.transform.localScale = new Vector3(2.8f, 3.6f, 1f);
                field.GetComponent<Renderer>().sharedMaterial = m.GoalField;
                field.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            });

            Edit("RewindPlatformVisual", null, create: true, build: root =>
            {
                var piece = Spawn(Modular, "template-floor-layer", root.transform, "Platform Mesh", new Vector3(0f, -0.2f, 0f),
                    Vector3.zero, new Vector3(1.19f, 1f, 0.714f), m.Platform);
                var feedback = root.AddComponent<RewindMechanismFeedback>();
                var data = new SerializedObject(feedback);
                var list = data.FindProperty("renderers");
                var renderers = piece.GetComponentsInChildren<Renderer>();
                list.arraySize = renderers.Length;
                for (var i = 0; i < renderers.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
                data.ApplyModifiedPropertiesWithoutUndo();
            });
        }

        private static void Edit(string name, Action<GameObject> modify, bool create = false, Action<GameObject> build = null)
        {
            var path = "Assets/_Echoryx/Prefabs/Level/" + name + ".prefab";
            GameObject root;
            if (create && !System.IO.File.Exists(path))
            {
                root = new GameObject(name);
                build(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                UnityEngine.Object.DestroyImmediate(root);
                return;
            }
            if (create)
            {
                root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    ClearChildren(root.transform);
                    var feedback = root.GetComponent<RewindMechanismFeedback>();
                    if (feedback != null) UnityEngine.Object.DestroyImmediate(feedback);
                    build(root);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
                return;
            }
            root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                modify(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        // ------------------------------------------------------------ scene

        private static void UpdateScene(Mats m)
        {
            var scene = EditorSceneManager.OpenScene(Level01TutorialBuilder.ScenePath, OpenSceneMode.Single);
            var geometry = GameObject.Find("Level Geometry") ?? throw new InvalidOperationException("Level Geometry missing.");
            var old = GameObject.Find(ArtRoot);
            if (old != null) UnityEngine.Object.DestroyImmediate(old);
            var art = NewChild(null, ArtRoot).transform;

            foreach (var name in new[] { "Floor 1 Start", "Floor 2 Main", "Floor 3 Goal", "Low Step", "Wall Left", "Wall Right", "Wall Start", "Wall End" })
                Hide(geometry.transform.Find(name));

            var startZ = -2f;
            var endZ = 56f;
            Floor(art, "Floor 1", startZ, 8f, m);
            Floor(art, "Floor 2", 10.5f, 36f, m);
            Floor(art, "Floor 3", 45f, endZ, m);

            Spawn(Station, "floor", art, "Low Step Art", new Vector3(-1.2f, 0.01f, 4.5f), Vector3.zero, new Vector3(2.4f, 0.7f / 0.3f, 1.2f), m.Accent);

            // Side walls (face toward the corridor), end walls, tiled along the existing wall colliders.
            var length = endZ - startZ;
            var count = Mathf.CeilToInt(length / 4f);
            var scaleLength = length / (count * 4f);
            for (var i = 0; i < count; i++)
            {
                var z = startZ + (i + 0.5f) * (length / count);
                Spawn(Modular, "template-wall", art, "Wall L " + i, new Vector3(-4f, 0f, z), new Vector3(0f, -90f, 0f), new Vector3(scaleLength, 1.4f, 1f), m.Wall);
                Spawn(Modular, "template-wall", art, "Wall R " + i, new Vector3(4f, 0f, z), new Vector3(0f, 90f, 0f), new Vector3(scaleLength, 1.4f, 1f), m.Wall);
            }
            for (var i = 0; i < 2; i++)
            {
                var x = -1.65f + i * 3.3f;
                Spawn(Modular, "template-wall", art, "Wall Start " + i, new Vector3(x, 0f, startZ - 1f), new Vector3(0f, 180f, 0f), new Vector3(0.825f, 1.4f, 1f), m.Wall);
                Spawn(Modular, "template-wall", art, "Wall End " + i, new Vector3(x, 0f, endZ + 1f), Vector3.zero, new Vector3(0.825f, 1.4f, 1f), m.Wall);
            }

            // Wall-mounted displays above head height (no obstruction of the lane).
            Display(art, "Display 1", new Vector3(2.92f, 3.7f, 3f), -90f, m);
            Display(art, "Display 2", new Vector3(-2.92f, 3.7f, 13f), 90f, m);
            Display(art, "Display 3", new Vector3(-2.92f, 3.7f, 22.5f), 90f, m);
            Display(art, "Display 4", new Vector3(2.92f, 3.7f, 33f), -90f, m);
            Display(art, "Display 5", new Vector3(2.92f, 3.7f, 49f), -90f, m);

            Background(art, m);

            // Gameplay-adjacent visuals in the scene.
            var platformSurface = GameObject.Find("Platform Surface and Collider");
            if (platformSurface != null)
            {
                Hide(platformSurface.transform);
                var existing = platformSurface.transform.parent.Find("Platform Art");
                if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Echoryx/Prefabs/Level/RewindPlatformVisual.prefab");
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, platformSurface.transform.parent);
                visual.name = "Platform Art";
                visual.transform.localPosition = Vector3.zero;
            }
            foreach (var marker in GameObject.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (marker.name != "Checkpoint Marker") continue;
                var filter = marker.GetComponent<MeshFilter>();
                filter.sharedMesh = MeshOf(Station, "floor-panel");
                var renderer = marker.GetComponent<Renderer>();
                renderer.sharedMaterial = m.Flat;
                marker.localPosition = new Vector3(0f, 0.015f, 0f);
                marker.localScale = new Vector3(5f, 0.2f, 1.5f);
            }

            PatchHint("Hint Rewind Platform", Level01TutorialBuilder.RewindPlatformHint);
            PatchHint("Hint Rewind Retry", Level01TutorialBuilder.RewindRetryHint);
            MarkScenery(art);
            ConfigureCamera();
            Atmosphere(scene, m);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save scene.");
        }

        private static void MarkScenery(Transform art)
        {
            foreach (var t in art.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
        }

        // Camera: sane depth range (near 0.1 / far 5000 caused depth z-fighting on the floor) and obstacle
        // avoidance against the gameplay colliders (Default layer: walls, floors, door panel, platform).
        private static void ConfigureCamera()
        {
            var cm = UnityEngine.Object.FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>()
                ?? throw new InvalidOperationException("Cinemachine camera missing.");
            var lens = cm.Lens;
            lens.NearClipPlane = 0.25f;
            lens.FarClipPlane = 300f;
            cm.Lens = lens;
            Camera.main.nearClipPlane = 0.25f;
            Camera.main.farClipPlane = 300f;

            var follow = cm.GetComponent<Unity.Cinemachine.CinemachineThirdPersonFollow>();
            var obstacles = follow.AvoidObstacles;
            obstacles.Enabled = true;
            obstacles.CameraRadius = 0.4f; // larger than the near-plane corner offset, so it never clips a wall
            follow.AvoidObstacles = obstacles;

            var deoccluder = cm.GetComponent<Unity.Cinemachine.CinemachineDeoccluder>()
                ?? cm.gameObject.AddComponent<Unity.Cinemachine.CinemachineDeoccluder>();
            deoccluder.CollideAgainst = LayerMask.GetMask("Default");
            deoccluder.IgnoreTag = "Player";
            deoccluder.MinimumDistanceFromTarget = 0.3f;
            var avoid = deoccluder.AvoidObstacles;
            avoid.Enabled = true;
            avoid.CameraRadius = 0.4f;
            avoid.Strategy = Unity.Cinemachine.CinemachineDeoccluder.ObstacleAvoidance.ResolutionStrategy.PullCameraForward;
            avoid.MaximumEffort = 4;
            avoid.SmoothingTime = 0.05f;
            avoid.Damping = 0.2f;
            avoid.DampingWhenOccluded = 0.02f;
            avoid.UseFollowTarget.Enabled = true; // the vcam has a Follow target only
            avoid.UseFollowTarget.YOffset = 0f;
            deoccluder.AvoidObstacles = avoid;
            EditorUtility.SetDirty(cm);
        }

        private static void PatchHint(string name, string text)
        {
            var hint = GameObject.Find(name)?.GetComponent<TutorialHint>() ?? throw new InvalidOperationException("Hint missing: " + name);
            SetString(hint, "message", text);
        }

        private static void SetString(UnityEngine.Object target, string field, string value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).stringValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Floor(Transform art, string name, float from, float to, Mats m)
        {
            var length = to - from;
            var count = Mathf.CeilToInt(length / 4.2f);
            var tile = length / count;
            for (var i = 0; i < count; i++)
                Spawn(Modular, "template-floor-layer", art, name + " Tile " + i, new Vector3(0f, -0.4f, from + (i + 0.5f) * tile),
                    Vector3.zero, new Vector3(1.571f, 1f, tile / 4.2f), m.Floor);
            // Hull block under the walkway so the level reads as a floating station section.
            Spawn(Station, "floor", art, name + " Hull", new Vector3(0f, -3.23f, (from + to) * 0.5f), Vector3.zero,
                new Vector3(5.6f, 2.8f / 0.3f, Mathf.Max(1f, length - 0.8f)), m.Hull);
        }

        private static void Display(Transform art, string name, Vector3 position, float yaw, Mats m) =>
            Spawn(Station, "display-wall-wide", art, name, position, new Vector3(0f, yaw, 0f), Vector3.one * 2.2f, m.Accent);

        private static void Background(Transform art, Mats m)
        {
            var bg = NewChild(art, "Background Station").transform;
            // Deterministic floating station sections and asteroids around the walkway.
            Spawn(Modular, "room-large", bg, "Hull A", new Vector3(-38f, -16f, 8f), new Vector3(6f, 25f, 10f), Vector3.one * 1.3f, m.Wall);
            Spawn(Modular, "room-small", bg, "Hull B", new Vector3(34f, -22f, 30f), new Vector3(-8f, -40f, 12f), Vector3.one * 1.4f, m.Wall);
            Spawn(Modular, "room-wide", bg, "Hull C", new Vector3(-32f, 4f, 60f), new Vector3(0f, 70f, 0f), Vector3.one * 1.2f, m.Wall);
            Spawn(Modular, "corridor", bg, "Strut A", new Vector3(18f, -10f, -14f), new Vector3(0f, 0f, 80f), new Vector3(2f, 3f, 2f), m.Floor);
            Spawn(Modular, "stairs-wide", bg, "Stairs A", new Vector3(30f, 6f, 62f), new Vector3(0f, 200f, 12f), Vector3.one * 1.5f, m.Wall);
            Spawn(Modular, "template-floor-big", bg, "Platform A", new Vector3(-22f, -9f, 34f), new Vector3(0f, 15f, 0f), Vector3.one * 1.8f, m.Floor);
            Spawn(Modular, "template-floor-big", bg, "Platform B", new Vector3(24f, -4f, 8f), new Vector3(10f, -25f, 5f), Vector3.one * 1.5f, m.Floor);
            var rocks = new[] { new Vector3(-14f, 7f, 24f), new Vector3(16f, 12f, 40f), new Vector3(-20f, -3f, -10f), new Vector3(26f, 3f, 52f), new Vector3(-9f, 16f, 6f), new Vector3(10f, -14f, 20f) };
            for (var i = 0; i < rocks.Length; i++)
                Spawn(Station, i % 3 == 2 ? "skip-rocks" : "rocks", bg, "Asteroid " + i, rocks[i], new Vector3(20f * i, 47f * i, 13f * i), Vector3.one * (3f + i * 0.9f), m.Accent);
            var cargo = new[] { new Vector3(-17f, 2f, 44f), new Vector3(19f, -8f, 14f), new Vector3(14f, 9f, 58f) };
            for (var i = 0; i < cargo.Length; i++)
                Spawn(Station, "container-tall", bg, "Cargo " + i, cargo[i], new Vector3(15f * i, 60f * i, 25f * i), Vector3.one * 3.5f, m.Accent);
        }

        private static void Atmosphere(UnityEngine.SceneManagement.Scene scene, Mats m)
        {
            RenderSettings.skybox = m.Sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.55f, 0.88f);
            RenderSettings.ambientEquatorColor = new Color(0.46f, 0.42f, 0.62f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.20f, 0.34f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.007f;
            RenderSettings.fogColor = new Color(0.30f, 0.20f, 0.46f);
            var light = GameObject.Find("Directional Light").GetComponent<Light>();
            light.color = new Color(1f, 0.82f, 0.64f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.8f;
            light.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            RenderSettings.sun = light;

            var camera = Camera.main;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            foreach (var component in profile.components.ToArray()) { profile.Remove(component.GetType()); UnityEngine.Object.DestroyImmediate(component, true); }
            var bloom = profile.Add<Bloom>();
            bloom.threshold.Override(1.0f);
            bloom.intensity.Override(0.55f);
            bloom.scatter.Override(0.7f);
            var adjust = profile.Add<ColorAdjustments>();
            adjust.saturation.Override(14f);
            adjust.contrast.Override(6f);
            var vignette = profile.Add<Vignette>();
            vignette.intensity.Override(0.2f);
            vignette.smoothness.Override(0.5f);
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(profile);

            var existing = GameObject.Find("Global Volume");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
            var volumeObject = new GameObject("Global Volume");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            DynamicGI.UpdateEnvironment();
        }

        // ------------------------------------------------------------ helpers

        private static GameObject Spawn(string kit, string model, Transform parent, string name, Vector3 position, Vector3 euler, Vector3 scale, Material material)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(kit + model + ".fbx")
                ?? throw new InvalidOperationException("Missing model " + kit + model);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.name = name;
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                var materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; i++) materials[i] = material;
                renderer.sharedMaterials = materials;
            }
            // Static batching bakes the mesh at its authored position: anything that moves (door leaf,
            // platform, plate indicator) must stay non-static. Pure scenery is marked in MarkScenery.
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            return go;
        }

        private static Mesh MeshOf(string kit, string model) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(kit + model + ".fbx").GetComponentInChildren<MeshFilter>().sharedMesh;

        private static void Hide(Transform target)
        {
            if (target == null) return;
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false;
        }

        private static GameObject NewChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        private static void ClearArt(Transform root)
        {
            var existing = root.Find(VisualName);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }

        private static void ClearChildren(Transform root)
        {
            for (var i = root.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
        }

        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetVector(UnityEngine.Object target, string field, Vector3 value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).vector3Value = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

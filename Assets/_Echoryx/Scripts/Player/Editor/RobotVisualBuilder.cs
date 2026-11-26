using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoryx.Player.Editor
{
    /// <summary>
    /// Builds the gameplay locomotion controller, the RobotVisual / RobotEchoVisual prefabs and a lab scene
    /// from the "Animated Low Poly Robot Free" package (referenced in place, never copied).
    /// Step 1 builds and validates in a lab without touching Player.prefab; step 2 integrates.
    /// </summary>
    public static class RobotVisualBuilder
    {
        private const string Fbx = "Assets/FreeLowPolyRobot/Meshes_and_Animations/ModularRobots.fbx";
        private const string AnimFolder = "Assets/_Echoryx/Art/Animations/Robot";
        public const string ControllerPath = AnimFolder + "/PlayerLocomotion.controller";
        public const string VisualPath = "Assets/_Echoryx/Prefabs/Characters/RobotVisual.prefab";
        public const string EchoVisualPath = "Assets/_Echoryx/Prefabs/Characters/RobotEchoVisual.prefab";
        public const string LabPath = "Assets/_Echoryx/Scenes/RobotIntegrationLab.unity";
        private const string PlayerPath = "Assets/_Echoryx/Prefabs/Player.prefab";
        private const string EchoPath = "Assets/_Echoryx/Prefabs/EchoActor.prefab";
        private const string HologramPath = "Assets/_Echoryx/Materials/EchoHologram.mat";
        private const string BasePath = "Assets/FreeLowPolyRobot/Materials/M_AtlasBase.mat";
        /// <summary>
        /// The Player robot's colour: golden amber, to stand out against the lilac/orange level (cyan and blue are
        /// reserved for the Echo). This is the only value to change; then run Echoryx/Robot/Apply Accent Color.
        /// It tints the atlas' light-to-dark ramp, so light parts show the colour and dark parts stay dark.
        /// </summary>
        public static readonly Color AccentColor = new Color(1f, 0.79f, 0.20f);
        /// <summary>The Player robot's eyes: warm white (cyan stays exclusive to the Echo). Same menu applies it.</summary>
        public static readonly Color EyeColor = new Color(1f, 0.95f, 0.85f);
        /// <summary>HDR strength of the eyes: enough to glow without burning out.</summary>
        private const float EyeGlow = 1.15f;
        private const string PackageEyes = "M_AtlasEmissive";
        private const string EyesName = "RobotPlayerEyes";
        /// <summary>Soft self-light so the colour still reads as gold on faces in shadow.</summary>
        private const float AccentGlow = 0.45f;

        /// <summary>Authored visual scale: ~1.85 m tall, matching the 2 m capsule collider.</summary>
        public const float ModelScale = 1.1f;
        /// <summary>The model faces +Z in the FBX; yaw correction if a lab render shows otherwise.</summary>
        public const float ModelYaw = 0f;
        public const float PlayerMaxSpeed = 6f;

        [MenuItem("Echoryx/Robot/1 Build Controller, Prefabs and Lab")]
        public static void BuildLab()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var clips = LoadClips();
            EnsureFolder("Assets/_Echoryx/Art", "Animations");
            EnsureFolder("Assets/_Echoryx/Art/Animations", "Robot");
            var calibration = Calibrate(clips);
            var controller = BuildController(clips, calibration);
            BuildPrefabs(controller, calibration);
            BuildLabScene();
            Debug.Log($"Robot lab built. Walk~{calibration.WalkSpeed:F2} m/s, Run~{calibration.RunSpeed:F2} m/s, " +
                $"nominal speeds set on the drivers, feet offset {calibration.FeetOffset:F3}.");
        }

        [MenuItem("Echoryx/Robot/2 Integrate Into Player and Echo")]
        public static void Integrate()
        {
            Replace(PlayerPath, "VisualRoot", "Capsule Placeholder", VisualPath, "Robot Visual");
            Replace(EchoPath, null, "Hologram Visual", EchoVisualPath, "Robot Echo Visual");
            AssetDatabase.SaveAssets();
            Debug.Log("Robot integrated into Player and Echo prefabs.");
        }

        // ------------------------------------------------------------ clips and calibration

        private sealed class Clips { public AnimationClip Idle, Walk, Run, JumpStart, JumpAir, JumpLanding; }
        private sealed class Calibration { public float WalkSpeed, RunSpeed, RunTimeScale, WalkTimeScale, FeetOffset; }

        private static Clips LoadClips()
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__")).ToArray();
            AnimationClip Get(string name) => all.FirstOrDefault(c => c.name == name)
                ?? throw new InvalidOperationException("Clip missing in package: " + name);
            return new Clips
            {
                Idle = Get("StaticIdle"), Walk = Get("Walk"), Run = Get("Run"),
                JumpStart = Get("Jump_Start"), JumpAir = Get("Jump_Air"), JumpLanding = Get("Jump_Landing"),
            };
        }

        private static Calibration Calibrate(Clips clips)
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Fbx));
            try
            {
                var calibration = new Calibration();
                // Feet on the floor: lowest skinned vertex in the idle pose.
                clips.Idle.SampleAnimation(model, 0f);
                var minY = float.MaxValue;
                foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var baked = new Mesh();
                    skin.BakeMesh(baked, true);
                    foreach (var vertex in baked.vertices) minY = Mathf.Min(minY, model.transform.InverseTransformPoint(skin.transform.TransformPoint(vertex)).y);
                    UnityEngine.Object.DestroyImmediate(baked);
                }
                calibration.FeetOffset = -minY * ModelScale;
                // Ground speed that matches the foot sweep during stance (about half the cycle).
                calibration.WalkSpeed = EstimateGroundSpeed(model, clips.Walk) * ModelScale;
                calibration.RunSpeed = EstimateGroundSpeed(model, clips.Run) * ModelScale;
                calibration.WalkTimeScale = 1f;
                // Run is authored for ~RunSpeed; the player tops out at 6 m/s, so scale playback to match.
                calibration.RunTimeScale = Mathf.Clamp(PlayerMaxSpeed / Mathf.Max(0.1f, calibration.RunSpeed), 0.8f, 1.5f);
                return calibration;
            }
            finally { UnityEngine.Object.DestroyImmediate(model); }
        }

        private static float EstimateGroundSpeed(GameObject model, AnimationClip clip)
        {
            var left = FindDeep(model.transform, "DEF_Foot.L");
            var right = FindDeep(model.transform, "DEF_Foot.R");
            var best = 0f;
            foreach (var foot in new[] { left, right })
            {
                float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
                for (var i = 0; i < 32; i++)
                {
                    clip.SampleAnimation(model, clip.length * i / 32f);
                    var local = model.transform.InverseTransformPoint(foot.position);
                    minX = Mathf.Min(minX, local.x); maxX = Mathf.Max(maxX, local.x);
                    minZ = Mathf.Min(minZ, local.z); maxZ = Mathf.Max(maxZ, local.z);
                }
                best = Mathf.Max(best, Mathf.Max(maxX - minX, maxZ - minZ));
            }
            return best / (0.5f * clip.length);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            throw new InvalidOperationException("Bone not found: " + name);
        }

        // ------------------------------------------------------------ controller

        private static AnimatorController BuildController(Clips clips, Calibration calibration)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            var parameters = controller.parameters;
            parameters[1].defaultBool = true;
            controller.parameters = parameters;
            var machine = controller.layers[0].stateMachine;

            // 1D blend tree on Speed: Idle -> Walk -> Run (Run calibrated to the 6 m/s player maximum).
            var tree = new BlendTree { name = "Locomotion", blendType = BlendTreeType.Simple1D, blendParameter = "Speed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.AddChild(clips.Idle, 0f);
            tree.AddChild(clips.Walk, Mathf.Clamp(calibration.WalkSpeed, 1.5f, 4f));
            tree.AddChild(clips.Run, PlayerMaxSpeed);
            // Playback speed is matched at runtime by RobotAnimationDriver (real speed / nominal stride speed),
            // so the blend tree children keep timeScale 1.
            var locomotion = machine.AddState("Locomotion");
            locomotion.motion = tree;
            machine.defaultState = locomotion;

            var start = machine.AddState("Jump_Start"); start.motion = clips.JumpStart;
            var air = machine.AddState("Jump_Air"); air.motion = clips.JumpAir;
            var landing = machine.AddState("Jump_Landing"); landing.motion = clips.JumpLanding;

            Transition(locomotion, start, 0.04f, false, 0f, ("Grounded", false));
            Transition(start, air, 0.05f, true, 0.95f);
            Transition(start, landing, 0.05f, false, 0f, ("Grounded", true));
            Transition(air, landing, 0.05f, false, 0f, ("Grounded", true));
            Transition(landing, start, 0.05f, false, 0f, ("Grounded", false));
            Transition(landing, locomotion, 0.1f, true, 0.9f);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void Transition(AnimatorState from, AnimatorState to, float duration, bool exitTime, float exit, (string name, bool value)? condition = null)
        {
            var transition = from.AddTransition(to);
            transition.duration = duration;
            transition.hasExitTime = exitTime;
            transition.exitTime = exit;
            transition.hasFixedDuration = true;
            if (condition.HasValue)
                transition.AddCondition(condition.Value.value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, condition.Value.name);
        }

        // ------------------------------------------------------------ prefabs

        private static void BuildPrefabs(AnimatorController controller, Calibration calibration)
        {
            SavePrefab(VisualPath, "RobotVisual", controller, calibration, echo: false);
            SavePrefab(EchoVisualPath, "RobotEchoVisual", controller, calibration, echo: true);
        }

        private static void SavePrefab(string path, string name, AnimatorController controller, Calibration calibration, bool echo)
        {
            var root = new GameObject(name);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Fbx), root.transform);
            model.name = "Model";
            model.transform.localPosition = new Vector3(0f, calibration.FeetOffset, 0f);
            model.transform.localRotation = Quaternion.Euler(0f, ModelYaw, 0f);
            model.transform.localScale = Vector3.one * ModelScale;
            var animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.avatar = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<Avatar>().FirstOrDefault();
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            if (!echo)
            {
                var (baseMatte, variant) = AccentMaterials();
                var eyes = EyeMaterial();
                foreach (var renderer in model.GetComponentsInChildren<Renderer>())
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(m =>
                        m == null ? m : m.name == "M_AtlasOffset" ? variant : m.name == "M_AtlasBase" ? baseMatte : m.name == PackageEyes ? eyes : m).ToArray();
            }
            if (echo)
            {
                // Same model, cyan translucent hologram (the project's Echo look). Echo never casts shadows.
                var hologram = AssetDatabase.LoadAssetAtPath<Material>(HologramPath) ?? throw new InvalidOperationException("EchoHologram material missing.");
                foreach (var renderer in model.GetComponentsInChildren<Renderer>())
                {
                    renderer.sharedMaterials = Enumerable.Repeat(hologram, renderer.sharedMaterials.Length).ToArray();
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
            var driver = root.AddComponent<RobotAnimationDriver>();
            var data = new SerializedObject(driver);
            data.FindProperty("animator").objectReferenceValue = animator;
            // Nominal stride speeds of the Walk/Run clips (estimated from the foot sweep, incl. visual scale).
            data.FindProperty("walkNominalSpeed").floatValue = Mathf.Round(calibration.WalkSpeed * 100f) / 100f;
            data.FindProperty("runNominalSpeed").floatValue = Mathf.Round(calibration.RunSpeed * 100f) / 100f;
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        /// <summary>Re-tints the Player body and eyes without rebuilding controller or lab. The Echo uses its own material.</summary>
        [MenuItem("Echoryx/Robot/Apply Accent Color")]
        public static void ApplyAccentColor()
        {
            AccentMaterials();
            AssignEyes(EyeMaterial());
            AssetDatabase.SaveAssets();
            Debug.Log("Robot accent applied.");
        }

        // The package's eye material takes its cyan from the atlas, so ours has no texture: only the colour.
        private static Material EyeMaterial()
        {
            EnsureFolder("Assets/_Echoryx/Materials", "Robot");
            var path = "Assets/_Echoryx/Materials/Robot/" + EyesName + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<Material>(BasePath) ?? throw new InvalidOperationException(BasePath + " missing.");
                material = new Material(source.shader) { name = EyesName };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", EyeColor);
            material.SetColor("_Color", EyeColor);
            material.SetColor("_EmissionColor", EyeColor * EyeGlow);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.2f);
            EditorUtility.SetDirty(material);
            return material;
        }

        // Swaps the eye slots of RobotVisual only; saved just when something changes.
        private static void AssignEyes(Material eyes)
        {
            var root = PrefabUtility.LoadPrefabContents(VisualPath);
            try
            {
                var changed = false;
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    var materials = renderer.sharedMaterials;
                    for (var i = 0; i < materials.Length; i++)
                        if (materials[i] != null && materials[i].name == PackageEyes) { materials[i] = eyes; changed = true; }
                    renderer.sharedMaterials = materials;
                }
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, VisualPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        // The package materials are Metallic 1 (render black without reflection probes), so both slots get matte
        // URP Lit copies; the package's offset shader has no tint, so it is not used.
        private static (Material baseSlot, Material offsetSlot) AccentMaterials() =>
            (Matte("RobotPlayerBase", 0.15f), Matte("RobotPlayerOrange", 0.25f));

        private static Material Matte(string name, float smoothness)
        {
            EnsureFolder("Assets/_Echoryx/Materials", "Robot");
            var source = AssetDatabase.LoadAssetAtPath<Material>(BasePath) ?? throw new InvalidOperationException(BasePath + " missing.");
            var path = "Assets/_Echoryx/Materials/Robot/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(source) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = source.shader;
            material.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
            material.SetTextureOffset("_BaseMap", Vector2.zero); // the atlas' neutral (grey) ramp
            material.SetColor("_BaseColor", AccentColor);
            material.SetColor("_Color", AccentColor);
            // The atlas masks the self-light, so its dark areas stay dark.
            material.SetTexture("_EmissionMap", source.GetTexture("_BaseMap"));
            material.SetColor("_EmissionColor", AccentColor * AccentGlow);
            material.EnableKeyword("_EMISSION");
            // URP drops _EMISSION when saving a material that is not flagged as emissive.
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Glossiness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        // ------------------------------------------------------------ lab scene

        private static void BuildLabScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Lab Floor";
            floor.transform.position = new Vector3(0f, -0.5f, 10f);
            floor.transform.localScale = new Vector3(14f, 1f, 40f);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Lab Wall";
            wall.transform.position = new Vector3(4f, 1.5f, 6f);
            wall.transform.localScale = new Vector3(0.5f, 3f, 12f);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath));
            player.name = "Player";
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            // Scene-only integration: the shared Player.prefab is untouched in the lab.
            var visualRoot = player.transform.Find("VisualRoot");
            visualRoot.Find("Capsule Placeholder").gameObject.SetActive(false);
            // (If the shared prefab already carries the robot, the lab simply reuses it.)
            if (visualRoot.Find("Robot Visual") == null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath), visualRoot);
                visual.name = "Robot Visual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
            }

            var camera = Camera.main;
            camera.transform.position = new Vector3(0f, 3f, -5f);
            camera.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, LabPath)) throw new InvalidOperationException("Could not save the lab scene.");
        }

        // ------------------------------------------------------------ integration

        private static void Replace(string prefabPath, string parentName, string placeholder, string visualPath, string visualName)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var parent = parentName == null ? root.transform : root.transform.Find(parentName)
                    ?? throw new InvalidOperationException(prefabPath + " has no " + parentName);
                var old = parent.Find(placeholder) ?? throw new InvalidOperationException(prefabPath + " has no " + placeholder);
                var existing = parent.Find(visualName);
                if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(visualPath), parent);
                visual.name = visualName;
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                // Keep the old placeholder (hidden) so scene overrides on it never break; colliders/logic untouched.
                old.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}

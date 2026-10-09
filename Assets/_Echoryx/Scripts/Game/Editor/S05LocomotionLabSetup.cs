using System;
using System.IO;
using System.Linq;
using Echoryx.Game.VisualLabs;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echoryx.Game.Editor
{
    /// <summary>Imports source Avatars and creates an isolated lab through Unity APIs.</summary>
    public static class S05LocomotionLabSetup
    {
        public const string Root = "Assets/_Echoryx/Art/Animations/SuperSoldier";
        public const string ControllerPath = Root + "/SuperSoldierLocomotion.controller";
        public const string ScenePath = "Assets/_Echoryx/Scenes/SuperSoldierLocomotionLab.unity";
        private const string JumpReplacementPath = Root + "/MixamoReplacement/Jumping Up.fbx";
        private const string TargetModel = "Assets/_Echoryx/Art/Characters/SihaGamesSuperSoldier/Idle.fbx";
        private static readonly string[] Actions = { "Walk", "Run", "Jump", "Fall", "Land" };
        private static readonly string[] Sources = { "Walking", "Running", "Jumping", "Falling Idle", "Falling To Landing" };

        [MenuItem("Echoryx/S05-B3/Prepare Locomotion Lab")]
        public static void Prepare()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            for (var i = 0; i < Actions.Length; i++)
                if (i != 2 || !HasReplacementJump()) ImportClip(i);
            CreateIdleLoop();
            ConfigureController();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) CreateLab();
            AssetDatabase.SaveAssets();
            Validate();
        }

        private static string SourcePath(int i) => Root + "/Mixamo/X Bot@" + Sources[i] + ".fbx";

        private static void ImportClip(int i)
        {
            var importer = AssetImporter.GetAtPath(SourcePath(i)) as ModelImporter;
            Require(importer != null, "Missing source " + SourcePath(i));
            importer.animationType = ModelImporterAnimationType.Human;
            // X Bot has 65 bones, unlike the SuperSoldier. Retarget using its own source Avatar.
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.sourceAvatar = null;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            // Animation-only exports otherwise calibrate the Avatar from an animated frame.
            // Generate the reference from the FBX rest transforms, then keep it when clips return.
            var reference = importer.humanDescription;
            reference.skeleton = Array.Empty<SkeletonBone>();
            importer.humanDescription = reference;
            importer.importAnimation = false;
            importer.SaveAndReimport();
            importer.importAnimation = true;
            importer.SaveAndReimport();
            var definitions = importer.defaultClipAnimations;
            Require(definitions.Length == 1, "Expected one Mixamo take in " + Sources[i]);
            var definition = definitions[0];
            definition.name = Actions[i];
            definition.loopTime = i == 0 || i == 1 || i == 3;
            definition.loopPose = definition.loopTime;
            // These exports move forward. Baking XZ into bones would retain visual drift.
            // Extract that trajectory and ignore its application on the lab Animator instead.
            definition.lockRootPositionXZ = false;
            definition.keepOriginalPositionXZ = false;
            definition.lockRootRotation = false;
            definition.keepOriginalOrientation = false;
            definition.rotationOffset = 0f;
            definition.lockRootHeightY = i == 0 || i == 1 || i == 4;
            definition.keepOriginalPositionY = false;
            definition.heightFromFeet = true;
            // Feet-based projection references X Bot bone contacts, not the SuperSoldier boot soles.
            // Negative offsets keep the extracted-motion airborne/impact poses above the lab floor.
            definition.heightOffset = i == 2 || i == 3 ? -0.2f : 0f;
            // Keep takeoff/ascent; Land begins at contact, not during the preceding fall.
            // Original full takes remain in the FBX, with no keyframe edits.
            if (i == 2) { definition.firstFrame = 10f; definition.lastFrame = 24f; }
            if (i == 4) definition.firstFrame = 8f;
            importer.clipAnimations = definitions;
            importer.SaveAndReimport();
        }

        private static void CreateIdleLoop()
        {
            var path = Root + "/IdleLoop.anim";
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null) return;
            var source = AssetDatabase.LoadAllAssetsAtPath(TargetModel).OfType<AnimationClip>()
                .Single(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));
            var copy = UnityEngine.Object.Instantiate(source);
            copy.name = "IdleLoop";
            var settings = AnimationUtility.GetAnimationClipSettings(copy);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(copy, settings);
            AssetDatabase.CreateAsset(copy, path);
        }

        private static bool HasReplacementJump() => AssetDatabase.LoadAllAssetsAtPath(JumpReplacementPath)
            .OfType<AnimationClip>().Any(c => c.name == "Jump" && c.humanMotion);

        private static string SourcePathForAction(int i) => i == 2 && HasReplacementJump() ? JumpReplacementPath : SourcePath(i);

        private static AnimationClip Clip(string action)
        {
            if (action == "Jump")
            {
                var inPlace = AssetDatabase.LoadAssetAtPath<AnimationClip>(S05JumpReplacementSetup.InPlacePath);
                if (inPlace != null) return inPlace;
            }
            if (action == "IdleLoop") return AssetDatabase.LoadAssetAtPath<AnimationClip>(Root + "/IdleLoop.anim");
            if (action == "Jump" && HasReplacementJump())
                return AssetDatabase.LoadAllAssetsAtPath(JumpReplacementPath).OfType<AnimationClip>().Single(c => c.name == "Jump");
            var i = Array.IndexOf(Actions, action);
            return AssetDatabase.LoadAllAssetsAtPath(SourcePath(i)).OfType<AnimationClip>().Single(c => c.name == action);
        }

        private static void ConfigureController()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath)
                ?? AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            foreach (var pair in new[] { ("Speed", AnimatorControllerParameterType.Float),
                ("Grounded", AnimatorControllerParameterType.Bool), ("VerticalSpeed", AnimatorControllerParameterType.Float) })
                if (!controller.parameters.Any(p => p.name == pair.Item1)) controller.AddParameter(pair.Item1, pair.Item2);
            var parameters = controller.parameters;
            parameters.Single(p => p.name == "Grounded").defaultBool = true;
            controller.parameters = parameters;
            var machine = controller.layers[0].stateMachine;
            var locomotion = State(machine, "Locomotion", new Vector3(220f, 70f));
            var tree = locomotion.motion as BlendTree;
            if (tree == null)
            {
                tree = new BlendTree { name = "Idle Walk Run" };
                AssetDatabase.AddObjectToAsset(tree, controller);
            }
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.children = new[] {
                new ChildMotion { motion = Clip("IdleLoop"), threshold = 0f, timeScale = 1f },
                new ChildMotion { motion = Clip("Walk"), threshold = Clip("Walk").averageSpeed.magnitude, timeScale = 1f },
                new ChildMotion { motion = Clip("Run"), threshold = Clip("Run").averageSpeed.magnitude, timeScale = 1f }
            };
            locomotion.motion = tree;
            var jump = State(machine, "Jump", new Vector3(450f, 70f));
            jump.motion = Clip("Jump");
            var fall = State(machine, "Fall", new Vector3(450f, 220f));
            fall.motion = Clip("Fall");
            var land = State(machine, "Land", new Vector3(220f, 220f));
            land.motion = Clip("Land");
            foreach (var state in machine.states)
            {
                state.state.writeDefaultValues = false;
                state.state.iKOnFeet = state.state == locomotion || state.state == land;
                foreach (var transition in state.state.transitions) state.state.RemoveTransition(transition);
            }
            machine.defaultState = locomotion;
            var takeoff = Transition(locomotion, jump);
            takeoff.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
            takeoff.AddCondition(AnimatorConditionMode.Greater, 0f, "VerticalSpeed");
            var drop = Transition(locomotion, fall);
            drop.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
            drop.AddCondition(AnimatorConditionMode.Less, 0.01f, "VerticalSpeed");
            Transition(jump, land).AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
            var apex = Transition(jump, fall);
            if (HasReplacementJump()) apex.duration = 0.18f;
            apex.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
            apex.AddCondition(AnimatorConditionMode.Less, 0.01f, "VerticalSpeed");
            Transition(fall, land).AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
            var landDrop = Transition(land, fall);
            landDrop.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
            var resume = Transition(land, locomotion);
            resume.hasExitTime = true;
            resume.exitTime = 0.95f;
            resume.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
            EditorUtility.SetDirty(tree);
            EditorUtility.SetDirty(controller);
        }

        private static AnimatorState State(AnimatorStateMachine machine, string name, Vector3 position)
        {
            var state = machine.states.FirstOrDefault(s => s.state.name == name).state;
            return state != null ? state : machine.AddState(name, position);
        }

        private static AnimatorStateTransition Transition(AnimatorState from, AnimatorState to)
        {
            var result = from.AddTransition(to);
            result.hasExitTime = false;
            result.hasFixedDuration = true;
            result.duration = 0.12f;
            result.canTransitionToSelf = false;
            result.interruptionSource = TransitionInterruptionSource.Destination;
            result.orderedInterruption = true;
            return result;
        }

        private static void CreateLab()
        {
            Require(AssetDatabase.CopyAsset("Assets/_Echoryx/Scenes/SuperSoldierValidationLab.unity", ScenePath), "Could not copy isolated lab.");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var model = GameObject.Find("SuperSoldier Animated Preview");
            Require(model != null, "No SuperSoldier in copied lab.");
            model.name = "SuperSoldier Locomotion Preview";
            // B2 aligned the static bind-pose bounds. Humanoid animation uses the root as its floor reference.
            var position = model.transform.position;
            position.y = 0f;
            model.transform.position = position;
            var animator = model.GetComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>()) renderer.updateWhenOffscreen = true;
            var driver = model.AddComponent<LocomotionAnimationPreview>();
            var fields = new SerializedObject(driver);
            fields.FindProperty("animator").objectReferenceValue = animator;
            fields.FindProperty("landingPreviewSeconds").floatValue = Clip("Land").length + 0.5f;
            fields.ApplyModifiedPropertiesWithoutUndo();
            Require(EditorSceneManager.SaveScene(scene), "Could not save locomotion lab.");
        }

        [MenuItem("Echoryx/S05-B3/Align Lab Floor Reference")]
        public static void AlignLabFloor()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var model = GameObject.Find("SuperSoldier Locomotion Preview");
            var position = model.transform.position;
            position.y = 0f;
            model.transform.position = position;
            Require(EditorSceneManager.SaveScene(scene), "Could not save floor alignment.");
            Validate();
        }

        [MenuItem("Echoryx/S05-B3/Validate Locomotion Lab")]
        public static void Validate()
        {
            var target = AssetDatabase.LoadAllAssetsAtPath(TargetModel).OfType<Avatar>().Single(a => a.isHuman && a.isValid);
            Require(target != null, "Target Humanoid Avatar invalid.");
            var reports = new ClipReport[6];
            for (var i = 0; i < Actions.Length; i++)
            {
                var sourcePath = SourcePathForAction(i);
                var importer = (ModelImporter)AssetImporter.GetAtPath(sourcePath);
                var avatar = AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<Avatar>().Single();
                var clip = Clip(Actions[i]);
                Require(importer.animationType == ModelImporterAnimationType.Human && importer.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel,
                    "Source must use its own Humanoid Avatar.");
                Require(avatar.isValid && avatar.isHuman && clip.humanMotion, "Invalid Humanoid source " + Actions[i]);
                var definition = importer.clipAnimations.Single();
                Require(!definition.lockRootPositionXZ, "Horizontal source trajectory must be extracted.");
                reports[i] = new ClipReport { name = Actions[i], source = sourcePath, motionAsset = AssetDatabase.GetAssetPath(clip), seconds = clip.length,
                    frameRate = clip.frameRate, firstFrame = definition.firstFrame, lastFrame = definition.lastFrame, humanMotion = clip.humanMotion, sourceAvatarValid = avatar.isValid,
                    sourceAvatarHuman = avatar.isHuman, loop = clip.isLooping, heightOffset = definition.heightOffset,
                    rootPolicy = AssetDatabase.GetAssetPath(clip) == S05JumpReplacementSetup.InPlacePath ? "Generated preparation-only body Y; positive source travel removed, baked with Original reference" :
                        definition.lockRootHeightY ? "XZ/heading extracted; Y baked into pose" : "XZ/heading/Y extracted",
                    sourceAverageSpeed = clip.averageSpeed };
            }
            var idle = Clip("IdleLoop");
            Require(idle.humanMotion && idle.isLooping, "Reused Idle must be Humanoid and loop.");
            reports[5] = new ClipReport { name = "IdleLoop", source = TargetModel, seconds = idle.length,
                frameRate = idle.frameRate, humanMotion = idle.humanMotion, sourceAvatarValid = true, sourceAvatarHuman = true,
                loop = idle.isLooping, rootPolicy = "Original Idle settings preserved; only loop settings enabled", sourceAverageSpeed = idle.averageSpeed };
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var model = GameObject.Find("SuperSoldier Locomotion Preview");
            var animator = model.GetComponent<Animator>();
            Require(animator.avatar == target && !animator.applyRootMotion && model.GetComponent<LocomotionAnimationPreview>() != null, "Invalid lab Animator.");
            Require(animator.runtimeAnimatorController.animationClips.Distinct().Count() == 6, "Controller must reference all six clips.");
            Require(scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Collider>(true)).Count() == 0, "Lab must remain outside gameplay physics.");
            Directory.CreateDirectory("docs");
            File.WriteAllText("docs/supersoldier-locomotion-import-report.json", JsonUtility.ToJson(new ImportReport
                { unity = Application.unityVersion, targetAvatarValid = target.isValid, targetAvatarHuman = target.isHuman,
                    scene = ScenePath, controller = ControllerPath, applyRootMotion = animator.applyRootMotion,
                    labRootPosition = animator.transform.position, clips = reports }, true) + "\n");
            Debug.Log("S05-B3 import validation passed: five source Humanoid Avatars, reused Idle, six-clip controller and isolated scene.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        [Serializable] private sealed class ImportReport
        {
            public string unity, scene, controller;
            public bool targetAvatarValid, targetAvatarHuman, applyRootMotion;
            public Vector3 labRootPosition;
            public ClipReport[] clips;
        }
        [Serializable] private sealed class ClipReport
        {
            public string name, source, rootPolicy, motionAsset;
            public float seconds, frameRate, heightOffset, firstFrame, lastFrame;
            public bool humanMotion, sourceAvatarValid, sourceAvatarHuman, loop;
            public Vector3 sourceAverageSpeed;
        }
    }
}

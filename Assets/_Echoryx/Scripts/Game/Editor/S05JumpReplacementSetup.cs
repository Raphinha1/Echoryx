using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Echoryx.Game.VisualLabs;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Echoryx.Game.Editor
{
    /// <summary>Imports the user-provided Mixamo take and replaces only the lab Jump motion.</summary>
    public static class S05JumpReplacementSetup
    {
        private const string Root = "Assets/_Echoryx/Art/Animations/SuperSoldier";
        public const string SourcePath = Root + "/MixamoReplacement/Jumping Up.fbx";
        public const string InPlacePath = Root + "/JumpInPlace.anim";
        private const string ControllerPath = Root + "/SuperSoldierLocomotion.controller";
        private const string LegacyPath = Root + "/Mixamo/X Bot@Jumping.fbx";

        [MenuItem("Echoryx/S05-B3/Replace Jump With Jumping Up")]
        public static void Replace()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureImporter();
            var source = AssetDatabase.LoadAllAssetsAtPath(SourcePath).OfType<AnimationClip>().Single(c => c.name == "Jump");
            var clip = BuildInPlaceJump(source);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(SourcePath).OfType<Avatar>().Single();
            Require(avatar.isValid && avatar.isHuman && clip.humanMotion, "Replacement must import as a valid Humanoid clip.");
            Require(!clip.isLooping, "Jumping Up must not loop.");

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Require(controller != null, "Locomotion controller not found.");
            var jump = controller.layers[0].stateMachine.states.Select(s => s.state).Single(s => s.name == "Jump");
            var fall = controller.layers[0].stateMachine.states.Select(s => s.state).Single(s => s.name == "Fall");
            jump.motion = clip;
            var jumpToFall = jump.transitions.Single(t => t.destinationState == fall);
            // The automatic demo holds upward velocity for the exact clip duration; avoid an exit-time delay.
            jumpToFall.hasExitTime = false;
            jumpToFall.hasFixedDuration = true;
            // ~19.5 degrees of pelvis tilt need a short 0.18s blend, not a delayed takeoff or held pose.
            jumpToFall.duration = 0.18f;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            Require(File.Exists(LegacyPath), "Keep the former Jump source for reference; it must not be deleted.");
            Require(AssetDatabase.GetAssetPath(jump.motion) == InPlacePath, "Controller Jump state does not use the preparation-preserving in-place clip.");
            WriteReport(clip, avatar, jumpToFall);
            Debug.Log($"Jumping Up installed: {clip.length:F4}s at {clip.frameRate:F1} fps; Humanoid Avatar valid; preparation-only Y retained in generated clip; Jump->Fall blend 0.18s; sources retained.");
        }

        private static AnimationClip BuildInPlaceJump(AnimationClip source)
        {
            // Full Y extraction removes the grounded crouch as well as flight. Keep only that authored body compression.
            // The curve is normalized Humanoid body position, not a world-space floor offset or a gameplay trajectory.
            var binding = AnimationUtility.GetCurveBindings(source).Single(b => b.path == "" && b.type == typeof(Animator) && b.propertyName == "RootT.y");
            var authoredY = AnimationUtility.GetEditorCurve(source, binding);
            var startY = authoredY.Evaluate(0f);
            var count = Mathf.Max(1, Mathf.RoundToInt(source.length * source.frameRate * 2f));
            var keys = new Keyframe[count + 1];
            for (var i = 0; i <= count; i++)
            {
                var time = source.length * i / count;
                keys[i] = new Keyframe(time, Mathf.Min(authoredY.Evaluate(time), startY));
            }
            var y = new AnimationCurve(keys);
            for (var i = 0; i <= count; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(y, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(y, i, AnimationUtility.TangentMode.ClampedAuto);
            }
            var copy = UnityEngine.Object.Instantiate(source);
            copy.hideFlags = HideFlags.None;
            copy.name = "Jump";
            AnimationUtility.SetEditorCurve(copy, binding, y);
            var settings = AnimationUtility.GetAnimationClipSettings(copy);
            settings.loopBlendPositionY = true;
            settings.keepOriginalPositionY = true;
            settings.heightFromFeet = false;
            settings.level = 0f;
            AnimationUtility.SetAnimationClipSettings(copy, settings);
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(InPlacePath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(copy, InPlacePath);
                existing = copy;
            }
            else
            {
                EditorUtility.CopySerialized(copy, existing);
                UnityEngine.Object.DestroyImmediate(copy);
                EditorUtility.SetDirty(existing);
            }
            // CreateAsset initially takes the filename; retain the controller/preview's stable clip identifier.
            existing.name = "Jump";
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static void ConfigureImporter()
        {
            var importer = AssetImporter.GetAtPath(SourcePath) as ModelImporter;
            Require(importer != null, "Copy Jumping Up.fbx into " + SourcePath);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.sourceAvatar = null;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.globalScale = 1f;
            importer.useFileScale = true;

            // Match the existing Mixamo source-avatar workflow: derive reference bones from the FBX rest transforms.
            var description = importer.humanDescription;
            description.skeleton = Array.Empty<SkeletonBone>();
            importer.humanDescription = description;
            importer.importAnimation = false;
            importer.SaveAndReimport();
            importer.importAnimation = true;
            importer.SaveAndReimport();

            var definitions = importer.defaultClipAnimations;
            Require(definitions.Length == 1, "Expected one animation take in Jumping Up.fbx.");
            var definition = definitions[0];
            definition.name = "Jump";
            definition.loop = false;
            definition.loopTime = false;
            definition.loopPose = false;
            definition.lockRootPositionXZ = false;
            definition.keepOriginalPositionXZ = false;
            definition.lockRootRotation = false;
            definition.keepOriginalOrientation = false;
            definition.rotationOffset = 0f;
            // In-place motor-driven jump: extract source vertical travel instead of blending away 0.61m of ascent.
            // Feet follows the raised knees and COM centers the body below the floor; Original retains its reference height.
            definition.lockRootHeightY = false;
            definition.keepOriginalPositionY = true;
            definition.heightFromFeet = false;
            definition.heightOffset = 0f;
            importer.clipAnimations = definitions;
            importer.SaveAndReimport();
        }

        private static void WriteReport(AnimationClip clip, Avatar avatar, AnimatorStateTransition transition)
        {
            var sourceAbsolutePath = Path.GetFullPath(SourcePath);
            var importer = (ModelImporter)AssetImporter.GetAtPath(SourcePath);
            var report = new JumpReplacementReport
            {
                source = SourcePath,
                sha256 = Sha256(sourceAbsolutePath),
                bytes = new FileInfo(sourceAbsolutePath).Length,
                unity = Application.unityVersion,
                clip = clip.name,
                seconds = clip.length,
                frameRate = clip.frameRate,
                approximateFrameCount = Mathf.RoundToInt(clip.length * clip.frameRate) + 1,
                sourceHumanBoneCount = importer.humanDescription.skeleton.Length,
                meshAssetCount = AssetDatabase.LoadAllAssetsAtPath(SourcePath).OfType<Mesh>().Count(),
                humanMotion = clip.humanMotion,
                avatarValid = avatar.isValid,
                avatarHuman = avatar.isHuman,
                loop = clip.isLooping,
                applyRootMotion = false,
                generatedMotion = InPlacePath,
                verticalRootMotionBakedIntoPose = true,
                verticalRootReference = "Original; only authored downward preparation retained, upward trajectory removed; Apply Root Motion disabled",
                jumpToFallExitTime = transition.hasExitTime ? transition.exitTime : -1f,
                jumpToFallTransitionSeconds = transition.duration,
                jumpToFallHasExitTime = transition.hasExitTime
            };
            Directory.CreateDirectory("docs");
            File.WriteAllText("docs/supersoldier-jump-replacement-report.json", JsonUtility.ToJson(report, true) + "\n");
        }

        private static string Sha256(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        [Serializable]
        private sealed class JumpReplacementReport
        {
            public string source, sha256, unity, clip, verticalRootReference, generatedMotion;
            public long bytes;
            public float seconds, frameRate, jumpToFallExitTime, jumpToFallTransitionSeconds;
            public int approximateFrameCount, sourceHumanBoneCount, meshAssetCount;
            public bool humanMotion, avatarValid, avatarHuman, loop, applyRootMotion, verticalRootMotionBakedIntoPose, jumpToFallHasExitTime;
        }
    }
}

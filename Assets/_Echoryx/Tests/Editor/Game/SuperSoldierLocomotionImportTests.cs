using System.Linq;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Echoryx.Game.Tests
{
    public sealed class SuperSoldierLocomotionImportTests
    {
        private const string Root = "Assets/_Echoryx/Art/Animations/SuperSoldier";
        private const string ReplacementPath = Root + "/MixamoReplacement/Jumping Up.fbx";
        private static readonly string[] Files = { "Walking", "Running", "Jumping", "Falling Idle", "Falling To Landing" };
        private static ModelImporter Importer(string file) => (ModelImporter)AssetImporter.GetAtPath(Root + "/Mixamo/X Bot@" + file + ".fbx");

        [Test]
        public void SourceAvatarsUseTheSameRestCalibrationRatherThanDifferentAnimatedPoses()
        {
            var reference = Importer(Files[0]).humanDescription;
            foreach (var file in Files)
            {
                var importer = Importer(file);
                Assert.That(importer.animationType, Is.EqualTo(ModelImporterAnimationType.Human), file);
                Assert.That(importer.avatarSetup, Is.EqualTo(ModelImporterAvatarSetup.CreateFromThisModel), file);
                Assert.That(importer.sourceAvatar, Is.Null, "Do not copy the incompatible target skeleton.");
                var avatar = AssetDatabase.LoadAllAssetsAtPath(importer.assetPath).OfType<Avatar>().Single();
                Assert.That(avatar.isHuman && avatar.isValid, Is.True, file);
                foreach (var bone in reference.skeleton.Skip(1))
                {
                    var match = importer.humanDescription.skeleton.Single(b => b.name == bone.name);
                    Assert.That(Vector3.Distance(bone.position, match.position), Is.LessThan(0.0001f), file + ": " + bone.name);
                    Assert.That(Quaternion.Angle(bone.rotation, match.rotation), Is.LessThan(0.02f), file + ": " + bone.name);
                }
                var forearm = importer.humanDescription.skeleton.Single(b => b.name == "mixamorig:LeftForeArm");
                Assert.That(Quaternion.Angle(forearm.rotation, Quaternion.identity), Is.LessThan(0.02f), file + " reference elbow must be straight.");
            }
        }

        [Test]
        public void ClipPoliciesSeparateTakeoffAndGroundContactWithoutBakingTravel()
        {
            foreach (var file in Files)
            {
                var clip = Importer(file).clipAnimations.Single();
                Assert.That(clip.lockRootPositionXZ || clip.keepOriginalOrientation, Is.False, file);
                Assert.That(clip.loopTime, Is.EqualTo(file == "Walking" || file == "Running" || file == "Falling Idle"), file);
                if (file == "Jumping")
                {
                    Assert.That(clip.firstFrame, Is.EqualTo(10f));
                    Assert.That(clip.lastFrame, Is.EqualTo(24f));
                    Assert.That(clip.lockRootHeightY, Is.False);
                }
                if (file == "Falling To Landing")
                {
                    Assert.That(clip.firstFrame, Is.EqualTo(8f));
                    Assert.That(clip.lastFrame, Is.EqualTo(32f));
                    Assert.That(clip.lockRootHeightY, Is.True);
                    Assert.That(clip.heightOffset, Is.Zero);
                }
            }
        }

        [Test]
        public void ControllerUsesSourceCadenceAndOneNonLoopingLandingWithSafeExit()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Root + "/SuperSoldierLocomotion.controller");
            var states = controller.layers[0].stateMachine.states.Select(s => s.state).ToArray();
            Assert.That(states.Select(s => s.name), Is.EquivalentTo(new[] { "Locomotion", "Jump", "Fall", "Land" }));
            var tree = (BlendTree)states.Single(s => s.name == "Locomotion").motion;
            Assert.That(tree.children.Length, Is.EqualTo(3));
            foreach (var child in tree.children.Skip(1))
            {
                var clip = (AnimationClip)child.motion;
                Assert.That(child.threshold, Is.EqualTo(clip.averageSpeed.magnitude).Within(0.001f));
                Assert.That(child.timeScale, Is.EqualTo(1f));
            }
            var land = states.Single(s => s.name == "Land");
            Assert.That(((AnimationClip)land.motion).isLooping, Is.False);
            Assert.That(land.iKOnFeet, Is.True);
            Assert.That(land.transitions[0].destinationState.name, Is.EqualTo("Fall"), "Loss of support precedes the grounded exit.");
            var resume = land.transitions.Single(t => t.destinationState.name == "Locomotion");
            Assert.That(resume.hasExitTime && resume.hasFixedDuration, Is.True);
            Assert.That(resume.exitTime, Is.EqualTo(0.95f));
            Assert.That(resume.conditions.Single().mode, Is.EqualTo(AnimatorConditionMode.If));
            foreach (var transition in states.SelectMany(s => s.transitions))
            {
                Assert.That(transition.canTransitionToSelf, Is.False);
                Assert.That(transition.interruptionSource, Is.EqualTo(TransitionInterruptionSource.Destination));
                var isApex = transition.destinationState.name == "Fall" && states.Single(s => s.name == "Jump").transitions.Contains(transition);
                Assert.That(transition.duration, Is.EqualTo(isApex ? 0.18f : 0.12f));
            }
        }

        [Test]
        public void JumpingUpIsValidHumanoidReplacementAndLegacySourceRemainsUnchanged()
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(ReplacementPath);
            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.animationType, Is.EqualTo(ModelImporterAnimationType.Human));
            Assert.That(importer.avatarSetup, Is.EqualTo(ModelImporterAvatarSetup.CreateFromThisModel));
            Assert.That(importer.sourceAvatar, Is.Null);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(ReplacementPath).OfType<Avatar>().Single();
            var clip = AssetDatabase.LoadAllAssetsAtPath(ReplacementPath).OfType<AnimationClip>().Single(c => c.name == "Jump");
            Assert.That(avatar.isValid && avatar.isHuman && clip.humanMotion, Is.True);
            Assert.That(clip.frameRate, Is.EqualTo(30f));
            Assert.That(clip.length, Is.EqualTo(25f / 30f).Within(0.002f));
            Assert.That(clip.isLooping, Is.False);
            var definition = importer.clipAnimations.Single();
            Assert.That(definition.firstFrame, Is.EqualTo(0f));
            Assert.That(definition.lastFrame, Is.EqualTo(25f));
            Assert.That(definition.loopTime, Is.False);
            Assert.That(definition.lockRootHeightY, Is.False, "Source vertical travel must not remain inside the pose during the blend to Fall.");
            Assert.That(definition.keepOriginalPositionY, Is.True, "Preserve the in-place height reference.");
            Assert.That(definition.heightFromFeet, Is.False, "Raised jump feet must not redefine the pelvis height.");
            Assert.That(definition.lockRootPositionXZ, Is.False);
            Assert.That(definition.lockRootRotation, Is.False);

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Root + "/SuperSoldierLocomotion.controller");
            var states = controller.layers[0].stateMachine.states.Select(s => s.state).ToArray();
            var jump = states.Single(s => s.name == "Jump");
            var inPlace = AssetDatabase.LoadAssetAtPath<AnimationClip>(Root + "/JumpInPlace.anim");
            Assert.That(inPlace, Is.Not.Null);
            Assert.That(jump.motion, Is.SameAs(inPlace));
            Assert.That(inPlace.humanMotion, Is.True);
            Assert.That(inPlace.length, Is.EqualTo(clip.length).Within(0.0001f));
            Assert.That(inPlace.isLooping, Is.False);
            var settings = AnimationUtility.GetAnimationClipSettings(inPlace);
            Assert.That(settings.loopBlendPositionY && settings.keepOriginalPositionY, Is.True);
            Assert.That(settings.level, Is.Zero);
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.propertyName == "RootT.y") continue;
                var original = AnimationUtility.GetEditorCurve(clip, binding);
                var retained = AnimationUtility.GetEditorCurve(inPlace, binding);
                Assert.That(retained, Is.Not.Null, binding.propertyName);
                for (var i = 0; i <= 50; i++)
                    Assert.That(retained.Evaluate(clip.length * i / 50f), Is.EqualTo(original.Evaluate(clip.length * i / 50f)).Within(0.00001f), binding.propertyName);
            }
            var legacyPath = Root + "/Mixamo/X Bot@Jumping.fbx";
            Assert.That(File.Exists(legacyPath), Is.True);
            Assert.That(AssetDatabase.LoadAllAssetsAtPath(legacyPath).OfType<AnimationClip>().Any(c => c.name == "Jump"), Is.True,
                "The previous clip remains available for reference.");
            var fall = states.Single(s => s.name == "Fall");
            var jumpToFall = jump.transitions.Single(t => t.destinationState == fall);
            Assert.That(jumpToFall.hasExitTime, Is.False, "The transition is parameter-driven without an artificial exit wait.");
            Assert.That(jumpToFall.conditions.Any(c => c.parameter == "VerticalSpeed" && c.mode == AnimatorConditionMode.Less), Is.True);
            Assert.That(jumpToFall.duration, Is.EqualTo(0.18f));
        }
    }
}

using System.Linq;
using Echoryx.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echoryx.Game.Tests
{
    public sealed class PlayerAnimationIntegrationTests
    {
        private const string Root = "Assets/_Echoryx/";

        [Test]
        public void GameplayControllerHasOnlyApprovedGroundClipsAndLocalCadence()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Root + "Art/Animations/SuperSoldier/PlayerLocomotion.controller");
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.animationClips.Select(c => c.name).OrderBy(n => n),
                Is.EquivalentTo(new[] { "IdleLoop", "Walk", "Run" }));
            Assert.That(controller.animationClips.All(c => c.humanMotion && c.isLooping), Is.True);
            Assert.That(controller.parameters.Select(p => p.name), Is.EquivalentTo(new[] { "Speed", "LocomotionRate" }));
            var machine = controller.layers[0].stateMachine;
            Assert.That(machine.states.Length, Is.EqualTo(1));
            var state = machine.defaultState;
            Assert.That(state.iKOnFeet && state.speedParameterActive, Is.True);
            Assert.That(state.speedParameter, Is.EqualTo("LocomotionRate"));
            var tree = (BlendTree)state.motion;
            Assert.That(tree.children.Select(c => c.threshold), Is.EqualTo(new[] { 0f, 1.7009519f, 4.3729978f }).Within(0.002f));
            Assert.That(PlayerAnimationDriver.Cadence(6f, 4.3729978f), Is.EqualTo(1.37206f).Within(0.001f));
            Assert.That(PlayerAnimationDriver.Cadence(1.7f, 4.3729978f), Is.EqualTo(1f));
        }

        [Test]
        public void VisualHasValidatedAvatarIdentityHeadingAndNoPhysics()
        {
            var visual = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Characters/SuperSoldierVisual.prefab");
            var animator = visual.GetComponentInChildren<Animator>();
            Assert.That(animator.avatar.isValid && animator.avatar.isHuman, Is.True);
            Assert.That(animator.applyRootMotion, Is.False);
            Assert.That(animator.updateMode, Is.EqualTo(AnimatorUpdateMode.Normal));
            Assert.That(animator.transform.localScale.x, Is.EqualTo(0.98656774f).Within(1e-6f));
            Assert.That(Quaternion.Angle(animator.transform.localRotation, Quaternion.identity), Is.LessThan(0.001f));
            Assert.That(animator.transform.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(visual.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(visual.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
            Assert.That(visual.GetComponentsInChildren<Camera>(true), Is.Empty);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Art/Characters/SihaGamesSuperSoldier/Idle.fbx");
            Assert.That(animator.avatar, Is.SameAs(source.GetComponent<Animator>().avatar));
            Assert.That(visual.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(r => r.sharedMaterials),
                Is.EqualTo(source.GetComponentsInChildren<SkinnedMeshRenderer>().SelectMany(r => r.sharedMaterials)));
        }

        [Test]
        public void LabKeepsOneControllerAndExactSharedMotorConfiguration()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var scene = EditorSceneManager.OpenScene(Root + "Scenes/PlayerAnimationIntegrationLab.unity");
                var player = scene.GetRootGameObjects().Single(r => r.name == "Player");
                var original = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Player.prefab");
                var cc = player.GetComponent<CharacterController>();
                var old = original.GetComponent<CharacterController>();
                Assert.That(player.GetComponentsInChildren<CharacterController>(true).Length, Is.EqualTo(1));
                Assert.That(cc.height, Is.EqualTo(old.height));
                Assert.That(cc.radius, Is.EqualTo(old.radius));
                Assert.That(cc.center, Is.EqualTo(old.center));
                Assert.That(cc.stepOffset, Is.EqualTo(old.stepOffset));
                Assert.That(cc.skinWidth, Is.EqualTo(old.skinWidth));
                Assert.That(cc.slopeLimit, Is.EqualTo(old.slopeLimit));
                Assert.That(cc.minMoveDistance, Is.EqualTo(old.minMoveDistance));
                var motor = new SerializedObject(player.GetComponent<PlayerMotor>());
                var source = new SerializedObject(original.GetComponent<PlayerMotor>());
                foreach (var field in new[] { "moveSpeed", "acceleration", "deceleration", "airControl", "turnSpeed", "jumpHeight", "gravity", "coyoteTime", "jumpBuffer", "groundedPull" })
                    Assert.That(motor.FindProperty(field).floatValue, Is.EqualTo(source.FindProperty(field).floatValue), field);
                var driver = new SerializedObject(player.GetComponent<PlayerAnimationDriver>());
                Assert.That(driver.FindProperty("motor").objectReferenceValue, Is.SameAs(player.GetComponent<PlayerMotor>()));
                Assert.That(driver.FindProperty("animator").objectReferenceValue, Is.SameAs(player.GetComponentInChildren<Animator>()));
                // The shared Player now carries the Robot visual (its own Animator); it still has no SuperSoldier
                // driver, and this lab keeps its single SuperSoldier Animator by hiding the robot in the instance.
                Assert.That(original.GetComponent<PlayerAnimationDriver>(), Is.Null, "Shared Player has no SuperSoldier driver.");
                var robot = original.transform.Find("VisualRoot/Robot Visual");
                Assert.That(original.GetComponentsInChildren<Animator>(true).All(a => robot != null && a.transform.IsChildOf(robot)),
                    Is.True, "The only Animators on the shared Player belong to the Robot visual.");
            }
            finally
            {
                if (setup.Any(s => s.isActive && s.isLoaded && !string.IsNullOrEmpty(s.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
    }
}

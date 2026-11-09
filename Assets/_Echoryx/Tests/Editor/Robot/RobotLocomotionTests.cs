using System.Linq;
using Echoryx.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Echoryx.Robot.Tests
{
    public sealed class RobotLocomotionTests
    {
        private const string ControllerPath = "Assets/_Echoryx/Art/Animations/Robot/PlayerLocomotion.controller";
        private const string VisualPath = "Assets/_Echoryx/Prefabs/Characters/RobotVisual.prefab";
        private const string EchoVisualPath = "Assets/_Echoryx/Prefabs/Characters/RobotEchoVisual.prefab";

        [Test]
        public void ControllerHasSpeedBlendTreeIdleWalkRunCalibratedToPlayerMaximum()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.parameters.Any(p => p.name == "Speed" && p.type == AnimatorControllerParameterType.Float));
            Assert.That(controller.parameters.Any(p => p.name == "Grounded" && p.type == AnimatorControllerParameterType.Bool && p.defaultBool));
            var machine = controller.layers[0].stateMachine;
            Assert.That(machine.defaultState.name, Is.EqualTo("Locomotion"));
            var tree = (BlendTree)machine.defaultState.motion;
            Assert.That(tree.blendType, Is.EqualTo(BlendTreeType.Simple1D));
            Assert.That(tree.blendParameter, Is.EqualTo("Speed"));
            var children = tree.children;
            Assert.That(children.Select(c => c.motion.name), Is.EqualTo(new[] { "StaticIdle", "Walk", "Run" }));
            Assert.That(children[0].threshold, Is.EqualTo(0f));
            Assert.That(children[1].threshold, Is.InRange(1.5f, 4f));
            Assert.That(children[2].threshold, Is.EqualTo(6f), "Run is calibrated to the 6 m/s player maximum.");
            Assert.That(machine.states.Select(s => s.state.name), Is.SupersetOf(new[] { "Jump_Start", "Jump_Air", "Jump_Landing" }));
        }

        [Test]
        public void RobotVisualHasNoPhysicsNoRootMotionAndTheDriver()
        {
            foreach (var path in new[] { VisualPath, EchoVisualPath })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.That(prefab, Is.Not.Null, path);
                Assert.That(prefab.GetComponentsInChildren<Collider>(true), Is.Empty, path);
                Assert.That(prefab.GetComponentsInChildren<Rigidbody>(true), Is.Empty, path);
                var animator = prefab.GetComponentInChildren<Animator>();
                Assert.That(animator.applyRootMotion, Is.False, path);
                Assert.That(animator.runtimeAnimatorController, Is.SameAs(AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath)));
                Assert.That(prefab.GetComponent<RobotAnimationDriver>(), Is.Not.Null, path);
                Assert.That(animator.transform.localScale.y, Is.EqualTo(1.1f).Within(0.001f));
            }
        }

        [Test]
        public void EchoVariantUsesTheCyanTranslucentHologramEverywhere()
        {
            var hologram = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Echoryx/Materials/EchoHologram.mat");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EchoVisualPath);
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                Assert.That(renderer.sharedMaterials, Is.All.SameAs(hologram), renderer.name);
                Assert.That(renderer.shadowCastingMode, Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.Off));
            }
            Assert.That(hologram.GetColor("_BaseColor").a, Is.LessThan(1f));
        }

        [Test]
        public void PlayerAndEchoPrefabsShowTheRobotAndKeepTheirLogic()
        {
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Echoryx/Prefabs/Player.prefab");
            var visualRoot = player.transform.Find("VisualRoot");
            Assert.That(visualRoot.Find("Robot Visual"), Is.Not.Null);
            Assert.That(visualRoot.Find("Robot Visual").gameObject.activeSelf, Is.True);
            Assert.That(visualRoot.Find("Capsule Placeholder").gameObject.activeSelf, Is.False);
            var controller = player.GetComponent<CharacterController>();
            Assert.That(controller.height, Is.EqualTo(2f));
            Assert.That(controller.radius, Is.EqualTo(0.4f));
            Assert.That(player.GetComponent<PlayerMotor>(), Is.Not.Null);

            var echo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Echoryx/Prefabs/EchoActor.prefab");
            Assert.That(echo.transform.Find("Robot Echo Visual"), Is.Not.Null);
            Assert.That(echo.transform.Find("Hologram Visual").gameObject.activeSelf, Is.False);
            Assert.That(echo.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(echo.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
        }

        [Test]
        public void SpeedSmoothingApproachesTheTargetWithoutOvershoot()
        {
            var velocity = 0f;
            var value = 0f;
            for (var i = 0; i < 60; i++)
            {
                var next = RobotAnimationDriver.Smooth(value, 6f, ref velocity, 0.08f, 1f / 60f);
                Assert.That(next, Is.GreaterThanOrEqualTo(value));
                Assert.That(next, Is.LessThanOrEqualTo(6f + 1e-4f));
                value = next;
            }
            Assert.That(value, Is.EqualTo(6f).Within(0.05f));
            velocity = 0f;
            Assert.That(RobotAnimationDriver.Smooth(1f, 4f, ref velocity, 0f, 0.016f), Is.EqualTo(4f), "No damping snaps to the target.");
        }

        [Test]
        public void PlaybackRateMatchesRealSpeedToTheNominalStrideOfTheBlendedClips()
        {
            const float walk = 2f, run = 3.9f, runThreshold = 6f;
            float Rate(float speed) => RobotAnimationDriver.SpeedMatchRate(speed, walk, run, runThreshold, 0.6f, 1.6f);
            Assert.That(Rate(0f), Is.EqualTo(1f));
            Assert.That(Rate(walk), Is.EqualTo(1f).Within(1e-4f), "Walking at the walk stride speed plays at rate 1.");
            Assert.That(Rate(runThreshold), Is.EqualTo(runThreshold / run).Within(1e-4f), "Max speed stretches Run to the real speed.");
            Assert.That(Rate(4f), Is.EqualTo(4f / Mathf.Lerp(walk, run, 0.5f)).Within(1e-4f), "Between clips the nominal speed is blended like the tree.");
            Assert.That(Rate(0.3f), Is.EqualTo(0.6f), "Clamped at the minimum rate.");
            Assert.That(Rate(20f), Is.EqualTo(1.6f), "Clamped at the maximum rate.");
            Assert.That(Rate(5f), Is.GreaterThan(Rate(3f)), "Faster movement never plays slower.");
        }

        [Test]
        public void JumpTransitionIsImmediateAndRunIsNotStaticallyScaled()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            var machine = controller.layers[0].stateMachine;
            var locomotion = machine.defaultState;
            var tree = (BlendTree)locomotion.motion;
            Assert.That(tree.children.All(c => Mathf.Approximately(c.timeScale, 1f)), Is.True, "Speed matching is dynamic, not a fixed timeScale.");
            var toJump = locomotion.transitions.Single(t => t.destinationState != null && t.destinationState.name == "Jump_Start");
            Assert.That(toJump.hasExitTime, Is.False);
            Assert.That(toJump.duration, Is.LessThanOrEqualTo(0.05f));
            Assert.That(toJump.conditions.Single().parameter, Is.EqualTo("Grounded"));
            Assert.That(toJump.conditions.Single().mode, Is.EqualTo(AnimatorConditionMode.IfNot));
            var driver = new SerializedObject(AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath).GetComponent<RobotAnimationDriver>());
            Assert.That(driver.FindProperty("walkNominalSpeed").floatValue, Is.GreaterThan(0.5f));
            Assert.That(driver.FindProperty("runNominalSpeed").floatValue, Is.GreaterThan(driver.FindProperty("walkNominalSpeed").floatValue));
        }
    }
}

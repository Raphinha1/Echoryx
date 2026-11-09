using System.Collections;
using Echoryx.Player;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Echoryx.Robot.PlayTests
{
    /// <summary>Plays RobotIntegrationLab (Player.prefab untouched; robot attached in the scene) with real input.</summary>
    public sealed class RobotLocomotionPlayChecks
    {
        private Keyboard keyboard;
        private Transform basis;
        private PlayerMotor player;
        private RobotAnimationDriver driver;
        private Animator animator;
        private InputSettings originalSettings;
        private InputSettings testSettings;
        private HideFlags originalFlags;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Echoryx/Scenes/RobotIntegrationLab.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("Lab checks load the authored scene in Unity Editor.");
#endif
            yield return null;
            Time.captureDeltaTime = 1f / 60f;
            originalSettings = InputSystem.settings;
            originalFlags = originalSettings.hideFlags;
            originalSettings.hideFlags = HideFlags.None;
            testSettings = Object.Instantiate(originalSettings);
            testSettings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = testSettings;
            keyboard = InputSystem.AddDevice<Keyboard>();
            player = Object.FindFirstObjectByType<PlayerMotor>();
            basis = new GameObject("Test movement basis").transform;
            player.SetCameraReference(basis);
            driver = player.GetComponentInChildren<RobotAnimationDriver>();
            animator = driver.GetComponentInChildren<Animator>();
            yield return Frames(5);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (basis != null) Object.Destroy(basis.gameObject);
            if (originalSettings != null) { InputSystem.settings = originalSettings; originalSettings.hideFlags = originalFlags; }
            if (testSettings != null) Object.Destroy(testSettings);
            Time.captureDeltaTime = 0f;
            yield return null;
        }

        private static float Weight(Animator animator, string clip)
        {
            var total = 0f;
            foreach (var info in animator.GetCurrentAnimatorClipInfo(0)) if (info.clip.name == clip) total += info.weight;
            return total;
        }

        [UnityTest]
        public IEnumerator RunsWhenMovingStopsToIdleAndNeverMovesThePlayerItself()
        {
            Assert.That(Weight(animator, "StaticIdle"), Is.GreaterThan(0.9f), "Starts idle.");
            var visual = driver.transform;
            var localBefore = visual.localPosition;
            Keys(Key.W);
            yield return Frames(70);
            Assert.That(driver.Speed, Is.GreaterThan(5f), "Real planar speed reaches the 6 m/s run.");
            Assert.That(Weight(animator, "Run"), Is.GreaterThan(0.8f));
            Assert.That(visual.localPosition, Is.EqualTo(localBefore), "No root motion: the visual never moves the player.");
            Assert.That(animator.applyRootMotion, Is.False);
            Keys();
            yield return Frames(60);
            Assert.That(driver.Speed, Is.LessThan(0.05f));
            Assert.That(Weight(animator, "StaticIdle"), Is.GreaterThan(0.9f), "Stops back to idle.");
        }

        [UnityTest]
        public IEnumerator JumpPlaysAirPoseThenLandsBackIntoLocomotion()
        {
            var sawAir = false;
            Keys(Key.Space);
            yield return Frames(2);
            Keys();
            for (var frame = 0; frame < 40; frame++)
            {
                sawAir |= !driver.Grounded && animator.GetCurrentAnimatorStateInfo(0).IsName("Jump_Air");
                if (!driver.Grounded && frame > 15) break;
                yield return null;
            }
            Assert.That(sawAir || Weight(animator, "Jump_Air") > 0.1f || Weight(animator, "Jump_Start") > 0.1f, Is.True, "Jump clips play in the air.");
            yield return Frames(90);
            Assert.That(driver.Grounded, Is.True);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"), Is.True, "Back to locomotion after landing.");
        }

        [UnityTest]
        public IEnumerator PushingAgainstAWallDoesNotAnimateRunning()
        {
            basis.rotation = Quaternion.LookRotation(Vector3.right);
            Keys(Key.W);
            yield return Frames(120);
            Assert.That(player.transform.position.x, Is.LessThan(3.75f), "Blocked by the lab wall.");
            Assert.That(driver.Speed, Is.LessThan(0.8f), "Blocked motion is not animated as running.");
            Assert.That(Weight(animator, "Run"), Is.LessThan(0.2f));
            Keys();
        }

        private void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        private static IEnumerator Frames(int count) { for (var i = 0; i < count; i++) yield return null; }
    }
}

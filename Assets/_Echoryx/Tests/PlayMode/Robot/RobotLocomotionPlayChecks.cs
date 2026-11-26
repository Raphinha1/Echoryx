using System.Collections;
using System.Reflection;
using Echoryx.Player;
using Echoryx.Rewind.Platforms;
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
        private readonly System.Collections.Generic.List<GameObject> spawned = new System.Collections.Generic.List<GameObject>();

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
            foreach (var item in spawned) if (item != null) Object.Destroy(item);
            spawned.Clear();
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
        public IEnumerator JumpTakeoffStartsTheJumpStateInTheSameFrameThePlayerLeavesTheGround()
        {
            var controller = player.GetComponent<CharacterController>();
            Assert.That(controller.isGrounded, Is.True);
            var startY = player.transform.position.y;
            Keys(Key.Space);
            var takeoffFrame = -1;
            for (var frame = 0; frame < 10; frame++)
            {
                yield return null;
                if (player.transform.position.y > startY + 0.05f) { takeoffFrame = frame; break; }
            }
            Keys();
            Assert.That(takeoffFrame, Is.GreaterThanOrEqualTo(0), "The motor accepted the jump.");
            Assert.That(driver.Grounded, Is.False, "Airborne in the very frame the motor lifts off (no grace delay).");
            var inJumpStart = animator.GetCurrentAnimatorStateInfo(0).IsName("Jump_Start") || animator.GetNextAnimatorStateInfo(0).IsName("Jump_Start");
            for (var extra = 0; extra < 2 && !inJumpStart; extra++)
            {
                yield return null;
                inJumpStart = animator.GetCurrentAnimatorStateInfo(0).IsName("Jump_Start") || animator.GetNextAnimatorStateInfo(0).IsName("Jump_Start");
            }
            Assert.That(inJumpStart, Is.True, "Jump_Start is playing or blending in within ~2 frames of takeoff.");
            yield return Frames(90);
            Assert.That(driver.Grounded, Is.True);
        }

        [UnityTest]
        public IEnumerator FootSpeedFollowsRealSpeedWhileRunning()
        {
            Keys(Key.W);
            yield return Frames(70);
            Assert.That(driver.Speed, Is.GreaterThan(5f));
            Assert.That(driver.PlaybackRate, Is.EqualTo(RobotAnimationDriver.SpeedMatchRate(driver.Speed, 1.99f, 3.92f, 6f, 0.6f, 1.6f)).Within(0.15f));
            Assert.That(animator.speed, Is.EqualTo(driver.PlaybackRate).Within(1e-4f));
            Keys();
            yield return Frames(60);
            Assert.That(animator.speed, Is.EqualTo(1f).Within(1e-4f), "Idle plays at its authored rate.");
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

        [UnityTest]
        public IEnumerator RidingPlatformExcludesTransportAndStillMeasuresWalking()
        {
            var controller = player.GetComponent<CharacterController>();
            var platform = BuildTransportPlatform(new Vector3(-3.5f, 1f, 0f), new Vector3(-3.5f, 1f, 6f), 1.5f);
            var top = platform.transform.position.y + 0.15f;
            player.Respawn(platform.transform.position + Vector3.up * 0.15f, Quaternion.identity);
            yield return Frames(8);
            var start = player.transform.position;
            yield return Frames(30);
            Assert.That(controller.isGrounded, Is.True);
            Assert.That(player.transform.position.y, Is.EqualTo(top).Within(0.2f), "Standing on the platform, not on the floor.");
            Assert.That(player.transform.position.z - start.z, Is.GreaterThan(0.65f), "The platform carries the player.");
            Assert.That(player.PlanarSpeed, Is.LessThan(0.02f), "Transport is not the player's own speed.");
            Assert.That(driver.Speed, Is.LessThan(0.05f));
            Assert.That(animator.GetFloat("Speed"), Is.LessThan(0.05f));
            Assert.That(Weight(animator, "StaticIdle"), Is.GreaterThan(0.9f), "A carried robot stays idle.");
            Keys(Key.D);
            yield return Frames(10);
            Assert.That(controller.isGrounded, Is.True);
            Assert.That(player.transform.position.y, Is.EqualTo(top).Within(0.2f), "Still on the platform while walking.");
            Assert.That(player.PlanarSpeed, Is.GreaterThan(1f), "Walking across the platform is measured.");
            Assert.That(player.PlanarVelocity.z, Is.EqualTo(0f).Within(0.05f), "The platform's +Z travel stays excluded.");
            Assert.That(driver.Speed, Is.GreaterThan(0.5f));
            Assert.That(animator.GetFloat("Speed"), Is.EqualTo(driver.Speed).Within(0.002f));
        }

        [UnityTest]
        public IEnumerator BlockedTransportAndWallPushDoNotInventVelocity()
        {
            var carrier = player.GetComponent<PlayerPlatformCarrier>();
            // Control: in open floor the offered transport really moves the player, yet is not own speed.
            player.Respawn(new Vector3(0f, 0.05f, -5f), Quaternion.identity);
            yield return Frames(5);
            var before = player.transform.position;
            for (var i = 0; i < 10; i++)
            {
                carrier.OfferMotion(Vector3.right * 0.025f);
                yield return null;
                Assert.That(player.PlanarSpeed, Is.LessThan(0.02f));
            }
            Assert.That(player.transform.position.x - before.x, Is.GreaterThan(0.2f), "Offered transport is consumed.");

            // Flat contact with the middle of the lab wall (x face at 3.75, z from 0 to 12).
            player.Respawn(new Vector3(2.8f, 0.05f, 6f), Quaternion.identity);
            yield return Frames(5);
            Keys(Key.D);
            yield return Frames(50);
            Assert.That(player.transform.position.x, Is.InRange(2.9f, 3.75f), "Pressed against the lab wall.");
            for (var i = 0; i < 20; i++)
            {
                carrier.OfferMotion(Vector3.right * 0.025f);
                yield return null;
                Assert.That(player.PlanarSpeed, Is.LessThan(0.05f));
            }
            Keys();
            yield return Frames(30);
            for (var i = 0; i < 10; i++)
            {
                carrier.OfferMotion(Vector3.right * 0.025f);
                yield return null;
                Assert.That(player.PlanarSpeed, Is.LessThan(0.05f));
                Assert.That(driver.Speed, Is.LessThan(0.05f));
                Assert.That(animator.GetFloat("Speed"), Is.LessThan(0.05f));
            }
        }

        /// <summary>A real RewindMovingPlatform wired at run time, so the authored robot lab stays unchanged.</summary>
        private RewindMovingPlatform BuildTransportPlatform(Vector3 from, Vector3 to, float speed)
        {
            var start = new GameObject("Platform Start");
            start.transform.position = from;
            var end = new GameObject("Platform End");
            end.transform.position = to;
            var root = new GameObject("Transport Test Platform");
            spawned.Add(start);
            spawned.Add(end);
            spawned.Add(root);
            root.SetActive(false); // Wire before Awake snaps the platform to its start point.
            var surface = root.AddComponent<BoxCollider>();
            surface.size = new Vector3(2.5f, 0.3f, 2.5f);
            var platform = root.AddComponent<RewindMovingPlatform>();
            Wire(platform, "startPoint", start.transform);
            Wire(platform, "endPoint", end.transform);
            Wire(platform, "surface", surface);
            Wire(platform, "passenger", player.GetComponent<PlayerPlatformCarrier>());
            Wire(platform, "speed", speed);
            root.SetActive(true);
            return platform;
        }

        private static void Wire(RewindMovingPlatform platform, string name, object value)
        {
            var field = typeof(RewindMovingPlatform).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "RewindMovingPlatform field " + name);
            field.SetValue(platform, value);
        }

        private void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        private static IEnumerator Frames(int count) { for (var i = 0; i < count; i++) yield return null; }
    }
}

using System.Collections;
using System.IO;
using System.Linq;
using Echoryx.Player;
using Echoryx.Rewind.Platforms;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Echoryx.Game.PlayTests
{
    public sealed class PlayerAnimationIntegrationChecks
    {
        private PlayerMotor motor;
        private Animator animator;
        private PlayerAnimationDriver driver;
        private Keyboard keyboard;
        private Mouse mouse;
        private Gamepad gamepad;
        private InputSettings originalSettings;
        private InputSettings settings;
        private HideFlags originalFlags;
        private Transform basis;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Echoryx/Scenes/PlayerAnimationIntegrationLab.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("Isolated lab requires Unity Editor.");
#endif
            Time.captureDeltaTime = 1f / 60f;
            yield return null;
            originalSettings = InputSystem.settings;
            originalFlags = originalSettings.hideFlags;
            originalSettings.hideFlags = HideFlags.None;
            settings = Object.Instantiate(originalSettings);
            settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = settings;
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            gamepad = InputSystem.AddDevice<Gamepad>();
            motor = GameObject.Find("Player").GetComponent<PlayerMotor>();
            driver = motor.GetComponent<PlayerAnimationDriver>();
            animator = motor.GetComponentInChildren<Animator>();
            basis = new GameObject("Fixed test basis").transform;
            motor.SetCameraReference(basis);
            yield return Frames(5);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            if (gamepad != null) InputSystem.RemoveDevice(gamepad);
            if (originalSettings != null) { InputSystem.settings = originalSettings; originalSettings.hideFlags = originalFlags; }
            if (settings != null) Object.Destroy(settings);
            if (basis != null) Object.Destroy(basis.gameObject);
            Time.captureDeltaTime = 0f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator IdleWalkRunUseResolvedSpeedAndCadenceWithoutVisualDrift()
        {
            var local = animator.transform.localPosition;
            Assert.That(animator.GetFloat("Speed"), Is.LessThan(0.01f));
            AssertClip("IdleLoop");
            Capture("idle");
            // Default stick deadzone remaps 0.36 into about 0.28 => 1.7 m/s.
            InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0f, 0.36f) });
            yield return Frames(40);
            Assert.That(motor.PlanarSpeed, Is.InRange(1.3f, 2.3f));
            Assert.That(animator.GetFloat("Speed"), Is.EqualTo(motor.PlanarSpeed).Within(0.002f));
            AssertClip("Walk");
            Capture("walk");
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            Keys(Key.W);
            yield return Frames(35);
            Assert.That(motor.PlanarSpeed, Is.EqualTo(6f).Within(0.02f));
            Assert.That(animator.GetFloat("LocomotionRate"), Is.EqualTo(6f / 4.3729978f).Within(0.01f));
            AssertClip("Run");
            Capture("run");
            Assert.That(animator.transform.localPosition, Is.EqualTo(local));
            Assert.That(Quaternion.Angle(animator.transform.localRotation, Quaternion.identity), Is.LessThan(0.001f));
            Assert.That(animator.applyRootMotion, Is.False);
            // Anatomical right x up points +Z: source heading follows Player, not lab yaw 180.
            var across = animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position - animator.GetBoneTransform(HumanBodyBones.LeftUpperArm).position;
            var torsoUp = animator.GetBoneTransform(HumanBodyBones.Chest).position - animator.GetBoneTransform(HumanBodyBones.Hips).position;
            Assert.That(Vector3.Dot(Vector3.Cross(across, torsoUp).normalized, motor.transform.forward), Is.GreaterThan(0.7f));
            Keys();
            yield return Frames(30);
            AssertClip("IdleLoop");
        }

        [UnityTest]
        public IEnumerator DiagonalAndDirectionChangesPreserveSpeedAndHeading()
        {
            Keys(Key.W, Key.D);
            yield return Frames(35);
            Assert.That(motor.PlanarSpeed, Is.EqualTo(6f).Within(0.02f));
            Assert.That(Vector3.Angle(motor.transform.forward, new Vector3(1f, 0f, 1f)), Is.LessThan(0.1f));
            Keys(Key.S);
            yield return Frames(45);
            Assert.That(Vector3.Angle(motor.transform.forward, Vector3.back), Is.LessThan(0.1f));
            Assert.That(motor.PlanarSpeed, Is.EqualTo(6f).Within(0.02f));
            Assert.That(Quaternion.Angle(animator.transform.rotation, motor.transform.rotation), Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator WallPushIsIdleAndSlidingUsesOnlyResolvedMotion()
        {
            motor.Respawn(new Vector3(0f, 0.02f, 9f), Quaternion.identity);
            yield return Frames(5);
            Keys(Key.W);
            yield return Frames(60);
            Assert.That(motor.PlanarSpeed, Is.LessThan(0.05f));
            Assert.That(animator.GetFloat("Speed"), Is.LessThan(0.05f));
            AssertClip("IdleLoop");
            var before = motor.transform.position;
            Keys(Key.W, Key.D);
            yield return Frames(30);
            Assert.That(motor.PlanarSpeed, Is.InRange(3.8f, 4.6f));
            Assert.That(motor.transform.position.x - before.x, Is.GreaterThan(1f));
            Assert.That(motor.transform.position.z - before.z, Is.LessThan(0.05f));
        }

        [UnityTest]
        public IEnumerator RidingPlatformExcludesTransportAndStillMeasuresWalking()
        {
            var platform = GameObject.Find("Transport Test Platform").GetComponent<RewindMovingPlatform>();
            motor.Respawn(platform.transform.position + Vector3.up * 0.15f, Quaternion.identity);
            yield return Frames(8);
            var start = motor.transform.position;
            yield return Frames(30);
            Assert.That(motor.GetComponent<CharacterController>().isGrounded, Is.True);
            Assert.That(motor.transform.position.z - start.z, Is.GreaterThan(0.65f));
            Assert.That(motor.PlanarSpeed, Is.LessThan(0.02f));
            AssertClip("IdleLoop");
            Capture("platform-idle");
            Keys(Key.D);
            yield return Frames(10);
            Assert.That(motor.PlanarSpeed, Is.GreaterThan(1f));
            Assert.That(animator.GetFloat("Speed"), Is.EqualTo(motor.PlanarSpeed).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator BlockedTransportAndWallPushDoNotInventVelocity()
        {
            var carrier = motor.GetComponent<PlayerPlatformCarrier>();
            motor.Respawn(new Vector3(0f, 0.02f, 9f), Quaternion.identity);
            yield return Frames(5);
            Keys(Key.W);
            yield return Frames(50);
            for (var i = 0; i < 20; i++)
            {
                carrier.OfferMotion(Vector3.forward * 0.025f);
                yield return null;
                Assert.That(motor.PlanarSpeed, Is.LessThan(0.05f));
            }
            Keys();
            yield return Frames(30);
            for (var i = 0; i < 10; i++)
            {
                carrier.OfferMotion(Vector3.forward * 0.025f);
                yield return null;
                Assert.That(animator.GetFloat("Speed"), Is.LessThan(0.05f));
            }
        }

        [UnityTest]
        public IEnumerator DriverDoesNotChangePhysicsAndStaleMotorSampleBecomesIdle()
        {
            yield return Travel(true);
            var enabledEnd = motor.transform.position;
            yield return Travel(false);
            Assert.That(Vector3.Distance(enabledEnd, motor.transform.position), Is.LessThan(0.001f));
            driver.enabled = true;
            motor.enabled = false;
            yield return Frames(3);
            Assert.That(animator.GetFloat("Speed"), Is.EqualTo(0f));
        }

        private IEnumerator Travel(bool animate)
        {
            Keys();
            yield return Frames(2);
            motor.Respawn(new Vector3(-8f, 0.05f, -8f), Quaternion.identity);
            driver.enabled = animate;
            yield return Frames(5);
            Keys(Key.W);
            yield return Frames(45);
        }

        private void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        private static IEnumerator Frames(int count) { for (var i = 0; i < count; i++) yield return null; }
        private void AssertClip(string name) => Assert.That(animator.GetCurrentAnimatorClipInfo(0).Any(c => c.clip.name == name && c.weight > 0.5f), Is.True, name);

        private void Capture(string phase)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            var camera = new GameObject("Temporary front inspection").AddComponent<Camera>();
            var target = new RenderTexture(1000, 800, 24);
            var previous = RenderTexture.active;
            Texture2D image = null;
            try
            {
                camera.transform.position = motor.transform.position + motor.transform.forward * 4f + Vector3.up * 1.5f;
                camera.transform.LookAt(motor.transform.position + Vector3.up);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.03f, 0.04f, 0.06f);
                target.Create();
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
                image.Apply();
                Directory.CreateDirectory("docs/images");
                File.WriteAllBytes("docs/images/s05-b42-" + phase + ".png", image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                if (image != null) Object.Destroy(image);
                target.Release();
                Object.Destroy(target);
                Object.Destroy(camera.gameObject);
            }
        }
    }
}

using System.Collections;
using System.IO;
using Echoryx.Player;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Echoryx.Game.PlayTests
{
    /// <summary>Isolated S05 checks: existing motor, real colliders and actual Cinemachine rig.</summary>
    public sealed class VisualReferenceRoomPlayChecks
    {
        private Keyboard keyboard;
        private Mouse mouse;
        private PlayerMotor player;
        private PlayerCameraOrbit orbit;
        private Camera camera;
        private InputSettings previousSettings, settings;
        private HideFlags previousFlags;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Echoryx/Scenes/VisualReferenceRoom.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("Reference room checks load the authored scene through Unity Editor.");
#endif
            yield return null;
            Time.captureDeltaTime = 1f / 60f;
            previousSettings = InputSystem.settings;
            previousFlags = previousSettings.hideFlags;
            previousSettings.hideFlags = HideFlags.None;
            settings = Object.Instantiate(previousSettings);
            settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = settings;
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            player = GameObject.Find("Player").GetComponent<PlayerMotor>();
            orbit = GameObject.Find("Camera Orbit Target").GetComponent<PlayerCameraOrbit>();
            camera = GameObject.Find("Main Camera").GetComponent<Camera>();
#if UNITY_EDITOR
            // Batchmode has no focused Game View. Only this runtime test instance is changed.
            var data = new SerializedObject(orbit);
            data.FindProperty("lockCursorOnPlay").boolValue = false;
            data.ApplyModifiedPropertiesWithoutUndo();
#endif
            yield return Frames(5);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            if (previousSettings != null)
            {
                InputSystem.settings = previousSettings;
                previousSettings.hideFlags = previousFlags;
            }
            if (settings != null) Object.Destroy(settings);
            Time.captureDeltaTime = 0f;
            Cursor.lockState = CursorLockMode.None;
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerCrossesOpenFrameAndCannotCrossClosedDoorOrWalls()
        {
            Keys(Key.W);
            yield return Frames(200);
            Keys();
            yield return Frames(20);
            Assert.That(player.transform.position.z, Is.InRange(6.5f, 7.6f), "Walk through the entrance, stop at the closed panel.");
            Assert.That(player.GetComponent<CharacterController>().isGrounded, Is.True);
            Assert.That(Mathf.Abs(player.transform.position.y), Is.LessThan(0.15f), "No floor seam fall or unwanted step.");
            Keys(Key.A);
            yield return Frames(180);
            Keys();
            yield return Frames(20);
            Assert.That(player.transform.position.x, Is.InRange(-7.7f, -6f), "A real side wall/corner stops the player.");
            Keys(Key.Space);
            yield return Frames(1);
            Keys();
            var peak = player.transform.position.y;
            for (var frame = 0; frame < 90; frame++)
            {
                peak = Mathf.Max(peak, player.transform.position.y);
                yield return null;
            }
            Assert.That(peak, Is.GreaterThan(1f), "Jump actually executed.");
            Assert.That(player.GetComponent<CharacterController>().isGrounded, Is.True);
            Assert.That(Mathf.Abs(player.transform.position.y), Is.LessThan(0.15f));
        }

        [UnityTest]
        public IEnumerator CameraFollowsOrbitsAndAvoidsTheRoomWall()
        {
            player.Respawn(new Vector3(0f, 0.05f, -3f), Quaternion.identity);
            yield return Frames(35);
            Assert.That(Vector3.Distance(orbit.transform.position, player.transform.Find("CameraAnchor").position), Is.LessThan(0.03f));
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null) CaptureActualCamera();
            player.Respawn(new Vector3(-6.9f, 0.05f, 0f), Quaternion.identity);
            yield return Frames(20);
            InputSystem.QueueDeltaStateEvent(mouse.delta, new Vector2(600f, 0f));
            yield return Frames(30);
            Assert.That(Mathf.DeltaAngle(0f, orbit.transform.eulerAngles.y), Is.EqualTo(90f).Within(2f), "Real mouse action rotates the orbit.");
            Assert.That(camera.transform.position.x, Is.GreaterThan(-7.95f), "Cinemachine must pull the camera in before the west wall.");
            Assert.That(Vector3.Distance(camera.transform.position, orbit.transform.position), Is.LessThan(4.5f));
            Assert.That(Physics.CheckSphere(camera.transform.position, 0.1f, ~0, QueryTriggerInteraction.Ignore), Is.False,
                "The camera must not end inside the solid wall or player.");
        }

        private void CaptureActualCamera()
        {
            var target = new RenderTexture(1600, 1000, 24);
            var pixels = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                target.Create();
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0);
                pixels.Apply();
                Directory.CreateDirectory("docs/images");
                File.WriteAllBytes("docs/images/s05-a2-third-person.png", pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
                Object.Destroy(target);
                Object.Destroy(pixels);
            }
        }
        private void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        private static IEnumerator Frames(int count) { for (var i = 0; i < count; i++) yield return null; }
    }
}

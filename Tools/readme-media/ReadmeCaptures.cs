// Capture harness for the README images. NOT part of the game or the test suites: capture.sh copies it
// into Tests/PlayMode/Level only while it runs. It plays the real scenes with virtual input devices and
// renders the game camera to a 1280x720 texture. It saves nothing in scenes or assets.
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Echoryx.Echo;
using Echoryx.Flow;
using Echoryx.Game;
using Echoryx.Interactions;
using Echoryx.Player;
using Echoryx.Rewind;
using Echoryx.Rewind.Platforms;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Echoryx.Level.PlayTests
{
    public sealed class ZzReadmeCaptures
    {
        private Keyboard keyboard;
        private Mouse mouse;
        private Camera aimCamera, view;
        private RenderTexture target;
        private Transform basis;
        private PlayerMotor player;
        private EchoRecorder recorder;
        private PlayerCameraOrbit orbit;
        private InputSettings original, copy;
        private HideFlags flags;

        private static string Output => Environment.GetEnvironmentVariable("CAPTURE_DIR");

        private IEnumerator Load(string scene)
        {
            Assert.That(Output, Is.Not.Null.And.Not.Empty, "Set CAPTURE_DIR.");
            GameProgress.Storage = new MemoryProgressStorage();
            GameSettings.Storage = new MemorySettingsStorage();
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Echoryx/Scenes/" + scene + ".unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            Time.captureDeltaTime = 1f / 60f;
            original = InputSystem.settings;
            flags = original.hideFlags;
            original.hideFlags = HideFlags.None;
            copy = Object.Instantiate(original);
            copy.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            copy.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            copy.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = copy;
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            view = Camera.main;
            target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            view.targetTexture = target;
            yield return Frames(3);
            var manager = Object.FindFirstObjectByType<CheckpointManager>();
            if (manager != null)
            {
                player = manager.Player;
                recorder = player.GetComponent<EchoRecorder>();
                orbit = Object.FindFirstObjectByType<PlayerCameraOrbit>();
                basis = new GameObject("Capture movement basis").transform;
                player.SetCameraReference(basis);
            }
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (view != null) view.targetTexture = null;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            if (original != null) { InputSystem.settings = original; original.hideFlags = flags; }
            GameProgress.Storage = null;
            GameSettings.Storage = null;
            Time.captureDeltaTime = 0f;
            yield return null;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Menu()
        {
            yield return Load("MainMenu");
            UiThroughCamera();
            yield return Frames(240);
            Shot("menu");
            Object.FindFirstObjectByType<MainMenuController>().OpenLevelSelect();
            yield return Frames(60);
            Shot("niveles");
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Level1AndEcho()
        {
            yield return Load("Level_01_Tutorial");
            UiThroughCamera();
            player.Respawn(new Vector3(0f, 0.05f, 16f), Quaternion.identity);
            yield return Frames(30);
            var plate = Object.FindFirstObjectByType<PressurePlate>().transform.position;
            // Level 1: the robot turned three quarters towards the orbiting game camera.
            var i = 0;
            foreach (var (x, z, face, yaw, pitch) in new[] {
                (0.9f, 24.5f, 215f, 14f, 16f), (-0.4f, 24.8f, 160f, -16f, 18f), (0.8f, 18.4f, 330f, -24f, 18f),
                (1.2f, 19f, 60f, -20f, 22f), (0f, 7f, 40f, 18f, 14f), (1f, 24f, 250f, 28f, 14f) })
            {
                player.Respawn(new Vector3(x, 0.05f, z), Quaternion.Euler(0f, face, 0f));
                Look(yaw, pitch);
                yield return Frames(150);
                Shot("nivel1_" + i++);
            }

            player.Respawn(new Vector3(0f, 0.05f, 18.5f), Quaternion.identity);
            Look(0f, 16f);
            yield return Frames(20);
            yield return WalkTo(plate);
            yield return Pulse(Key.Q);
            yield return Frames(310);
            i = 0;
            foreach (var (dx, dz, face, yaw, pitch) in new[] {
                (2.2f, -2.2f, 315f, -18f, 24f), (2.0f, -3.0f, 325f, -10f, 26f), (-2.0f, -2.8f, 35f, 12f, 26f),
                (2.2f, -1.2f, 285f, -42f, 28f), (1.9f, -3.2f, 330f, -54f, 24f), (-1.6f, -3.4f, 20f, 40f, 28f) })
            {
                player.Respawn(plate + new Vector3(dx, 0.05f, dz), Quaternion.Euler(0f, face, 0f));
                Look(yaw, pitch);
                yield return Frames(90);
                yield return Pulse(Key.E);
                yield return Frames(70);
                Shot("echo_" + i++);
                yield return Frames(260);
            }
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Rewind()
        {
            yield return Load("Level_01_Tutorial");
            UiThroughCamera();
            var platform = Object.FindFirstObjectByType<RewindMovingPlatform>();
            var n = 0;
            foreach (var (x, z, yaw, pitch) in new[] { (-1.7f, 35.55f, 20f, 25f), (1.7f, 35.55f, -20f, 25f), (-2f, 35.5f, 30f, 30f) })
            {
                player.Respawn(new Vector3(x, 0.05f, z), Quaternion.Euler(0f, yaw, 0f));
                Look(yaw, pitch);
                yield return Frames(480);
                Aim(platform.transform.position, player.transform.position + new Vector3(0.35f, 2.6f, -3.6f));
                Mouse(true);
                yield return Frames(50);
                Shot("rewind_" + n + "_a");
                yield return Frames(22);
                Shot("rewind_" + n + "_b");
                Mouse(false);
                n++;
            }
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Level3()
        {
            yield return Load("Level_03_Paradojas");
            UiThroughCamera();
            player.Respawn(new Vector3(0f, 0.05f, 28f), Quaternion.identity);
            yield return Frames(30);
            var plate = GameObject.Find("Plate Bridge").transform.position;
            yield return WalkTo(plate);
            yield return Pulse(Key.Q);
            yield return Frames(310);
            var i = 0;
            // The deck while it rises: the open pit shows what the bridge is.
            foreach (var (x, yaw, pitch) in new[] { (-1.8f, 20f, 27f), (1.8f, -20f, 27f), (-2.2f, 24f, 34f) })
            {
                player.Respawn(new Vector3(x, 0.05f, 33.4f), Quaternion.Euler(0f, yaw, 0f));
                Look(yaw, pitch);
                yield return Frames(120);
                yield return Pulse(Key.E);
                yield return Frames(42);
                Shot("nivel3_puente_" + i + "_a");
                yield return Frames(20);
                Shot("nivel3_puente_" + i++ + "_b");
                yield return Frames(300);
            }

            player.Respawn(new Vector3(1.6f, 0.05f, 66.4f), Quaternion.identity);
            yield return Frames(400);
            var elevator = GameObject.Find("Rewind Platform Room 4").GetComponent<RewindMovingPlatform>();
            Aim(elevator.transform.position, new Vector3(0.6f, 2.8f, 67.8f));
            Mouse(true);
            yield return Frames(220);
            i = 0;
            foreach (var (x, z, yaw, pitch) in new[] { (2.2f, 66.8f, -34f, 30f), (1.8f, 65.6f, -18f, 30f), (2.4f, 68f, -60f, 30f) })
            {
                player.Respawn(new Vector3(x, 0.05f, z), Quaternion.Euler(0f, yaw - 20f, 0f));
                Mouse(false);
                yield return Frames(420);
                Look(yaw, pitch);
                Mouse(true);
                yield return Frames(110);
                Shot("nivel3_elevador_" + i++);
            }
            Mouse(false);
        }

        // Overlay canvases are not part of a camera render: draw them through the game camera (in memory only).
        private void UiThroughCamera()
        {
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = view;
                canvas.planeDistance = 0.4f;
            }
        }

        private void Look(float yaw, float pitch)
        {
            const BindingFlags f = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(PlayerCameraOrbit).GetField("yaw", f).SetValue(orbit, yaw);
            typeof(PlayerCameraOrbit).GetField("pitch", f).SetValue(orbit, pitch);
        }

        private void Aim(Vector3 at, Vector3 from)
        {
            if (aimCamera == null)
            {
                aimCamera = new GameObject("Capture aim camera").AddComponent<Camera>();
                aimCamera.enabled = false;
                typeof(RewindInteractor).GetField("selectionCamera", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(player.GetComponent<RewindInteractor>(), aimCamera);
            }
            aimCamera.transform.SetPositionAndRotation(from, Quaternion.LookRotation(at - from));
        }

        private void Shot(string name)
        {
            Canvas.ForceUpdateCanvases();
            view.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(Output, name + ".png"), image.EncodeToPNG());
            Object.Destroy(image);
        }

        private void Mouse(bool held) => InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Right, held));
        private void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

        private IEnumerator Pulse(Key key)
        {
            Keys(key);
            yield return Frames(2);
            Keys();
            yield return Frames(2);
        }

        private IEnumerator WalkTo(Vector3 point)
        {
            for (var frame = 0; frame < 600; frame++)
            {
                var delta = point - player.transform.position;
                delta.y = 0f;
                if (delta.magnitude < 0.4f) break;
                basis.rotation = Quaternion.LookRotation(delta);
                Keys(Key.W);
                yield return null;
            }
            Keys();
            yield return Frames(18);
        }

        private static IEnumerator Frames(int count)
        {
            for (var i = 0; i < count; i++) yield return null;
        }
    }
}

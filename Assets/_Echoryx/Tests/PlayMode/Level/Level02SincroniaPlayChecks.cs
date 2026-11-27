using System.Collections;
using System.Reflection;
using Echoryx.Echo;
using Echoryx.Flow;
using Echoryx.Game;
using Echoryx.Interactions;
using Echoryx.Player;
using Echoryx.Rewind;
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

namespace Echoryx.Level.PlayTests
{
    /// <summary>
    /// Plays Level_02_Sincronia with real input, the real motor and the authored distances, and measures the
    /// time windows quoted in docs/level-02.md (lines tagged L2TIME in the test log). 60 fps fixed.
    /// </summary>
    public sealed class Level02SincroniaPlayChecks
    {
        private const string ScenePath = "Assets/_Echoryx/Scenes/Level_02_Sincronia.unity";
        private const float EchoSeconds = 5f;
        private const float RequiredMargin = 0.8f;

        private Keyboard keyboard;
        private Mouse mouse;
        private Camera aimCamera;
        private Transform basis;
        private PlayerMotor player;
        private EchoRecorder recorder;
        private CheckpointManager manager;
        private LevelStats stats;
        private InputSettings originalSettings;
        private InputSettings testSettings;
        private HideFlags originalFlags;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameProgress.Storage = new MemoryProgressStorage();
            GameSettings.Storage = new MemorySettingsStorage();
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("Loads the authored scene in the Editor.");
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
            mouse = InputSystem.AddDevice<Mouse>();
            manager = Object.FindFirstObjectByType<CheckpointManager>();
            stats = Object.FindFirstObjectByType<LevelStats>();
            player = manager.Player;
            recorder = player.GetComponent<EchoRecorder>();
            basis = new GameObject("Test movement basis").transform;
            player.SetCameraReference(basis);
            yield return Frames(3);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            if (aimCamera != null) Object.Destroy(aimCamera.gameObject);
            if (basis != null) Object.Destroy(basis.gameObject);
            if (originalSettings != null) { InputSystem.settings = originalSettings; originalSettings.hideFlags = originalFlags; }
            if (testSettings != null) Object.Destroy(testSettings);
            GameProgress.Storage = null;
            GameSettings.Storage = null;
            Time.captureDeltaTime = 0f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Room1TheEchoHoldsTheWellPlateWhileThePlayerUsesTheDoorPlate()
        {
            var door = Find<DoorController>("Door Two Plates");
            var plateDoor = Find<PressurePlate>("Plate Door");
            var plateWell = Find<PressurePlate>("Plate Well");
            var doorPlate = new Vector3(0f, 0f, 12.6f);

            // One plate alone never opens it.
            yield return WalkTo(doorPlate);
            yield return Frames(30);
            Assert.That(plateDoor.IsPressed && !door.IsOpen, Is.True, "The door needs both plates.");

            // Into the well: no way out by jumping (tempting wrong answer: hold the well plate yourself).
            yield return WalkTo(new Vector3(1.9f, 0f, 5.75f));
            Assert.That(player.transform.position.y, Is.LessThan(-2f));
            Assert.That(plateWell.IsPressed, Is.True);
            basis.rotation = Quaternion.LookRotation(Vector3.left);
            for (var jump = 0; jump < 4; jump++)
            {
                Keys(Key.W, Key.Space);
                yield return Frames(2);
                Keys(Key.W);
                yield return Frames(40);
            }
            Keys();
            yield return Frames(20);
            Assert.That(player.transform.position.y, Is.LessThan(-2f), "Cannot climb out of the well.");

            // Record on the well plate (full five seconds), respawn with R: the recording is kept.
            yield return Pulse(Key.Q);
            yield return Frames(310);
            Assert.That(recorder.State, Is.EqualTo(EchoState.Ready));
            yield return Pulse(Key.R);
            yield return Frames(10);
            Assert.That(player.transform.position.z, Is.LessThan(1f));
            Assert.That(recorder.State, Is.EqualTo(EchoState.Ready), "R keeps a finished recording.");

            yield return WalkTo(doorPlate);
            var t0 = Time.time;
            yield return Pulse(Key.E);
            for (var frame = 0; frame < 30 && !door.IsOpen; frame++) yield return null;
            Assert.That(door.IsOpen, Is.True, "Echo in the well + player on the door plate.");
            var opened = Time.time - t0;
            var crossed = -1f;
            yield return WalkTo(new Vector3(0f, 0f, 17f), () => { if (crossed < 0f && player.transform.position.z > 14.9f) crossed = Time.time - t0; });
            Assert.That(manager.ActiveCheckpoint.name, Is.EqualTo("Checkpoint End Room 1"));
            Debug.Log($"L2TIME room1 doorOpen={opened:F2}s crossed={crossed:F2}s echo={EchoSeconds:F2}s margin={EchoSeconds - crossed:F2}s");
            Assert.That(EchoSeconds - crossed, Is.GreaterThan(RequiredMargin));
        }

        [UnityTest]
        public IEnumerator Room2EchoOpensTheGrateLongEnoughToRewindAndBoardTheFarPlatform()
        {
            var grate = Find<DoorController>("Grate");
            var platform = Find<RewindMovingPlatform>("Rewind Platform Room 2");
            var start = platform.transform.parent.Find("Platform Start").position;
            var end = platform.transform.parent.Find("Platform End").position;
            player.Respawn(new Vector3(0f, 0.05f, 17f), Quaternion.identity);
            yield return Frames(10);
            Assert.That(manager.ActiveCheckpoint.name, Is.EqualTo("Checkpoint End Room 1"));
            for (var frame = 0; frame < 600 && Vector3.Distance(platform.transform.position, end) > 0.02f; frame++) yield return null;

            // Holding the plate yourself opens the grate, but it is shut again before you reach it.
            yield return WalkTo(new Vector3(-1.6f, 0f, 21f));
            yield return Frames(20);
            Assert.That(grate.IsOpen, Is.True);
            yield return Pulse(Key.Q); // record on the plate, from its first instant
            yield return Frames(310);
            Assert.That(recorder.State, Is.EqualTo(EchoState.Ready));
            yield return WalkTo(new Vector3(0f, 0f, 29.3f));
            Assert.That(grate.IsOpen, Is.False, "Tempting answer fails: nobody is on the plate any more.");

            // Closed, it also blocks aiming.
            var cameraPosition = new Vector3(0.35f, 2.4f, 25.4f);
            AimFrom(platform.transform.position, cameraPosition);
            HoldRewind(true);
            yield return Frames(10);
            Assert.That(platform.Mode, Is.EqualTo(RewindMode.Normal), "Rewind cannot aim through the closed grate.");
            HoldRewind(false);
            yield return Frames(3);

            var t0 = Time.time;
            yield return Pulse(Key.E);
            var panel = grate.Panel;
            var closedY = panel.localPosition.y;
            while (panel.localPosition.y < closedY + 2.5f && Time.time - t0 < 1f) yield return null;
            AimFrom(platform.transform.position, cameraPosition);
            HoldRewind(true);
            yield return Frames(2);
            Assert.That(platform.Mode, Is.EqualTo(RewindMode.Rewinding));
            var pressed = Time.time - t0;
            while (Vector3.Distance(platform.transform.position, start) > 0.05f && Time.time - t0 < 6f) yield return null;
            var back = Time.time - t0;
            var crossed = -1f;
            yield return WalkTo(start, () => { if (crossed < 0f && player.transform.position.z > 30.3f) crossed = Time.time - t0; });
            HoldRewind(false);
            Assert.That(crossed, Is.GreaterThan(0f), "The player went through the grate.");
            var echoEnd = -1f;
            for (var frame = 0; frame < 360; frame++)
            {
                if (echoEnd < 0f && recorder.State != EchoState.Playing) echoEnd = Time.time - t0;
                yield return null;
            }
            yield return WalkTo(new Vector3(0f, 0f, 40.2f));
            Assert.That(manager.ActiveCheckpoint.name, Is.EqualTo("Checkpoint End Room 2"));
            Debug.Log($"L2TIME room2 rewindPressed={pressed:F2}s platformBack={back:F2}s crossed={crossed:F2}s echoEnd={echoEnd:F2}s margin={echoEnd - crossed:F2}s");
            Assert.That(echoEnd - crossed, Is.GreaterThan(RequiredMargin));
        }

        [UnityTest]
        public IEnumerator Room3OnlyTheEchoHeldGateLetsTheRewoundPlatformCarryThePlayer()
        {
            var gate = Find<DoorController>("Hanging Gate");
            var platform = Find<RewindMovingPlatform>("Rewind Platform Room 3");
            var start = platform.transform.parent.Find("Platform Start").position; // far side
            player.Respawn(new Vector3(0f, 0.05f, 40.2f), Quaternion.identity);
            yield return Frames(10);
            Assert.That(manager.ActiveCheckpoint.name, Is.EqualTo("Checkpoint End Room 2"));
            var near = platform.transform.parent.Find("Platform End").position;
            for (var frame = 0; frame < 600 && Vector3.Distance(platform.transform.position, near) > 0.02f; frame++) yield return null;
            Assert.That(Vector3.Distance(platform.transform.position, near), Is.LessThan(0.05f), "Parked next to the player.");

            // Tempting answer: ride the rewind without the Echo. The closed gate pushes the rider off.
            var attempts = stats.Attempts;
            yield return WalkTo(near);
            AimFrom(platform.transform.position, player.transform.position + new Vector3(0.35f, 2.6f, -3.6f));
            HoldRewind(true);
            for (var frame = 0; frame < 600 && stats.Attempts == attempts; frame++) yield return null;
            HoldRewind(false);
            Assert.That(stats.Attempts, Is.EqualTo(attempts + 1), "Pushed into the pit by the closed gate.");
            yield return Frames(20);

            // Record on the gate plate, ride the rewind and call the Echo while boarding.
            yield return WalkTo(new Vector3(1.6f, 0f, 41.6f));
            yield return Pulse(Key.Q);
            yield return Frames(310);
            Assert.That(recorder.State, Is.EqualTo(EchoState.Ready));
            yield return WalkTo(near);
            AimFrom(platform.transform.position, player.transform.position + new Vector3(0.35f, 2.6f, -3.6f));
            var t0 = Time.time;
            yield return Pulse(Key.E);
            HoldRewind(true);
            yield return Frames(2);
            Assert.That(platform.Mode, Is.EqualTo(RewindMode.Rewinding));
            var passed = -1f;
            while (Vector3.Distance(platform.transform.position, start) > 0.05f && Time.time - t0 < 8f)
            {
                if (passed < 0f && player.transform.position.z > 52.4f + 0.6f) passed = Time.time - t0;
                yield return null;
            }
            var arrived = Time.time - t0;
            Assert.That(passed, Is.GreaterThan(0f), "Carried under the open gate.");
            yield return WalkTo(new Vector3(0f, 0f, 57f));
            HoldRewind(false);
            yield return WalkTo(new Vector3(0f, 0f, 60.5f));
            yield return Frames(5);
            Assert.That(stats.IsCompleted, Is.True);
            Debug.Log($"L2TIME room3 passedGate={passed:F2}s arrivedFar={arrived:F2}s echo={EchoSeconds:F2}s margin={EchoSeconds - passed:F2}s");
            Assert.That(EchoSeconds - passed, Is.GreaterThan(RequiredMargin));
            Assert.That(gate, Is.Not.Null);
        }

        // ---------------------------------------------------------------- helpers

        private static T Find<T>(string name) where T : Component
        {
            var go = GameObject.Find(name);
            Assert.That(go, Is.Not.Null, name);
            return go.GetComponent<T>();
        }

        private void AimFrom(Vector3 target, Vector3 position)
        {
            if (aimCamera == null)
            {
                aimCamera = new GameObject("Test selection camera").AddComponent<Camera>();
                aimCamera.enabled = false;
                typeof(RewindInteractor).GetField("selectionCamera", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(player.GetComponent<RewindInteractor>(), aimCamera);
            }
            aimCamera.transform.position = position;
            aimCamera.transform.rotation = Quaternion.LookRotation(target - position);
        }

        private void HoldRewind(bool held) => InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Right, held));

        private void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

        private IEnumerator Pulse(Key key)
        {
            Keys(key);
            yield return Frames(2);
            Keys();
            yield return Frames(2);
        }

        private IEnumerator WalkTo(Vector3 target, System.Action eachFrame = null)
        {
            for (var frame = 0; frame < 600; frame++)
            {
                eachFrame?.Invoke();
                var delta = target - player.transform.position;
                delta.y = 0f;
                if (delta.magnitude < 0.45f) break;
                basis.rotation = Quaternion.LookRotation(delta);
                Keys(Key.W);
                yield return null;
            }
            Keys();
            for (var frame = 0; frame < 18; frame++) { eachFrame?.Invoke(); yield return null; }
            var remaining = target - player.transform.position;
            remaining.y = 0f;
            Assert.That(remaining.magnitude, Is.LessThan(0.9f), "Walk target unreachable: " + target + "; player=" + player.transform.position);
        }

        private static IEnumerator Frames(int count)
        {
            for (var i = 0; i < count; i++) yield return null;
        }
    }
}

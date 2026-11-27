using System;
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
using Object = UnityEngine.Object;

namespace Echoryx.Level.PlayTests
{
    /// <summary>
    /// Plays Level_03_Paradojas section by section with real input (keyboard and mouse devices), the real motor
    /// and the authored distances. Each section first plays its tempting wrong answer and checks that it fails,
    /// then plays the solution of docs/level-03.md and measures its margin (lines tagged L3TIME). 60 fps fixed.
    /// </summary>
    public sealed class Level03ParadojasPlayChecks
    {
        private const string ScenePath = "Assets/_Echoryx/Scenes/Level_03_Paradojas.unity";
        private const float EchoSeconds = 5f;
        private const float RewindSeconds = 5f;
        private const float RequiredMargin = 0.9f;

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
            Assert.That(manager, Is.Not.Null, "Level_03_Paradojas must exist and have checkpoints.");
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

        // ------------------------------------------------------------ 1. reminder: Rewind, then Echo

        [UnityTest]
        public IEnumerator Section1RewindBringsThePlatformBackAndTheEchoHoldsTheDoor()
        {
            var platform = Find<RewindMovingPlatform>("Rewind Platform Room 1");
            var start = Point(platform, "Platform Start");
            var end = Point(platform, "Platform End");
            yield return WaitUntil(() => Near(platform, end), 600);
            Assert.That(Near(platform, end), Is.True, "The platform has left before the player arrives.");

            // Tempting answer: jump the gap. It is too wide.
            yield return WalkTo(new Vector3(0f, 0f, 4.8f));
            yield return RunAndJump(5.7f);

            // Rewind it back, board while holding, release: its trip carries the player across.
            yield return WalkTo(new Vector3(0f, 0f, 5f));
            AimFrom(platform.transform.position, new Vector3(0.35f, 2.6f, 4.2f));
            HoldRewind(true);
            yield return Frames(2);
            Assert.That(platform.Mode, Is.EqualTo(RewindMode.Rewinding));
            yield return WaitUntil(() => Near(platform, start), 400);
            yield return WalkTo(start);
            var released = Time.time;
            HoldRewind(false);
            yield return WaitUntil(() => Near(platform, end), 400);
            var travel = Time.time - released;
            yield return WalkTo(new Vector3(0f, 0f, 17.5f));
            Debug.Log($"L3TIME s1rewind travel={travel:F2}s history={RewindSeconds:F2}s margin={RewindSeconds - travel:F2}s");
            Assert.That(RewindSeconds - travel, Is.GreaterThan(RequiredMargin), "Its whole trip fits in the Rewind history.");

            // Tempting answer: hold the plate yourself. The door is shut again before you reach it.
            var door = Find<DoorController>("Door Reminder");
            var plate = Find<PressurePlate>("Plate Reminder");
            yield return WalkTo(plate.transform.position);
            yield return WaitUntil(() => door.IsOpen, 60);
            Assert.That(door.IsOpen, Is.True);
            yield return WalkTo(new Vector3(0f, 0f, 24.3f));
            Assert.That(door.IsOpen, Is.False, "Nobody is on the plate any more.");

            // Record on the plate, call the Echo in front of the door.
            yield return WalkTo(plate.transform.position);
            yield return Record();
            yield return WalkTo(new Vector3(0f, 0f, 24.3f));
            var t0 = Time.time;
            yield return Pulse(Key.E);
            var crossed = -1f;
            yield return WalkTo(new Vector3(0f, 0f, 28f), () => { if (crossed < 0f && player.transform.position.z > 26.4f) crossed = Time.time - t0; });
            Assert.That(manager.ActiveCheckpoint.name, Is.EqualTo("Checkpoint End Room 1"));
            Debug.Log($"L3TIME s1echo crossed={crossed:F2}s echo={EchoSeconds:F2}s margin={EchoSeconds - crossed:F2}s");
            Assert.That(EchoSeconds - crossed, Is.GreaterThan(RequiredMargin));
        }

        // ------------------------------------------------------------ 2. the Echo holds the bridge while you cross

        [UnityTest]
        public IEnumerator Section2TheEchoKeepsTheBridgeUpWhileThePlayerCrosses()
        {
            yield return RespawnAt(28f, "Checkpoint End Room 1");
            var bridge = Find<DoorController>("Bridge");
            var plate = Find<PressurePlate>("Plate Bridge");

            // Tempting answer: raise it yourself and run. It sinks under you.
            yield return WalkTo(plate.transform.position);
            yield return WaitUntil(() => bridge.IsOpen, 120);
            Assert.That(bridge.IsOpen, Is.True, "The plate raises the bridge.");
            yield return ExpectFall(new Vector3(0f, 0f, 49.5f), "The bridge sinks as soon as nobody is on its plate.");

            yield return WalkTo(plate.transform.position);
            yield return Record();
            yield return WalkTo(new Vector3(0f, 0f, 33.4f));
            var t0 = Time.time;
            yield return Pulse(Key.E);
            yield return WaitUntil(() => bridge.IsOpen, 120);
            var raised = Time.time - t0;
            var crossed = -1f;
            var echoEnd = -1f;
            void Track()
            {
                if (crossed < 0f && player.transform.position.z > 48.4f) crossed = Time.time - t0;
                if (echoEnd < 0f && recorder.State != EchoState.Playing) echoEnd = Time.time - t0;
            }
            yield return WalkTo(new Vector3(0f, 0f, 50.5f), Track);
            for (var frame = 0; frame < 360 && echoEnd < 0f; frame++) { Track(); yield return null; }
            Assert.That(manager.ActiveCheckpoint.name, Is.EqualTo("Checkpoint End Room 2"));
            Debug.Log($"L3TIME s2 bridgeUp={raised:F2}s crossed={crossed:F2}s echoEnd={echoEnd:F2}s margin={echoEnd - crossed:F2}s");
            Assert.That(echoEnd - crossed, Is.GreaterThan(RequiredMargin));
        }

        // ------------------------------------------------------------ 3. Rewind recovers the platform and carries you

        [UnityTest]
        public IEnumerator Section3RewindRidesThePlatformBackToTheFarSide()
        {
            yield return RespawnAt(50.5f, "Checkpoint End Room 2");
            var platform = Find<RewindMovingPlatform>("Rewind Platform Room 3");
            var far = Point(platform, "Platform Start");
            var near = Point(platform, "Platform End");
            yield return WaitUntil(() => Near(platform, near), 600);
            Assert.That(Near(platform, near), Is.True, "Parked next to the player.");

            // Tempting answer: board it and jump to the far side. Too far.
            yield return WalkTo(near);
            yield return RunAndJump(55.7f);

            yield return WalkTo(near);
            AimFrom(platform.transform.position, player.transform.position + new Vector3(0.35f, 2.6f, -3.6f));
            var t0 = Time.time;
            HoldRewind(true);
            yield return Frames(2);
            Assert.That(platform.Mode, Is.EqualTo(RewindMode.Rewinding));
            yield return WaitUntil(() => Near(platform, far), 600);
            var ride = Time.time - t0;
            Assert.That(Near(platform, far), Is.True, "Rewind carried the player to the far side.");
            yield return WalkTo(new Vector3(0f, 0f, 63.5f));
            HoldRewind(false);
            Assert.That(manager.ActiveCheckpoint.name, Is.EqualTo("Checkpoint End Room 3"));
            Debug.Log($"L3TIME s3 ride={ride:F2}s history={RewindSeconds:F2}s margin={RewindSeconds - ride:F2}s");
            Assert.That(RewindSeconds - ride, Is.GreaterThan(RequiredMargin));
        }

        // ------------------------------------------------------------ 4. phase: sync the elevator with Rewind, then E

        [UnityTest]
        public IEnumerator Section4TheEchoOnlyHoldsTheElevatorPlateOnceRewindPutsItBackInPhase()
        {
            yield return RespawnAt(63.5f, "Checkpoint End Room 3");
            var elevator = Find<RewindMovingPlatform>("Rewind Platform Room 4");
            var top = Point(elevator, "Platform Start");
            var bottom = Point(elevator, "Platform End");
            var plate = Find<PressurePlate>("Plate Elevator Room 4");
            var grate = Find<DoorController>("Grate Phase");
            const float grateZ = 80f;
            var aim = new Vector3(0.6f, 2.8f, 67.8f);
            yield return WaitUntil(() => Near(elevator, bottom), 600);
            Assert.That(Near(elevator, bottom), Is.True, "The elevator has sunk into the shaft.");

            // Rewind pulls it up; holding the button keeps it there.
            yield return PullUp(elevator, top, aim);
            yield return WalkTo(new Vector3(top.x, 0f, top.z));
            yield return WaitUntil(() => grate.IsOpen, 60);
            Assert.That(plate.PlayerPresent && grate.IsOpen, Is.True, "The plate travels with the elevator.");

            // Tempting answer 1: hold its plate yourself and run to the grate.
            yield return WalkTo(new Vector3(1.5f, 0f, grateZ - 1.4f));
            Assert.That(grate.IsOpen, Is.False, "Shut before you reach it.");

            // Record on the elevator plate, then step off and let it sink.
            yield return WalkTo(new Vector3(top.x, 0f, top.z));
            yield return Record();
            yield return WalkTo(new Vector3(1.5f, 0f, top.z));
            HoldRewind(false);
            yield return WaitUntil(() => Near(elevator, bottom), 400);

            // Tempting answer 2: call the Echo now. It replays at the top, the plate is down the shaft.
            yield return WalkTo(new Vector3(1.5f, 0f, grateZ - 1.4f));
            var opened = false;
            yield return Pulse(Key.E);
            for (var frame = 0; frame < 330 && recorder.State == EchoState.Playing; frame++)
            {
                opened |= grate.IsOpen || plate.EchoPresent;
                yield return null;
            }
            Assert.That(opened, Is.False, "Out of phase: the Echo stands where the plate no longer is.");
            yield return WaitUntil(() => recorder.State == EchoState.Ready, 120);

            // Solution: put the elevator back in its recorded phase with Rewind, then call the Echo.
            yield return WalkTo(new Vector3(1.5f, 0f, top.z));
            yield return PullUp(elevator, top, aim);
            var t0 = Time.time;
            yield return Pulse(Key.E);
            var crossed = -1f;
            yield return WalkTo(new Vector3(0f, 0f, 84f), () => { if (crossed < 0f && player.transform.position.z > grateZ + 0.6f) crossed = Time.time - t0; });
            HoldRewind(false);
            Assert.That(crossed, Is.GreaterThan(0f), "Through the grate.");
            Assert.That(manager.ActiveCheckpoint.name, Is.EqualTo("Checkpoint End Room 4"));
            Debug.Log($"L3TIME s4 crossed={crossed:F2}s echo={EchoSeconds:F2}s margin={EchoSeconds - crossed:F2}s");
            Assert.That(EchoSeconds - crossed, Is.GreaterThan(RequiredMargin));
        }

        // ------------------------------------------------------------ 5a. finale, stage 1: two plates, one on the elevator

        [UnityTest]
        public IEnumerator Section5aTheDoorNeedsYourPlateAndTheEchoOnTheSyncedElevator()
        {
            yield return RespawnAt(84f, "Checkpoint End Room 4");
            var elevator = Find<RewindMovingPlatform>("Rewind Platform Room 5");
            var top = Point(elevator, "Platform Start");
            var bottom = Point(elevator, "Platform End");
            var plateElevator = Find<PressurePlate>("Plate Elevator Room 5");
            var plateDoor = Find<PressurePlate>("Plate Door Room 5");
            var door = Find<DoorController>("Door Two Plates");
            var aim = new Vector3(0.6f, 2.8f, 88.8f);
            yield return WaitUntil(() => Near(elevator, bottom), 600);

            // Tempting answer: your own plate alone.
            yield return WalkTo(plateDoor.transform.position);
            yield return Frames(30);
            Assert.That(plateDoor.IsPressed && !door.IsOpen, Is.True, "The door needs both plates.");

            // Record on the elevator plate.
            yield return WalkTo(new Vector3(1.5f, 0f, 88.3f));
            yield return PullUp(elevator, top, aim);
            yield return WalkTo(new Vector3(top.x, 0f, top.z));
            yield return Record();
            yield return WalkTo(new Vector3(1.5f, 0f, top.z));
            HoldRewind(false);
            yield return WaitUntil(() => Near(elevator, bottom), 400);

            // Out of phase again: Echo + your plate is not enough.
            yield return WalkTo(plateDoor.transform.position);
            var opened = false;
            yield return Pulse(Key.E);
            for (var frame = 0; frame < 330 && recorder.State == EchoState.Playing; frame++) { opened |= door.IsOpen; yield return null; }
            Assert.That(opened, Is.False, "The elevator plate is down the shaft.");
            yield return WaitUntil(() => recorder.State == EchoState.Ready, 120);

            // Solution: sync the elevator, keep holding, walk to your plate, call the Echo.
            yield return WalkTo(new Vector3(1.5f, 0f, 88.3f));
            yield return PullUp(elevator, top, aim);
            yield return WalkTo(plateDoor.transform.position);
            var t0 = Time.time;
            yield return Pulse(Key.E);
            yield return WaitUntil(() => door.IsOpen, 60);
            var crossed = -1f;
            yield return WalkTo(new Vector3(0f, 0f, 102f), () => { if (crossed < 0f && player.transform.position.z > 99.9f) crossed = Time.time - t0; });
            HoldRewind(false);
            Assert.That(manager.ActiveCheckpoint.name, Is.EqualTo("Checkpoint Final Stage"));
            Debug.Log($"L3TIME s5a crossed={crossed:F2}s echo={EchoSeconds:F2}s margin={EchoSeconds - crossed:F2}s");
            Assert.That(EchoSeconds - crossed, Is.GreaterThan(RequiredMargin));
        }

        // ------------------------------------------------------------ 5b. finale, stage 2: E to aim, Rewind and hold, E again, release

        [UnityTest]
        public IEnumerator Section5bPinThePlatformCallTheEchoAndRideUnderTheGate()
        {
            yield return RespawnAt(102f, "Checkpoint Final Stage");
            var platform = Find<RewindMovingPlatform>("Rewind Platform Room 6");
            var start = Point(platform, "Platform Start");
            var end = Point(platform, "Platform End");
            var plate = Find<PressurePlate>("Plate Gate");
            var gate = Find<DoorController>("Hanging Gate");
            const float gateZ = 112.8f;
            var aim = new Vector3(0.35f, 2.6f, 104.2f);
            yield return WaitUntil(() => Near(platform, end), 600);

            yield return WalkTo(plate.transform.position);
            yield return Record();

            // The platform is parked beyond the closed gate: the gate blocks aiming at it.
            yield return WalkTo(new Vector3(0f, 0f, 106.4f));
            AimFrom(platform.transform.position, aim);
            HoldRewind(true);
            yield return Frames(10);
            Assert.That(platform.Mode, Is.EqualTo(RewindMode.Normal), "Rewind cannot aim through the closed gate.");
            HoldRewind(false);
            yield return Frames(3);

            // Tempting answer: the Echo opens the gate, fetch the platform with Rewind and ride. The Echo runs out first.
            yield return Pulse(Key.E);
            yield return WaitUntil(() => gate.IsOpen, 60);
            yield return PullUp(platform, start, aim);
            yield return WalkTo(start);
            var attempts = stats.Attempts;
            HoldRewind(false);
            yield return WaitUntil(() => stats.Attempts != attempts, 600);
            Assert.That(stats.Attempts, Is.EqualTo(attempts + 1), "The closed gate pushes the rider into the pit.");
            yield return Frames(20);
            Assert.That(recorder.State, Is.EqualTo(EchoState.Ready), "The recording survives the respawn.");

            // Solution: the Echo opens the gate to aim, Rewind brings the platform back and keeps it at the dock,
            // the Echo is called again once it has finished, then release.
            yield return WaitUntil(() => Near(platform, end), 600);
            yield return WalkTo(new Vector3(0f, 0f, 106.4f));
            yield return Pulse(Key.E);
            yield return WaitUntil(() => gate.IsOpen, 60);
            yield return PullUp(platform, start, aim);
            yield return WalkTo(start);
            yield return WaitUntil(() => recorder.State == EchoState.Ready, 400);
            yield return WaitUntil(() => !gate.IsOpen, 60);
            Assert.That(Near(platform, start), Is.True, "Still held at the dock.");
            var t0 = Time.time;
            yield return Pulse(Key.E);
            HoldRewind(false);
            var passed = -1f;
            yield return WaitUntil(() =>
            {
                if (passed < 0f && player.transform.position.z > gateZ + 0.6f) passed = Time.time - t0;
                return Near(platform, end);
            }, 600);
            Assert.That(passed, Is.GreaterThan(0f), "Carried under the open gate.");
            yield return WalkTo(new Vector3(0f, 0f, 117.5f));
            yield return WalkTo(new Vector3(0f, 0f, 120f));
            yield return Frames(5);
            Assert.That(stats.IsCompleted, Is.True);
            Debug.Log($"L3TIME s5b passedGate={passed:F2}s echo={EchoSeconds:F2}s margin={EchoSeconds - passed:F2}s");
            Assert.That(EchoSeconds - passed, Is.GreaterThan(RequiredMargin));
        }

        // ---------------------------------------------------------------- helpers

        private static T Find<T>(string name) where T : Component
        {
            var go = GameObject.Find(name);
            Assert.That(go, Is.Not.Null, name);
            return go.GetComponent<T>();
        }

        private static Vector3 Point(Component mechanism, string name) => mechanism.transform.parent.Find(name).position;

        private static bool Near(Component mechanism, Vector3 point) => Vector3.Distance(mechanism.transform.position, point) < 0.05f;

        private IEnumerator RespawnAt(float z, string checkpoint)
        {
            player.Respawn(new Vector3(0f, 0.05f, z), Quaternion.identity);
            yield return Frames(10);
            Assert.That(manager.ActiveCheckpoint.name, Is.EqualTo(checkpoint));
        }

        private IEnumerator Record()
        {
            yield return Pulse(Key.Q);
            yield return Frames(310);
            Assert.That(recorder.State, Is.EqualTo(EchoState.Ready), "A full five-second recording.");
        }

        private IEnumerator PullUp(RewindMovingPlatform mechanism, Vector3 point, Vector3 cameraPosition)
        {
            AimFrom(mechanism.transform.position, cameraPosition);
            HoldRewind(true);
            yield return Frames(2);
            Assert.That(mechanism.Mode, Is.EqualTo(RewindMode.Rewinding), "Aimed at " + mechanism.name);
            yield return WaitUntil(() => Near(mechanism, point), 400);
            Assert.That(Near(mechanism, point), Is.True, mechanism.name + " back where its trip started.");
        }

        /// <summary>Runs forward and jumps once past <paramref name="takeOffZ"/>; expects to fall into the pit.</summary>
        private IEnumerator RunAndJump(float takeOffZ)
        {
            var attempts = stats.Attempts;
            basis.rotation = Quaternion.identity;
            Keys(Key.W);
            for (var frame = 0; frame < 120 && player.transform.position.z < takeOffZ; frame++) yield return null;
            Keys(Key.W, Key.Space);
            yield return Frames(2);
            Keys(Key.W);
            yield return WaitUntil(() => stats.Attempts != attempts, 240);
            Keys();
            Assert.That(stats.Attempts, Is.EqualTo(attempts + 1), "The gap cannot be jumped.");
            yield return Frames(20);
        }

        private IEnumerator ExpectFall(Vector3 target, string message)
        {
            var attempts = stats.Attempts;
            for (var frame = 0; frame < 400 && stats.Attempts == attempts; frame++)
            {
                var delta = target - player.transform.position;
                delta.y = 0f;
                if (delta.sqrMagnitude > 0.01f) basis.rotation = Quaternion.LookRotation(delta);
                Keys(Key.W);
                yield return null;
            }
            Keys();
            Assert.That(stats.Attempts, Is.EqualTo(attempts + 1), message);
            yield return Frames(20);
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

        private IEnumerator WalkTo(Vector3 target, Action eachFrame = null)
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

        private static IEnumerator WaitUntil(Func<bool> condition, int maximumFrames)
        {
            for (var frame = 0; frame < maximumFrames && !condition(); frame++) yield return null;
        }

        private static IEnumerator Frames(int count)
        {
            for (var i = 0; i < count; i++) yield return null;
        }
    }
}

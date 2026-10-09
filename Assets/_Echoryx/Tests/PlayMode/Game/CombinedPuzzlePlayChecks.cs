using System.Collections;
using Echoryx.Echo;
using Echoryx.Interactions;
using Echoryx.Player;
using Echoryx.Rewind;
using Echoryx.Rewind.Platforms;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace Echoryx.Game.PlayTests
{
    /// <summary>PlayMode test: actual input, motor, collisions and transport.
    /// A fixed camera basis makes the scripted walking reproducible; it does not validate manual aiming.</summary>
    public sealed class CombinedPuzzlePlayChecks
    {
        private Keyboard keyboard;
        private Mouse mouse;
        private Camera aimCamera;
        private Transform basis;
        private PlayerMotor player;
        private CheckpointManager manager;
        private InputSettings originalSettings;
        private InputSettings testSettings;
        private HideFlags originalFlags;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Echoryx/Scenes/Prototype.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("These prototype checks load the authored scene in Unity Editor.");
#endif
            yield return null;
            Time.captureDeltaTime = 1f / 60f;
            originalSettings = InputSystem.settings;
            originalFlags = originalSettings.hideFlags;
            originalSettings.hideFlags = HideFlags.None;
            testSettings = Object.Instantiate(originalSettings);
            testSettings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            // Batchmode has no focused Game View: keep virtual input in the player update.
            testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = testSettings;
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            manager = GameObject.Find("S04-A Checkpoints").GetComponent<CheckpointManager>();
            player = manager.Player;
            basis = new GameObject("Test movement camera basis").transform;
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
            if (originalSettings != null)
            {
                InputSystem.settings = originalSettings;
                originalSettings.hideFlags = originalFlags;
            }
            if (testSettings != null) Object.Destroy(testSettings);
            Time.captureDeltaTime = 0f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator HistoricalRoomBDockSpawnReproducesShortcut()
        {
            var checkpoint = GameObject.Find("Checkpoint Room B").GetComponent<CheckpointTrigger>();
            // Test-only historical fixture: prove the exact old R -> board -> cross path.
            // The scene is reloaded per test; this pose is never saved to the asset.
            Assert.That(checkpoint.SpawnPoint.position.z, Is.GreaterThan(-5f));
            checkpoint.SpawnPoint.position = new Vector3(6f, 0.05f, -20f);
            manager.ActivateCheckpoint(checkpoint);
            Keys(Key.R);
            yield return Frames(1);
            Keys();
            yield return Frames(3);
            var platform = GameObject.Find("S03-C Rewind Crossing").GetComponentInChildren<RewindMovingPlatform>();
            yield return BoardAndRide(platform, Vector3.right, 7.3f, true);
            yield return WalkTo(new Vector3(18f, 0f, -20f));
            Assert.That(player.transform.position.x, Is.GreaterThan(17f));
            Assert.That(player.GetComponent<CharacterController>().isGrounded, Is.True);
            Debug.Log("Room B regression: historical dock spawn reproduces the no-Rewind shortcut.");
        }

        [UnityTest]
        public IEnumerator RoomBResetAccessOutlastsInitialTrip()
        {
            var checkpoint = GameObject.Find("Checkpoint Room B").GetComponent<CheckpointTrigger>();
            player.Respawn(checkpoint.SpawnPoint.position, checkpoint.SpawnPoint.rotation);
            yield return Frames(4);
            Assert.That(manager.ActiveCheckpoint, Is.SameAs(checkpoint), "Real entrance volume activates B.");
            var crossing = GameObject.Find("S03-C Rewind Crossing").transform;
            var platform = crossing.GetComponentInChildren<RewindMovingPlatform>();
            var combined = GameObject.Find("S04-C Combined Puzzle").GetComponentInChildren<RewindMovingPlatform>();
            yield return Frames(150);
            var foreignPose = combined.transform.position;
            var foreignTime = History(combined).Time;
            // Repeated R and a real fall-volume reset must use the same safe entrance.
            for (var attempt = 0; attempt < 3; attempt++)
            {
                if (attempt < 2) yield return Pulse(Key.R);
                else
                {
                    player.Respawn(new Vector3(10f, -5.2f, -20f), Quaternion.identity);
                    yield return Frames(4);
                }
                Assert.That(Vector3.Distance(player.transform.position, checkpoint.SpawnPoint.position), Is.LessThan(0.2f));
                Assert.That(History(platform).Time, Is.LessThan(0.2d), "Reset still restarts the platform normally.");
                // Actual WASD approach, without deliberate waiting or Rewind after reset.
                yield return WalkTo(new Vector3(5.5f, 0f, -20f));
                Assert.That(Vector3.Distance(platform.transform.position, crossing.Find("Platform End").position),
                    Is.LessThan(0.03f), "The initial trip must end before a respawned player can board it.");
                Assert.That(platform.Mode, Is.EqualTo(RewindMode.Normal));
                // Maximum-speed edge jump, including the motor's coyote-time allowance.
                basis.rotation = Quaternion.LookRotation(Vector3.right);
                Keys(Key.W);
                for (var frame = 0; frame < 90 && player.transform.position.x < 8.15f; frame++) yield return null;
                Keys(Key.W, Key.Space);
                yield return Frames(1);
                Keys(Key.W);
                var peak = player.transform.position.y;
                var farthestLandingX = player.transform.position.x;
                var boarded = false;
                for (var frame = 0; frame < 160; frame++)
                {
                    peak = Mathf.Max(peak, player.transform.position.y);
                    // Horizontal travel below the surface cannot count as reaching the platform.
                    if (player.transform.position.y >= 0.1f)
                        farthestLandingX = Mathf.Max(farthestLandingX, player.transform.position.x);
                    boarded |= player.GetComponent<CharacterController>().isGrounded && player.transform.position.x > 9f;
                    yield return null;
                }
                Keys();
                Assert.That(peak, Is.GreaterThan(1f), "The attempt must actually jump.");
                Assert.That(farthestLandingX, Is.LessThan(14.3f), "Jump-only cannot reach the parked platform at landing height.");
                Assert.That(boarded, Is.False, "No landing on the platform or far dock is allowed without Rewind.");
                Assert.That(Vector3.Distance(player.transform.position, checkpoint.SpawnPoint.position), Is.LessThan(0.3f),
                    "The failed shortcut falls and returns to the B entrance.");
                Assert.That(manager.ActiveCheckpoint, Is.SameAs(checkpoint));
            }
            Assert.That(combined.transform.position, Is.EqualTo(foreignPose));
            Assert.That(History(combined).Time, Is.EqualTo(foreignTime), "B resets must not touch the combined room.");
        }

        [UnityTest]
        public IEnumerator RoomBRemainsRecoverableAfterLongWaitAndRepeatsWithRmb()
        {
            var checkpoint = GameObject.Find("Checkpoint Room B").GetComponent<CheckpointTrigger>();
            manager.ActivateCheckpoint(checkpoint);
            var crossing = GameObject.Find("S03-C Rewind Crossing").transform;
            var platform = crossing.GetComponentInChildren<RewindMovingPlatform>();
            for (var attempt = 0; attempt < 2; attempt++)
            {
                yield return Pulse(Key.R);
                yield return WalkTo(new Vector3(6.5f, 0f, -20f));
                var historyTime = History(platform).Time;
                yield return Frames(600);
                Assert.That(History(platform).Time, Is.EqualTo(historyTime), "Long waits retain the trip history.");
                Assert.That(platform.CanRewind, Is.True);
                AimFrom(platform, new Vector3(6.5f, 2.1f, -23f));
                HoldRewind(true);
                yield return Frames(3);
                Assert.That(platform.Mode, Is.EqualTo(RewindMode.Rewinding), "Real RMB and raycast select the child collider.");
                yield return Frames(100);
                Assert.That(Vector3.Distance(platform.transform.position, crossing.Find("Platform Start").position), Is.LessThan(0.03f));
                yield return WalkTo(crossing.Find("Platform Start").position);
                Assert.That(player.GetComponent<CharacterController>().isGrounded, Is.True);
                var recovered = platform.transform.position;
                HoldRewind(false);
                yield return Frames(3);
                Assert.That(platform.Mode, Is.EqualTo(RewindMode.Normal));
                Assert.That(Vector3.Distance(platform.transform.position, recovered), Is.LessThan(0.3f), "Release continues without a teleport.");
                yield return Frames(130);
                Assert.That(player.GetComponent<CharacterController>().isGrounded, Is.True);
                Assert.That(Vector3.Distance(player.transform.position, platform.transform.position), Is.LessThan(1.7f));
                yield return WalkTo(new Vector3(18f, 0f, -20f));
                Assert.That(player.transform.position.x, Is.GreaterThan(17f));
                Assert.That(player.GetComponent<CharacterController>().isGrounded, Is.True);
            }
        }

        [UnityTest]
        public IEnumerator CombinedRoomRequiresBothSkillsAndRepeatsAfterReset()
        {
            var room = GameObject.Find("S04-C Combined Puzzle").transform;
            var checkpoint = room.GetComponentInChildren<CheckpointTrigger>();
            var platform = room.GetComponentInChildren<RewindMovingPlatform>();
            var plate = room.GetComponentInChildren<PressurePlate>();
            var door = room.GetComponentInChildren<DoorController>();
            player.Respawn(checkpoint.SpawnPoint.position, checkpoint.SpawnPoint.rotation);
            yield return Frames(4);
            Assert.That(manager.ActiveCheckpoint, Is.SameAs(checkpoint), "Entrance volume activates the actual checkpoint.");

            // No Echo, even after stepping off the plate: door closes before the threshold is reachable.
            yield return WalkTo(room.TransformPoint(new Vector3(0f, 0f, 2f)));
            Assert.That(plate.IsPressed, Is.True);
            yield return Frames(30);
            yield return WalkTo(room.TransformPoint(new Vector3(0f, 0f, 9.5f)));
            basis.rotation = Quaternion.identity;
            Keys(Key.W, Key.Space);
            yield return Frames(45);
            Keys(Key.W);
            yield return Frames(60);
            Keys();
            Assert.That(player.transform.position.z, Is.LessThan(10.6f), "Jump/walking cannot cross the closed first door.");
            Assert.That(door.IsOpen, Is.False);
            Assert.That(platform.BeginRewind(), Is.True);
            yield return Frames(100);
            platform.EndRewind();
            Assert.That(player.transform.position.z, Is.LessThan(10.6f), "Rewind alone does not open the Echo barrier.");

            // Repeated real R input returns to the same entrance; never a forward checkpoint.
            for (var attempt = 0; attempt < 3; attempt++)
            {
                yield return Pulse(Key.R);
                Assert.That(Vector3.Distance(player.transform.position, checkpoint.SpawnPoint.position), Is.LessThan(0.2f));
                Assert.That(player.GetComponent<EchoRecorder>().State, Is.EqualTo(EchoState.Available));
                yield return Frames(150);
                Assert.That(Vector3.Distance(platform.transform.position, room.Find("Platform End").position), Is.LessThan(0.03f));
            }

            for (var attempt = 0; attempt < 3; attempt++)
            {
                yield return Pulse(Key.R);
                // Record with Q and the production five-second automatic stop.
                yield return WalkTo(room.TransformPoint(new Vector3(0f, 0f, 2f)));
                yield return Pulse(Key.Q);
                yield return Frames(310);
                var recorder = player.GetComponent<EchoRecorder>();
                Assert.That(recorder.State, Is.EqualTo(EchoState.Ready));
                Assert.That(recorder.ReadyRecording.Duration, Is.EqualTo(5d).Within(0.02d));
                yield return WalkTo(room.TransformPoint(new Vector3(0f, 0f, 9.5f)));
                yield return Pulse(Key.E);
                Assert.That(recorder.State, Is.EqualTo(EchoState.Playing));
                yield return WalkTo(room.TransformPoint(new Vector3(0f, 0f, 15.5f)));
                yield return Frames(310);
                Assert.That(player.transform.position.z, Is.GreaterThan(13.5f));
                Assert.That(recorder.State, Is.EqualTo(EchoState.Available));
                Assert.That(door.IsOpen, Is.False, "Safe middle surface permits waiting after Echo expires.");

                // Echo alone: run up at full speed and jump just beyond the edge (coyote time).
                if (attempt == 0)
                {
                    basis.rotation = Quaternion.identity;
                    Keys(Key.W);
                    for (var frame = 0; frame < 90 && player.transform.position.z < 18.25f; frame++) yield return null;
                    Keys(Key.W, Key.Space);
                    yield return Frames(1);
                    Keys(Key.W);
                    var jumpPeak = player.transform.position.y;
                    for (var frame = 0; frame < 160; frame++)
                    {
                        jumpPeak = Mathf.Max(jumpPeak, player.transform.position.y);
                        yield return null;
                    }
                    Keys();
                    Assert.That(jumpPeak, Is.GreaterThan(1f), "The edge/coyote attempt must actually jump, not merely walk off.");
                    Assert.That(Vector3.Distance(player.transform.position, checkpoint.SpawnPoint.position), Is.LessThan(0.3f),
                        "Jump-only falls into the room hazard and respawns; cannot reach the far dock.");
                    Assert.That(recorder.ReadyRecording, Is.Null);
                    Debug.Log("S04C CHECK: no Echo, Rewind only, repeated R and Echo-only jumping failed to complete the room.");
                    continue;
                }

                yield return WalkTo(room.TransformPoint(new Vector3(0f, 0f, 16.7f)));
                AimAt(platform, room);
                HoldRewind(true);
                yield return Frames(3);
                Assert.That(platform.Mode, Is.EqualTo(RewindMode.Rewinding), "RMB selects the child collider through the real raycast.");
                yield return Frames(100);
                Assert.That(Vector3.Distance(platform.transform.position, room.Find("Platform Start").position), Is.LessThan(0.03f));
                // Hold Rewind at origin while walking aboard: no timed jump is required in this room.
                yield return WalkTo(room.Find("Platform Start").position);
                Assert.That(player.GetComponent<CharacterController>().isGrounded, Is.True);
                HoldRewind(false);
                yield return Frames(3);
                Assert.That(platform.Mode, Is.EqualTo(RewindMode.Normal));
                yield return Frames(130);
                Assert.That(player.GetComponent<CharacterController>().isGrounded, Is.True);
                Assert.That(Vector3.Distance(player.transform.position, platform.transform.position), Is.LessThan(1.7f));
                yield return WalkTo(room.TransformPoint(new Vector3(0f, 0f, 30.5f)));
                Assert.That(player.transform.position.z, Is.GreaterThan(30f));
                Assert.That(player.GetComponent<CharacterController>().isGrounded, Is.True);
                Debug.Log("S04C CHECK: combined room completed with Q/E and recovered platform; actual collisions and transport.");
            }
            yield return Pulse(Key.R);
            Assert.That(Vector3.Distance(player.transform.position, checkpoint.SpawnPoint.position), Is.LessThan(0.2f));
            Assert.That(player.GetComponent<EchoPlayback>().ActiveActor, Is.Null);
            Assert.That(player.GetComponent<EchoRecorder>().ReadyRecording, Is.Null);
            Assert.That(plate.IsPressed, Is.False);
            Assert.That(door.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator CombinedResetClearsLiveAbilitiesHeldRmbAndKeepsOtherRooms()
        {
            var room = GameObject.Find("S04-C Combined Puzzle").transform;
            var checkpoint = room.GetComponentInChildren<CheckpointTrigger>();
            var platform = room.GetComponentInChildren<RewindMovingPlatform>();
            var plate = room.GetComponentInChildren<PressurePlate>();
            var recorder = player.GetComponent<EchoRecorder>();
            var playback = player.GetComponent<EchoPlayback>();
            player.Respawn(checkpoint.SpawnPoint.position, checkpoint.SpawnPoint.rotation);
            yield return Frames(160);
            Assert.That(manager.ActiveCheckpoint, Is.SameAs(checkpoint));
            var foreign = GameObject.Find("S03-C Rewind Crossing").GetComponentInChildren<RewindMovingPlatform>();
            var foreignPosition = foreign.transform.position;
            var foreignHistory = History(foreign);
            var foreignTime = foreignHistory.Time;

            player.Respawn(plate.transform.position + Vector3.up * 0.15f, Quaternion.identity);
            yield return Frames(4);
            Assert.That(recorder.Begin(), Is.True);
            yield return Frames(60);
            Assert.That(recorder.Stop(), Is.True);
            player.Respawn(room.TransformPoint(new Vector3(0f, 0.05f, 16.7f)), Quaternion.identity);
            yield return Frames(4);
            Assert.That(playback.TryStart(), Is.True);
            yield return Frames(4);
            Assert.That(plate.EchoPresent, Is.True);
            AimAt(platform, room);
            HoldRewind(true);
            yield return Frames(4);
            Assert.That(platform.Mode, Is.EqualTo(RewindMode.Rewinding));
            yield return Pulse(Key.R);
            Assert.That(playback.ActiveActor, Is.Null);
            Assert.That(recorder.State, Is.EqualTo(EchoState.Available));
            Assert.That(recorder.ReadyRecording, Is.Null);
            Assert.That(plate.IsPressed, Is.False);
            Assert.That(platform.Mode, Is.EqualTo(RewindMode.Normal));
            Assert.That(History(platform).Time, Is.LessThan(0.2d));
            Assert.That(History(platform).OldestTime, Is.Zero);
            Assert.That(Vector3.Distance(player.transform.position, checkpoint.SpawnPoint.position), Is.LessThan(0.2f));
            yield return Frames(150);
            Assert.That(platform.Mode, Is.EqualTo(RewindMode.Normal), "Held RMB cannot restart a session after R.");
            Assert.That(Vector3.Distance(platform.transform.position, room.Find("Platform End").position), Is.LessThan(0.03f));
            HoldRewind(false);
            yield return Frames(3);

            Assert.That(recorder.Begin(), Is.True);
            yield return Frames(10);
            player.Respawn(room.TransformPoint(new Vector3(0f, -5.2f, 22f)), Quaternion.identity);
            yield return Frames(4);
            Assert.That(recorder.State, Is.EqualTo(EchoState.Available), "Room fall volume cancels the active recording.");
            Assert.That(recorder.SampleCount, Is.Zero);
            Assert.That(Vector3.Distance(player.transform.position, checkpoint.SpawnPoint.position), Is.LessThan(0.2f));
            Assert.That(foreign.transform.position, Is.EqualTo(foreignPosition));
            Assert.That(foreignHistory.Time, Is.EqualTo(foreignTime));
            Debug.Log("S04C CHECK: R/fall cleared live Echo, RMB and room history; held RMB blocked; Room B intact.");
        }

        private static RewindHistory History(RewindableMechanism mechanism) => (RewindHistory)
            typeof(RewindableMechanism).GetField("history", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(mechanism);

        private void AimAt(RewindMovingPlatform platform, Transform room)
            => AimFrom(platform, room.TransformPoint(new Vector3(0f, 2f, 12.7f)));

        private void AimFrom(RewindMovingPlatform platform, Vector3 position)
        {
            if (aimCamera == null)
            {
                aimCamera = new GameObject("Test selection camera").AddComponent<Camera>();
                aimCamera.enabled = false;
                typeof(RewindInteractor).GetField("selectionCamera", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(player.GetComponent<RewindInteractor>(), aimCamera);
            }
            aimCamera.transform.position = position;
            aimCamera.transform.rotation = Quaternion.LookRotation(platform.transform.position - aimCamera.transform.position);
        }
        private void HoldRewind(bool held) => InputSystem.QueueStateEvent(mouse,
            new MouseState().WithButton(MouseButton.Right, held));

        private IEnumerator BoardAndRide(RewindMovingPlatform platform, Vector3 direction, float jumpAt, bool alongX)
        {
            basis.rotation = Quaternion.LookRotation(direction);
            var jumped = false;
            var landed = false;
            for (var frame = 0; frame < 150; frame++)
            {
                var position = alongX ? player.transform.position.x : player.transform.position.z;
                var shouldJump = !jumped && position >= jumpAt;
                if (shouldJump) jumped = true;
                if (jumped && !shouldJump && player.GetComponent<CharacterController>().isGrounded
                    && position > jumpAt + 1.3f) { landed = true; break; }
                if (shouldJump) Keys(Key.W, Key.Space); else Keys(Key.W);
                yield return null;
            }
            Keys();
            Assert.That(landed, Is.True, "Scripted jump must land on the solid moving platform.");
            yield return Frames(130);
            Assert.That(player.GetComponent<CharacterController>().isGrounded, Is.True, "Rider remains supported during the trip.");
            var distance = Vector3.Distance(player.transform.position, platform.transform.position);
            Assert.That(distance, Is.LessThan(1.7f), "Rider travels with the platform instead of slipping off.");
        }

        private IEnumerator WalkTo(Vector3 target)
        {
            for (var frame = 0; frame < 400; frame++)
            {
                var delta = target - player.transform.position;
                delta.y = 0f;
                if (delta.magnitude < 0.45f) break;
                basis.rotation = Quaternion.LookRotation(delta);
                Keys(Key.W);
                yield return null;
            }
            Keys();
            yield return Frames(18);
            var remaining = target - player.transform.position;
            remaining.y = 0f;
            Assert.That(remaining.magnitude, Is.LessThan(0.9f), "Walk target unreachable: " + target + "; player=" + player.transform.position);
        }

        private IEnumerator Pulse(Key key)
        {
            Keys(key);
            yield return Frames(3);
            Keys();
            yield return Frames(4);
        }
        private void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        private static IEnumerator Frames(int count) { for (var i = 0; i < count; i++) yield return null; }
    }
}

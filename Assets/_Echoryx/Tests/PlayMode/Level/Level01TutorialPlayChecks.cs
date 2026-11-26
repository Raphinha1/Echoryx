using System.Collections;
using System.Linq;
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
    /// Plays the intended route of Level_01_Tutorial with real input, motor, collisions and transport.
    /// A fixed camera basis makes walking reproducible; aiming uses a test camera (as the S04 checks do).
    /// </summary>
    public sealed class Level01TutorialPlayChecks
    {
        private Keyboard keyboard;
        private Mouse mouse;
        private Camera aimCamera;
        private Transform basis;
        private PlayerMotor player;
        private CheckpointManager manager;
        private LevelStats stats;
        private InputSettings originalSettings;
        private InputSettings testSettings;
        private HideFlags originalFlags;
        private MemoryProgressStorage progressStorage;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            progressStorage = new MemoryProgressStorage();
            GameProgress.Storage = progressStorage; // completing the level must not touch real PlayerPrefs
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Echoryx/Scenes/Level_01_Tutorial.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("These checks load the authored scene in Unity Editor.");
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
            basis = new GameObject("Test movement camera basis").transform;
            basis.rotation = Quaternion.identity;
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
            GameProgress.Storage = null;
            Time.captureDeltaTime = 0f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator IntendedRouteCompletesLevelWithEchoAndRewind()
        {
            var display = Object.FindFirstObjectByType<TutorialHintDisplay>();
            var panel = Object.FindFirstObjectByType<LevelCompletePanel>();
            var plate = Object.FindFirstObjectByType<PressurePlate>();
            var door = Object.FindFirstObjectByType<DoorController>();
            var platform = Object.FindFirstObjectByType<RewindMovingPlatform>();
            var platformStart = platform.transform.parent.Find("Platform Start").position;
            var platformEnd = platform.transform.parent.Find("Platform End").position;
            Assert.That(display.CurrentText, Does.Contain("WASD"), "Room 1 shows the controls hint at the start.");
            Assert.That(panel.IsVisible, Is.False);

            // Room 1: falling into the pit and R both count as attempts.
            yield return WalkTo(new Vector3(1.2f, 0f, 6f));
            Keys(Key.W);
            for (var frame = 0; frame < 240 && stats.Attempts < 1; frame++) yield return null;
            Keys();
            yield return Frames(10);
            Assert.That(player.transform.position.z, Is.LessThan(1f), "Pit hazard returns the player to the start.");
            Assert.That(stats.Attempts, Is.EqualTo(1));
            yield return Pulse(Key.R);
            Assert.That(stats.Attempts, Is.EqualTo(2));

            // Room 1: jump the gap and reach the checkpoint.
            yield return WalkTo(new Vector3(1.2f, 0f, 6f));
            Keys(Key.W);
            for (var frame = 0; frame < 60 && player.transform.position.z < 7f; frame++) yield return null;
            Keys(Key.W, Key.Space);
            yield return Frames(1);
            Keys(Key.W);
            yield return Frames(70);
            Assert.That(player.transform.position.z, Is.GreaterThan(10.9f), "A normal jump crosses the 2.5 m gap.");
            Assert.That(player.GetComponent<CharacterController>().isGrounded, Is.True);
            yield return WalkTo(new Vector3(0f, 0f, 15.5f));
            Assert.That(manager.ActiveCheckpoint, Is.Not.Null);
            Assert.That(manager.ActiveCheckpoint.name, Is.EqualTo("Checkpoint End Room 1"));

            // Room 2: the plate alone cannot open the door for a crossing player.
            yield return WalkTo(new Vector3(0f, 0f, 18.5f));
            Assert.That(display.CurrentText, Does.StartWith("¿Cómo puedes mantener la placa"));
            yield return WalkTo(new Vector3(0f, 0f, 21f));
            Assert.That(plate.IsPressed, Is.True);
            Assert.That(plate.PlayerPresent, Is.True);
            // Interaction beacon: green while the player holds the plate (idle was orange), particles alive.
            var plateBeacon = plate.GetComponentInChildren<InteractionBeacon>();
            Assert.That(plateBeacon.TargetColor.g, Is.GreaterThan(plateBeacon.TargetColor.r), "Green when held by the player.");
            Assert.That(plateBeacon.TargetIntensity, Is.GreaterThan(0.7f));
            Assert.That(plateBeacon.Trail.isPlaying, Is.True);

            // Record on the plate with the production five-second automatic stop, then replay it.
            var recorder = player.GetComponent<EchoRecorder>();
            yield return Pulse(Key.Q);
            yield return Frames(310);
            Assert.That(recorder.State, Is.EqualTo(EchoState.Ready));
            yield return Pulse(Key.E);
            Assert.That(recorder.State, Is.EqualTo(EchoState.Playing));
            // The Echo is the same robot in the cyan hologram variant, still without colliders.
            var echoActor = player.GetComponent<EchoPlayback>().ActiveActor;
            Assert.That(echoActor.GetComponentInChildren<RobotAnimationDriver>(), Is.Not.Null);
            Assert.That(echoActor.GetComponentsInChildren<Collider>(), Is.Empty);
            Assert.That(echoActor.GetComponentsInChildren<Renderer>().Where(r => r.enabled).All(r => r.sharedMaterial.name == "EchoHologram"), Is.True);
            yield return WalkTo(new Vector3(0f, 0f, 33f));
            Assert.That(player.transform.position.z, Is.GreaterThan(31f), "The Echo held the plate while the player crossed.");
            Assert.That(plate.EchoPresent && !plate.PlayerPresent, Is.True);
            Assert.That(plateBeacon.TargetColor.b, Is.GreaterThan(plateBeacon.TargetColor.r), "Cyan when only the Echo holds it.");
            yield return Frames(320);
            Assert.That(recorder.State, Is.EqualTo(EchoState.Ready), "A finished playback keeps the recording for replay.");
            Assert.That(door.IsOpen, Is.False, "Door closes behind the player once the Echo is gone.");
            yield return WalkTo(new Vector3(0f, 0f, 34.5f));
            Assert.That(manager.ActiveCheckpoint.name, Is.EqualTo("Checkpoint End Room 2"));

            // Room 3: the platform already left. Falling once and respawning must leave it parked and
            // out of reach again: the only way across is still Rewind.
            Assert.That(Vector3.Distance(platform.transform.position, platformEnd), Is.LessThan(0.05f));
            yield return RunAndJumpAtTheEdge(3);
            Assert.That(Vector3.Distance(platform.transform.position, platformEnd), Is.LessThan(0.02f), "Parked again after the respawn.");
            Assert.That(manager.ActiveCheckpoint.name, Is.EqualTo("Checkpoint End Room 2"));
            Assert.That(recorder.State, Is.EqualTo(EchoState.Ready), "The recording survives the respawn.");
            yield return WalkTo(new Vector3(0f, 0f, 33f));
            Assert.That(display.CurrentText, Does.Contain("rebobinar su movimiento"));
            yield return WalkTo(new Vector3(0f, 0f, 35.2f));
            Assert.That(display.CurrentText, Does.Contain("otra estrategia"));
            var idleBeacon = platform.GetComponentInChildren<InteractionBeacon>();
            Assert.That(idleBeacon.TargetIntensity, Is.EqualTo(0.75f).Within(0.01f), "Bright when it can be rewound and the player is in range.");
            Assert.That(idleBeacon.Trail.isPlaying, Is.True);
            AimFrom(platform, new Vector3(0f, 2.1f, 33f));
            HoldRewind(true);
            yield return Frames(3);
            Assert.That(platform.Mode, Is.EqualTo(RewindMode.Rewinding));
            var platformBeacon = platform.GetComponentInChildren<InteractionBeacon>();
            yield return Frames(2);
            Assert.That(platformBeacon.TargetIntensity, Is.EqualTo(1f), "Strongest while rewinding.");
            Assert.That(platformBeacon.TargetColor.r, Is.GreaterThan(platformBeacon.TargetColor.g), "Violet.");
            var feedback = platform.GetComponentInChildren<RewindMechanismFeedback>();
            yield return Frames(2);
            Assert.That(feedback.Glowing, Is.True, "Platform glows while being rewound.");
            yield return Frames(108);
            Assert.That(Vector3.Distance(platform.transform.position, platformStart), Is.LessThan(0.05f));
            yield return WalkTo(platformStart);
            HoldRewind(false);
            yield return Frames(3);
            Assert.That(platform.Mode, Is.EqualTo(RewindMode.Normal));
            yield return Frames(2);
            Assert.That(feedback.Glowing, Is.False, "Glow stops when Rewind is released.");
            yield return Frames(168);
            Assert.That(Vector3.Distance(platform.transform.position, platformEnd), Is.LessThan(0.1f));
            Assert.That(player.GetComponent<CharacterController>().isGrounded, Is.True);
            Assert.That(Vector3.Distance(player.transform.position, platform.transform.position), Is.LessThan(2.2f));
            Assert.That(player.GetComponentInChildren<RobotAnimationDriver>().Speed, Is.LessThan(0.3f), "Riding the platform standing still is not animated as walking.");

            // Goal.
            yield return WalkTo(new Vector3(0f, 0f, 51f));
            yield return Frames(3);
            Assert.That(stats.IsCompleted, Is.True);
            Assert.That(panel.IsVisible, Is.True);
            Assert.That(stats.Attempts, Is.EqualTo(3));
            Assert.That(stats.RewindUses, Is.EqualTo(1));
            Assert.That(stats.ElapsedSeconds, Is.GreaterThan(10d));

            // Reaching the real goal saves progress and offers the way on.
            var record = new GameProgress(progressStorage).Find("level-01-tutorial");
            Assert.That(record, Is.Not.Null);
            Assert.That(record.bestAttempts, Is.EqualTo(3));
            Assert.That(record.bestRewinds, Is.EqualTo(1));
            Assert.That(record.bestSeconds, Is.EqualTo(stats.ElapsedSeconds).Within(1e-6d));
            var flow = Object.FindFirstObjectByType<LevelFlow>();
            Assert.That(flow.State, Is.EqualTo(LevelFlowState.Completed));
            Assert.That(flow.CompleteRetryButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(flow.CompleteMenuButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(flow.AllCompletedLabel.gameObject.activeInHierarchy, Is.True, "Only one level ships today.");
            Assert.That(flow.NextLevelButton.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator AfterFallingTheRewindPlatformIsParkedOutOfReachAtEveryMoment()
        {
            var platform = Object.FindFirstObjectByType<RewindMovingPlatform>();
            var surface = platform.GetComponentInChildren<BoxCollider>();
            var end = platform.transform.parent.Find("Platform End").position;
            var door = Object.FindFirstObjectByType<DoorController>();
            var plate = Object.FindFirstObjectByType<PressurePlate>();
            var second = GameObject.Find("Checkpoint End Room 2").GetComponent<CheckpointTrigger>();
            player.Respawn(second.SpawnPoint.position, second.SpawnPoint.rotation);
            yield return Frames(200);
            Assert.That(manager.ActiveCheckpoint, Is.SameAs(second));
            Assert.That(Vector3.Distance(platform.transform.position, end), Is.LessThan(0.02f), "Level state: platform parked far away.");

            // Maximum horizontal reach of a jump from the lobby edge (run speed, air time and coyote time).
            var edge = GameObject.Find("Floor 2 Main").GetComponent<BoxCollider>().bounds.max.z;
            var gap = surface.bounds.min.z - edge;
            Assert.That(gap, Is.GreaterThan(MaximumJumpReach() + 0.25f), "The parked platform is beyond any jump.");

            for (var attempt = 1; attempt <= 2; attempt++)
            {
                yield return RunAndJumpAtTheEdge(attempt);
                Assert.That(Vector3.Distance(player.transform.position, second.SpawnPoint.position), Is.LessThan(0.6f));
                Assert.That(stats.Attempts, Is.EqualTo(attempt), "A fall counts as one attempt.");
                Assert.That(stats.IsCompleted, Is.False);
                // After the reset the platform is in the SAME state as when the level loaded: far, idle, rewindable.
                Assert.That(platform.Mode, Is.EqualTo(RewindMode.Normal));
                Assert.That(platform.CanRewind, Is.True, "Rewind can bring it back.");
                for (var frame = 0; frame < 300; frame++)
                {
                    Assert.That(Vector3.Distance(platform.transform.position, end), Is.LessThan(0.02f), "frame " + frame + ": it never enters the reachable zone.");
                    yield return null;
                }
                Assert.That(surface.bounds.min.z - edge, Is.GreaterThan(MaximumJumpReach() + 0.25f));
                // The other mechanisms of the route are coherent: plate idle, door closed, no clone.
                Assert.That(plate.IsPressed, Is.False);
                Assert.That(door.IsOpen, Is.False);
                Assert.That(player.GetComponent<EchoPlayback>().ActiveActor, Is.Null);
            }
        }

        [UnityTest]
        public IEnumerator RecordingReplaysRepeatedlyWithTheSameResultAndIsReplacedByANewOne()
        {
            var recorder = player.GetComponent<EchoRecorder>();
            var playback = player.GetComponent<EchoPlayback>();
            basis.rotation = Quaternion.identity;
            yield return Pulse(Key.Q);
            Keys(Key.W);
            yield return Frames(90);
            Keys();
            yield return Pulse(Key.Q);
            Assert.That(recorder.State, Is.EqualTo(EchoState.Ready));
            var first = recorder.ReadyRecording;
            var origin = first[0].Position;

            var samples = new System.Collections.Generic.List<Vector3[]>();
            for (var run = 0; run < 3; run++)
            {
                Keys(Key.E);
                yield return null;
                Keys();
                Assert.That(recorder.State, Is.EqualTo(EchoState.Playing), "run " + run);
                var actor = playback.ActiveActor;
                var points = new Vector3[3];
                points[0] = actor.transform.position;
                yield return Frames(20);
                points[1] = actor.transform.position;
                // E while the Echo is active is ignored: the same clone keeps playing, nothing restarts.
                yield return Pulse(Key.E);
                Assert.That(playback.ActiveActor, Is.SameAs(actor), "run " + run);
                points[2] = actor.transform.position;
                samples.Add(points);
                yield return Frames(Mathf.CeilToInt((float)first.Duration * 60f) + 20);
                Assert.That(recorder.State, Is.EqualTo(EchoState.Ready), "Recording kept after run " + run);
                Assert.That(recorder.ReadyRecording, Is.SameAs(first));
                Assert.That(playback.ActiveActor, Is.Null);
            }
            for (var run = 1; run < 3; run++)
                for (var i = 0; i < 3; i++)
                    Assert.That(Vector3.Distance(samples[run][i], samples[0][i]), Is.LessThan(0.001f), "run " + run + " point " + i);
            Assert.That(Vector3.Distance(samples[0][0], origin), Is.LessThan(0.05f), "Starts at the recorded origin.");
            Assert.That(samples[0][1].z, Is.GreaterThan(samples[0][0].z + 0.1f), "It follows the recorded path.");

            // Respawn keeps the recording.
            yield return Pulse(Key.R);
            Assert.That(recorder.ReadyRecording, Is.SameAs(first));

            // A new recording (elsewhere, other direction) replaces it; E now plays the new one.
            yield return WalkTo(new Vector3(1.5f, 0f, 2f));
            yield return Pulse(Key.Q);
            basis.rotation = Quaternion.LookRotation(Vector3.right);
            Keys(Key.W);
            yield return Frames(40);
            Keys();
            yield return Pulse(Key.Q);
            var second = recorder.ReadyRecording;
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.Duration, Is.Not.EqualTo(first.Duration).Within(0.05d));
            yield return Pulse(Key.E);
            Assert.That(Vector3.Distance(playback.ActiveActor.transform.position, second[0].Position), Is.LessThan(0.05f));
            Assert.That(Vector3.Distance(playback.ActiveActor.transform.position, origin), Is.GreaterThan(1f), "The old recording is gone.");
        }

        [UnityTest]
        public IEnumerator DoorLeafAndPlatformVisualsFollowTheirLogicalObjects()
        {
            // Platform (it only travels during the first seconds after load): the visual is a child of the moving root, non-static, and tracks its collider while it travels.
            var platform = Object.FindFirstObjectByType<RewindMovingPlatform>();
            var mesh = platform.GetComponentInChildren<RewindMechanismFeedback>().GetComponentInChildren<MeshRenderer>();
            var collider = platform.GetComponentInChildren<BoxCollider>();
            Assert.That(mesh.gameObject.isStatic, Is.False);
            var start = collider.bounds.center;
            yield return Frames(30);
            Assert.That(Vector3.Distance(collider.bounds.center, start), Is.GreaterThan(0.5f), "Platform moved.");
            Assert.That(Vector3.Distance(mesh.transform.position, platform.transform.position + Vector3.down * 0.2f), Is.LessThan(0.01f), "Visual keeps its authored offset from the moving root.");
            Assert.That(Mathf.Abs(mesh.bounds.center.z - collider.bounds.center.z), Is.LessThan(0.3f));

            var door = Object.FindFirstObjectByType<DoorController>();
            var follower = door.GetComponentInChildren<SmoothVisualFollow>();
            var panel = door.transform.Find("Door Panel");
            var offset = (Vector3)typeof(SmoothVisualFollow).GetField("offset", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(follower);
            Vector3 Expected() => panel.position + panel.rotation * offset;
            Assert.That(follower.gameObject.isStatic, Is.False);
            var closedY = follower.transform.position.y;
            Assert.That(Vector3.Distance(follower.transform.position, Expected()), Is.LessThan(0.05f));

            // Standing on the plate opens the door; the visual leaf must travel with the logical panel.
            player.Respawn(new Vector3(0f, 0.05f, 21f), Quaternion.identity);
            var maxLag = 0f;
            for (var frame = 0; frame < 120; frame++)
            {
                maxLag = Mathf.Max(maxLag, Vector3.Distance(follower.transform.position, Expected()));
                yield return null;
            }
            Assert.That(door.IsOpen, Is.True);
            Assert.That(follower.transform.position.y, Is.GreaterThan(closedY + 5f), "Leaf visibly rose.");
            Assert.That(Vector3.Distance(follower.transform.position, Expected()), Is.LessThan(0.05f), "Leaf settles on the panel.");
            Assert.That(maxLag, Is.LessThan(2.5f), "Easing lag stays bounded.");

            // Step off: the door closes and the leaf follows it back down.
            player.Respawn(new Vector3(0f, 0.05f, 12f), Quaternion.identity);
            yield return Frames(120);
            Assert.That(door.IsOpen, Is.False);
            Assert.That(follower.transform.position.y, Is.EqualTo(closedY).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator CameraDoesNotPassThroughWallsOrTheClosedDoor()
        {
            var orbit = Object.FindFirstObjectByType<PlayerCameraOrbit>();
            var yaw = typeof(PlayerCameraOrbit).GetField("yaw", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var camera = Camera.main;
            Assert.That(camera.GetComponent<Unity.Cinemachine.CinemachineBrain>(), Is.Not.Null);

            // Backed against the right wall (collider inner face x = 3.0): camera would sit at x = 6.4 without avoidance.
            player.Respawn(new Vector3(2.4f, 0.05f, 12f), Quaternion.identity);
            yaw.SetValue(orbit, -90f);
            orbit.SnapToAnchor();
            var worst = float.MinValue;
            for (var frame = 0; frame < 120; frame++) { yield return null; if (frame > 40) worst = Mathf.Max(worst, camera.transform.position.x); }
            Assert.That(worst, Is.LessThan(3.0f), "Camera stays inside the right wall.");

            // Same on the left wall.
            player.Respawn(new Vector3(-2.4f, 0.05f, 12f), Quaternion.identity);
            yaw.SetValue(orbit, 90f);
            orbit.SnapToAnchor();
            worst = float.MaxValue;
            for (var frame = 0; frame < 120; frame++) { yield return null; if (frame > 40) worst = Mathf.Min(worst, camera.transform.position.x); }
            Assert.That(worst, Is.GreaterThan(-3.0f), "Camera stays inside the left wall.");

            // In front of the closed door (panel collider face z = 29.7): the camera must not end up behind it.
            player.Respawn(new Vector3(0f, 0.05f, 28.9f), Quaternion.identity);
            yaw.SetValue(orbit, 180f);
            orbit.SnapToAnchor();
            worst = float.MinValue;
            for (var frame = 0; frame < 120; frame++) { yield return null; if (frame > 40) worst = Mathf.Max(worst, camera.transform.position.z); }
            Assert.That(worst, Is.LessThan(29.7f), "Camera does not pass through the closed door.");
        }

        private IEnumerator RunAndJumpAtTheEdge(int expectedAttempts)
        {
            basis.rotation = Quaternion.identity;
            Keys(Key.W);
            for (var frame = 0; frame < 120 && player.transform.position.z < 35.3f; frame++) yield return null;
            Keys(Key.W, Key.Space);
            yield return Frames(1);
            Keys(Key.W);
            for (var frame = 0; frame < 300 && stats.Attempts < expectedAttempts; frame++) yield return null;
            Keys();
            yield return Frames(3);
        }

        private float MaximumJumpReach()
        {
            float Field(string name) => (float)typeof(PlayerMotor).GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(player);
            var gravity = Mathf.Abs(Field("gravity"));
            var airTime = 2f * Mathf.Sqrt(2f * Field("jumpHeight") * gravity) / gravity;
            return Field("moveSpeed") * (airTime + Field("coyoteTime"));
        }

        // ---------------------------------------------------------------- helpers

        private void AimFrom(RewindMovingPlatform platform, Vector3 position)
        {
            if (aimCamera == null)
            {
                aimCamera = new GameObject("Test selection camera").AddComponent<Camera>();
                aimCamera.enabled = false;
                typeof(RewindInteractor).GetField("selectionCamera",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(player.GetComponent<RewindInteractor>(), aimCamera);
            }
            aimCamera.transform.position = position;
            aimCamera.transform.rotation = Quaternion.LookRotation(platform.transform.position - position);
        }

        private void HoldRewind(bool held) => InputSystem.QueueStateEvent(mouse,
            new MouseState().WithButton(MouseButton.Right, held));

        private IEnumerator WalkTo(Vector3 target)
        {
            for (var frame = 0; frame < 500; frame++)
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
            Assert.That(remaining.magnitude, Is.LessThan(0.9f),
                "Walk target unreachable: " + target + "; player=" + player.transform.position);
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

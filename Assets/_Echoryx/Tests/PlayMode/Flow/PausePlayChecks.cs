using System.Collections;
using System.Reflection;
using Echoryx.Echo;
using Echoryx.Game;
using Echoryx.Interactions;
using Echoryx.Level;
using Echoryx.Player;
using Echoryx.Rewind;
using Echoryx.Rewind.Platforms;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace Echoryx.Flow.PlayTests
{
    /// <summary>Pause, resume and restart on Level_01 with real input, checking Echo and Rewind survive it.</summary>
    public sealed class PausePlayChecks : FlowPlayTest
    {
        private LevelFlow flow;
        private CheckpointManager manager;
        private PlayerMotor player;
        private LevelStats stats;
        private Transform basis;
        private Camera aimCamera;

        [UnitySetUp]
        public IEnumerator LoadLevel()
        {
            yield return Load(LevelScene);
            Bind();
            yield return MoveToClearLane();
        }

        // The right-hand lane: walking forward from here does not run into the practice step.
        private IEnumerator MoveToClearLane()
        {
            player.Respawn(new Vector3(1.5f, 0.05f, -1f), Quaternion.identity);
            yield return Frames(8);
        }

        [UnityTearDown]
        public IEnumerator DestroyHelpers()
        {
            if (basis != null) Object.Destroy(basis.gameObject);
            if (aimCamera != null) Object.Destroy(aimCamera.gameObject);
            yield return null;
        }

        private void Bind()
        {
            flow = Object.FindFirstObjectByType<LevelFlow>();
            manager = Object.FindFirstObjectByType<CheckpointManager>();
            stats = Object.FindFirstObjectByType<LevelStats>();
            player = manager.Player;
            if (basis == null) basis = new GameObject("Test movement basis").transform;
            player.SetCameraReference(basis);
        }

        [UnityTest]
        public IEnumerator EscapePausesAndResumesWithoutLettingGameplayInputThrough()
        {
            Assert.That(flow, Is.Not.Null, "Catalog levels receive the flow automatically.");
            Assert.That(flow.IsPauseMenuVisible, Is.False);
            var recorder = player.GetComponent<EchoRecorder>();
            var orbit = Object.FindFirstObjectByType<PlayerCameraOrbit>();
            yield return Pulse(Key.Escape);
            Assert.That(flow.State, Is.EqualTo(LevelFlowState.Paused));
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            Assert.That(flow.IsPauseMenuVisible, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(flow.ResumeButton.gameObject));
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));

            var position = player.transform.position;
            var look = orbit.transform.rotation;
            var clock = Time.timeAsDouble;
            var elapsed = stats.ElapsedSeconds;
            yield return Pulse(Key.Q);
            yield return Pulse(Key.E);
            yield return Pulse(Key.R);
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(80f, 30f) });
            Keys(Key.W, Key.Space);
            yield return Frames(60);
            Keys();
            yield return Frames(2);
            Assert.That(Time.timeAsDouble, Is.EqualTo(clock), "Scaled time is frozen.");
            Assert.That(stats.ElapsedSeconds, Is.EqualTo(elapsed), "Paused time does not count.");
            Assert.That(recorder.State, Is.EqualTo(EchoState.Available), "Q and E are ignored.");
            Assert.That(stats.Attempts, Is.EqualTo(0), "R is ignored.");
            Assert.That(player.transform.position, Is.EqualTo(position));
            Assert.That(orbit.transform.rotation, Is.EqualTo(look), "The camera does not turn behind the menu.");

            yield return Pulse(Key.Escape);
            Assert.That(flow.State, Is.EqualTo(LevelFlowState.Playing));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(flow.IsPauseMenuVisible, Is.False);
            yield return Frames(20);
            Assert.That(player.GetComponent<CharacterController>().isGrounded, Is.True);
            Assert.That(player.transform.position.y, Is.EqualTo(position.y).Within(0.05f), "No jump buffered while paused.");
            Keys(Key.W);
            yield return Frames(45);
            Assert.That(player.PlanarSpeed, Is.EqualTo(6f).Within(0.05f));
            Assert.That(player.GetComponentInChildren<RobotAnimationDriver>().Speed, Is.GreaterThan(5f), "The robot animates again.");
        }

        [UnityTest]
        public IEnumerator ResumeButtonWorksFromTheKeyboardAndHeldKeysNeedANewPress()
        {
            Keys(Key.W);
            yield return Frames(30);
            Keys(Key.W, Key.Escape);
            yield return Frames(2);
            Keys(Key.W);
            yield return Frames(10);
            Assert.That(flow.State, Is.EqualTo(LevelFlowState.Paused));
            Keys(Key.W, Key.Enter); // submit the selected Resume button
            yield return Frames(2);
            Keys(Key.W);
            yield return Frames(40);
            Assert.That(flow.State, Is.EqualTo(LevelFlowState.Playing));
            Assert.That(player.PlanarSpeed, Is.LessThan(0.05f), "W held through the pause is not applied until pressed again.");
            Keys();
            yield return Frames(3);
            Keys(Key.W);
            yield return Frames(40);
            Assert.That(player.PlanarSpeed, Is.EqualTo(6f).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator PausingWhileRecordingKeepsTheRecordingAndExcludesPausedTime()
        {
            var recorder = player.GetComponent<EchoRecorder>();
            yield return Pulse(Key.Q);
            yield return Frames(60);
            Assert.That(recorder.State, Is.EqualTo(EchoState.Recording));
            yield return Pulse(Key.Escape);
            var samples = recorder.SampleCount;
            yield return Frames(600); // ten paused seconds, twice the Echo limit
            Assert.That(recorder.State, Is.EqualTo(EchoState.Recording));
            Assert.That(recorder.SampleCount, Is.EqualTo(samples));
            yield return Pulse(Key.Escape);
            yield return Frames(30);
            Assert.That(recorder.State, Is.EqualTo(EchoState.Recording));
            yield return Pulse(Key.Q);
            Assert.That(recorder.State, Is.EqualTo(EchoState.Ready));
            // About 64 frames before the pause and 34 after it: ~1.65 s of play.
            Assert.That(recorder.ReadyRecording.Duration, Is.InRange(1.4d, 1.9d));
        }

        [UnityTest]
        public IEnumerator PausingDuringPlaybackFreezesTheEchoAndThenItFinishes()
        {
            var recorder = player.GetComponent<EchoRecorder>();
            var playback = player.GetComponent<EchoPlayback>();
            yield return Pulse(Key.Q);
            Keys(Key.W);
            yield return Frames(90);
            Keys();
            yield return Pulse(Key.Q);
            var duration = recorder.ReadyRecording.Duration;
            yield return Pulse(Key.E);
            yield return Frames(30);
            Assert.That(recorder.State, Is.EqualTo(EchoState.Playing));
            var actor = playback.ActiveActor;
            yield return Pulse(Key.Escape);
            var frozen = actor.transform.position;
            yield return Frames(400);
            Assert.That(playback.ActiveActor, Is.SameAs(actor));
            Assert.That(actor.transform.position, Is.EqualTo(frozen));
            Assert.That(recorder.State, Is.EqualTo(EchoState.Playing));
            yield return Pulse(Key.Escape);
            yield return Frames(10);
            Assert.That(playback.ActiveActor, Is.SameAs(actor), "The same clone carries on.");
            Assert.That(Vector3.Distance(actor.transform.position, frozen), Is.GreaterThan(0.05f));
            yield return Frames((int)(duration * 60d) + 10);
            Assert.That(playback.ActiveActor == null, Is.True);
            Assert.That(recorder.State, Is.EqualTo(EchoState.Ready), "The recording is still replayable.");
            var echoDriverSpeed = player.GetComponentInChildren<RobotAnimationDriver>().Speed;
            Assert.That(float.IsNaN(echoDriverSpeed), Is.False);
        }

        [UnityTest]
        public IEnumerator PausingDuringRewindReleasesItCleanlyAndItCanBeUsedAgain()
        {
            var platform = Object.FindFirstObjectByType<RewindMovingPlatform>();
            var end = platform.transform.parent.Find("Platform End").position;
            yield return GoToRewindRoom();
            Assert.That(Vector3.Distance(platform.transform.position, end), Is.LessThan(0.05f));
            AimAt(platform);
            HoldRewind(true);
            yield return Frames(30);
            Assert.That(platform.Mode, Is.EqualTo(RewindMode.Rewinding));
            Assert.That(stats.RewindUses, Is.EqualTo(1));

            Keys(Key.Escape);
            yield return Frames(2);
            Keys();
            yield return Frames(2);
            Assert.That(flow.State, Is.EqualTo(LevelFlowState.Paused));
            Assert.That(platform.Mode, Is.EqualTo(RewindMode.Normal), "Pause lets go of Rewind.");
            var frozen = platform.transform.position;
            var time = Timeline(platform);
            // Releasing and pressing the button behind the menu must not start anything.
            HoldRewind(false);
            yield return Frames(5);
            HoldRewind(true);
            yield return Frames(300);
            Assert.That(platform.Mode, Is.EqualTo(RewindMode.Normal));
            Assert.That(platform.transform.position, Is.EqualTo(frozen));
            Assert.That(Timeline(platform), Is.EqualTo(time), "The history does not age while paused.");
            Assert.That(platform.CanRewind, Is.True);

            yield return Pulse(Key.Escape);
            yield return Frames(20);
            Assert.That(platform.Mode, Is.EqualTo(RewindMode.Normal), "A button held through the pause needs a new press.");
            Assert.That(stats.RewindUses, Is.EqualTo(1), "Pausing never adds a Rewind use.");
            Assert.That(platform.transform.position.z, Is.GreaterThan(frozen.z), "Normal travel continues from the recovered time.");
            HoldRewind(false);
            yield return Frames(3);
            AimAt(platform);
            HoldRewind(true);
            yield return Frames(20);
            Assert.That(platform.Mode, Is.EqualTo(RewindMode.Rewinding));
            Assert.That(stats.RewindUses, Is.EqualTo(2));
            HoldRewind(false);
            yield return Frames(3);
            Assert.That(platform.Mode, Is.EqualTo(RewindMode.Normal));
        }

        [UnityTest]
        public IEnumerator APlayerRidingThePlatformIsStillCarriedAfterResuming()
        {
            var platform = Object.FindFirstObjectByType<RewindMovingPlatform>();
            yield return GoToRewindRoom();
            // Rewind the parked platform to its start, step on it and let go.
            var start = platform.transform.parent.Find("Platform Start").position;
            AimAt(platform);
            HoldRewind(true);
            yield return Frames(115);
            yield return WalkTo(start);
            HoldRewind(false);
            yield return Frames(40);
            var controller = player.GetComponent<CharacterController>();
            Assert.That(controller.isGrounded, Is.True);
            var offset = player.transform.position - platform.transform.position;
            for (var cycle = 0; cycle < 3; cycle++)
            {
                yield return Pulse(Key.Escape);
                var held = player.transform.position;
                yield return Frames(30);
                Assert.That(player.transform.position, Is.EqualTo(held));
                yield return Pulse(Key.Escape);
                yield return Frames(8);
            }
            Assert.That(controller.isGrounded, Is.True);
            Assert.That(Vector3.Distance(player.transform.position - platform.transform.position, offset), Is.LessThan(0.03f),
                "No slip relative to the platform across pauses.");
        }

        [UnityTest]
        public IEnumerator RetryRestartsTheLevelAndPauseKeepsWorkingAfterReloading()
        {
            yield return Pulse(Key.R);
            Assert.That(stats.Attempts, Is.EqualTo(1), "R keeps its checkpoint-respawn meaning.");
            var firstStats = stats;
            yield return Pulse(Key.Escape);
            flow.RetryButton.onClick.Invoke();
            yield return Frames(5);
            Assert.That(firstStats == null, Is.True, "The scene was reloaded.");
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(Object.FindObjectsByType<LevelFlow>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Bind();
            Assert.That(flow.State, Is.EqualTo(LevelFlowState.Playing));
            Assert.That(stats.Attempts, Is.EqualTo(0));
            yield return Frames(5);
            yield return MoveToClearLane();
            Keys(Key.W);
            yield return Frames(30);
            Assert.That(player.PlanarSpeed, Is.EqualTo(6f).Within(0.05f), "Gameplay input works after reloading.");
            Keys();
            yield return Pulse(Key.Escape);
            Assert.That(flow.State, Is.EqualTo(LevelFlowState.Paused), "Pause input works after reloading.");
        }

        [UnityTest]
        public IEnumerator BackspaceRestartsTheWholeLevelOnceEvenWhenHeld()
        {
            var checkpoint = GameObject.Find("Checkpoint End Room 2").GetComponent<CheckpointTrigger>();
            player.Respawn(checkpoint.SpawnPoint.position, checkpoint.SpawnPoint.rotation);
            yield return Frames(5);
            yield return Pulse(Key.R);
            var firstStats = stats;
            Keys(Key.Backspace);
            yield return Frames(5);
            Assert.That(firstStats == null, Is.True);
            Bind();
            var reloaded = stats;
            yield return Frames(30);
            Assert.That(reloaded != null, Is.True, "Holding the key does not reload again.");
            Assert.That(stats.Attempts, Is.EqualTo(0));
            Assert.That(manager.ActiveCheckpoint, Is.Null);
            Assert.That(player.transform.position.z, Is.LessThan(1f), "Back at the start of the level.");
        }

        [UnityTest]
        public IEnumerator MainMenuFromPauseLeavesWithTimeRunning()
        {
            yield return Pulse(Key.Escape);
            flow.MenuButton.onClick.Invoke();
            yield return WaitForScene(MenuScene);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(Object.FindFirstObjectByType<MainMenuController>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<LevelFlow>(), Is.Null);
        }

        // ------------------------------------------------------------ helpers

        private IEnumerator GoToRewindRoom()
        {
            var checkpoint = GameObject.Find("Checkpoint End Room 2").GetComponent<CheckpointTrigger>();
            player.Respawn(checkpoint.SpawnPoint.position, checkpoint.SpawnPoint.rotation);
            yield return Frames(10);
            yield return WalkTo(new Vector3(0f, 0f, 35.2f));
            yield return Frames(200); // let the one-way platform finish its trip and park
        }

        private IEnumerator WalkTo(Vector3 target)
        {
            for (var frame = 0; frame < 600; frame++)
            {
                var delta = target - player.transform.position;
                delta.y = 0f;
                if (delta.magnitude < 0.25f) break;
                basis.rotation = Quaternion.LookRotation(delta.normalized);
                Keys(Key.W);
                yield return null;
            }
            Keys();
            basis.rotation = Quaternion.identity;
            yield return Frames(20);
        }

        private void AimAt(RewindMovingPlatform platform)
        {
            if (aimCamera == null)
            {
                aimCamera = new GameObject("Test aim camera").AddComponent<Camera>();
                aimCamera.enabled = false;
                var field = typeof(RewindInteractor).GetField("selectionCamera", BindingFlags.Instance | BindingFlags.NonPublic);
                field.SetValue(player.GetComponent<RewindInteractor>(), aimCamera);
            }
            aimCamera.transform.position = new Vector3(0f, 2.1f, 33f);
            aimCamera.transform.LookAt(platform.transform.position);
        }

        private void HoldRewind(bool held) =>
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Right, held));

        private static double Timeline(RewindableMechanism mechanism)
        {
            var field = typeof(RewindableMechanism).GetField("history", BindingFlags.Instance | BindingFlags.NonPublic);
            return ((RewindHistory)field.GetValue(mechanism)).Time;
        }
    }
}

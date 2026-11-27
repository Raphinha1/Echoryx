using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Echoryx.Echo;
using Echoryx.Game;
using Echoryx.Level;
using Echoryx.Player;
using Echoryx.Rewind;
using Echoryx.Rewind.Platforms;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;

namespace Echoryx.Flow.PlayTests
{
    /// <summary>
    /// Devices appearing and disappearing while the game's actions are live (menu, level, pause, level end,
    /// scene reloads) must log no errors and must never change the shape of the input asset. This guards the
    /// rule behind "count in old and new state must be the same": no runtime code adds or removes maps,
    /// actions or bindings. See docs/game-flow.md for that error's editor-only cause.
    /// </summary>
    public sealed class DeviceDiscoveryPlayChecks : FlowPlayTest
    {
        private readonly List<string> errors = new List<string>();
        private readonly List<InputDevice> discovered = new List<InputDevice>();
        private Transform basis;
        private Camera aimCamera;
        private string shape;

        [SetUp]
        public void Watch()
        {
            errors.Clear();
            Application.logMessageReceived += Collect;
            shape = Shape(LevelCatalog.Current.InputActions);
        }

        [UnityTearDown]
        public IEnumerator StopWatching()
        {
            Application.logMessageReceived -= Collect;
            RemoveDiscovered();
            if (basis != null) Object.Destroy(basis.gameObject);
            if (aimCamera != null) Object.Destroy(aimCamera.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator InTheMainMenuAndTheLevelSelector()
        {
            yield return Load(MenuScene);
            var menu = Object.FindFirstObjectByType<MainMenuController>();
            yield return DiscoverAndRemove();
            Discover();
            yield return Frames(3);
            menu.SelectButton.onClick.Invoke();
            yield return DiscoverAndRemove();
            Assert.That(menu.IsSelectingLevel, Is.True);
            menu.BackButton.onClick.Invoke();
            yield return Frames(2);
            AssertHealthy();
            yield return Pulse(Key.Enter); // the focused Play button still submits
            yield return WaitForScene(LevelScene);
            AssertHealthy();
        }

        [UnityTest]
        public IEnumerator WhilePlayingPausedAndResuming()
        {
            yield return Load(LevelScene);
            var flow = Object.FindFirstObjectByType<LevelFlow>();
            var player = Player();
            yield return DiscoverAndRemove();

            yield return Pulse(Key.Escape);
            yield return DiscoverAndRemove();
            Discover();
            yield return Frames(10);
            Assert.That(flow.State, Is.EqualTo(LevelFlowState.Paused));
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            AssertHealthy();

            // Discovery on the resume frame itself, then again while control comes back.
            Assert.That(flow.Resume(), Is.True);
            Discover();
            yield return null;
            Discover();
            yield return Frames(10);
            Assert.That(flow.State, Is.EqualTo(LevelFlowState.Playing));
            yield return WalkForwardInTheClearLane(player);
            AssertHealthy();
        }

        [UnityTest]
        public IEnumerator AGamepadDiscoveredWhilePausedCanResumeAndPauseWithStart()
        {
            yield return Load(LevelScene);
            var flow = Object.FindFirstObjectByType<LevelFlow>();
            yield return Pulse(Key.Escape);
            var pad = InputSystem.AddDevice<Gamepad>();
            discovered.Add(pad);
            yield return Frames(3);
            yield return Press(pad, GamepadButton.Start);
            Assert.That(flow.State, Is.EqualTo(LevelFlowState.Playing));
            yield return Frames(3);
            yield return Press(pad, GamepadButton.Start);
            Assert.That(flow.State, Is.EqualTo(LevelFlowState.Paused));
            AssertHealthy();
        }

        [UnityTest]
        public IEnumerator DuringEchoRecordingPlaybackAndAHeldRewind()
        {
            yield return Load(LevelScene);
            var player = Player();
            var recorder = player.GetComponent<EchoRecorder>();
            yield return Pulse(Key.Q);
            yield return DiscoverAndRemove();
            Assert.That(recorder.State, Is.EqualTo(EchoState.Recording));
            yield return Frames(30);
            yield return Pulse(Key.Q);
            yield return Pulse(Key.E);
            yield return DiscoverAndRemove();
            Assert.That(recorder.State, Is.EqualTo(EchoState.Playing));

            var platform = Object.FindFirstObjectByType<RewindMovingPlatform>();
            var stats = Object.FindFirstObjectByType<LevelStats>();
            var checkpoint = GameObject.Find("Checkpoint End Room 2").GetComponent<CheckpointTrigger>();
            player.Respawn(checkpoint.SpawnPoint.position, checkpoint.SpawnPoint.rotation);
            yield return Frames(220); // the one-way platform finishes its trip and parks
            AimAt(player, platform);
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Right));
            yield return Frames(15);
            Assert.That(platform.Mode, Is.EqualTo(RewindMode.Rewinding));
            yield return DiscoverAndRemove();
            AssertHealthy();
            // Not asserted either way: Input System 1.20.1 does not restore InputAction.IsPressed() when a new
            // device makes it re-resolve bindings, so today a held Rewind is let go here (docs/game-flow.md).
            // What must hold: letting go leaves a normal mechanism, and a new press rewinds again.
            InputSystem.QueueStateEvent(mouse, new MouseState());
            yield return Frames(5);
            Assert.That(platform.Mode, Is.EqualTo(RewindMode.Normal));
            aimCamera.transform.LookAt(platform.transform.position);
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Right));
            yield return Frames(10);
            Assert.That(platform.Mode, Is.EqualTo(RewindMode.Rewinding), "Rewind still works after discovery.");
            InputSystem.QueueStateEvent(mouse, new MouseState());
            yield return Frames(5);
            Assert.That(platform.Mode, Is.EqualTo(RewindMode.Normal));
            AssertHealthy();
        }

        [UnityTest]
        public IEnumerator AtLevelEndAndAcrossRetryAndMenuReloadsWithDevicesConnected()
        {
            yield return Load(LevelScene);
            Object.FindFirstObjectByType<LevelStats>().Complete();
            yield return Frames(2);
            yield return DiscoverAndRemove();
            Discover(); // stays connected through the reloads below
            yield return Frames(2);
            AssertHealthy();

            for (var round = 0; round < 2; round++)
            {
                Object.FindFirstObjectByType<LevelFlow>().Retry();
                yield return Frames(5);
                yield return DiscoverAndRemove();
                AssertHealthy();
                yield return Pulse(Key.Escape);
                Object.FindFirstObjectByType<LevelFlow>().GoToMenu();
                yield return WaitForScene(MenuScene);
                yield return DiscoverAndRemove();
                AssertHealthy();
                Object.FindFirstObjectByType<MainMenuController>().LoadLevel(0); // Play would continue to Level 02
                yield return WaitForScene(LevelScene);
                AssertHealthy();
            }

            RemoveDiscovered();
            yield return Frames(3);
            yield return WalkForwardInTheClearLane(Player());
            AssertHealthy();
        }

        // ------------------------------------------------------------ helpers

        private void Collect(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception) errors.Add(type + ": " + message);
        }

        private void AssertHealthy()
        {
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
            var actions = LevelCatalog.Current.InputActions;
            Assert.That(Shape(actions), Is.EqualTo(shape), "Maps, actions and bindings never change at run time.");
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                Is.EqualTo(1), "Exactly one EventSystem.");
            var modules = Object.FindObjectsByType<BaseInputModule>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(modules.Length, Is.EqualTo(1), "Exactly one UI input module.");
            Assert.That(modules[0], Is.InstanceOf<InputSystemUIInputModule>());
        }

        private static string Shape(InputActionAsset asset) =>
            string.Join("; ", asset.actionMaps.Select(m => m.name + ": " + m.actions.Count + " actions, " + m.bindings.Count + " bindings"));

        private void Discover()
        {
            discovered.Add(InputSystem.AddDevice<Keyboard>());
            discovered.Add(InputSystem.AddDevice<Mouse>());
            discovered.Add(InputSystem.AddDevice<Gamepad>());
            // The virtual devices that tests drive stay the current ones.
            keyboard.MakeCurrent();
            mouse.MakeCurrent();
        }

        private void RemoveDiscovered()
        {
            foreach (var device in discovered)
                if (device != null && device.added) InputSystem.RemoveDevice(device);
            discovered.Clear();
        }

        private IEnumerator DiscoverAndRemove()
        {
            var already = discovered.Count;
            Discover();
            yield return Frames(3);
            for (var i = discovered.Count - 1; i >= already; i--)
            {
                InputSystem.RemoveDevice(discovered[i]);
                discovered.RemoveAt(i);
            }
            yield return Frames(2);
        }

        private static IEnumerator Press(Gamepad pad, GamepadButton button)
        {
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(button));
            yield return Frames(2);
            InputSystem.QueueStateEvent(pad, new GamepadState());
            yield return Frames(2);
        }

        private static PlayerMotor Player() => Object.FindFirstObjectByType<CheckpointManager>().Player;

        private IEnumerator WalkForwardInTheClearLane(PlayerMotor player)
        {
            if (basis == null) basis = new GameObject("Test movement basis").transform;
            player.SetCameraReference(basis);
            player.Respawn(new Vector3(1.5f, 0.05f, -1f), Quaternion.identity);
            yield return Frames(8);
            Keys(Key.W);
            yield return Frames(30);
            Assert.That(player.PlanarSpeed, Is.EqualTo(6f).Within(0.05f), "Movement input still works.");
            Keys();
        }

        private void AimAt(PlayerMotor player, RewindMovingPlatform platform)
        {
            aimCamera = new GameObject("Test aim camera").AddComponent<Camera>();
            aimCamera.enabled = false;
            aimCamera.transform.position = new Vector3(0f, 2.1f, 33f);
            aimCamera.transform.LookAt(platform.transform.position);
            typeof(RewindInteractor).GetField("selectionCamera", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(player.GetComponent<RewindInteractor>(), aimCamera);
        }
    }
}

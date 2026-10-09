using Echoryx.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Echoryx.Game.Tests
{
    public sealed class InputReleaseGateTests
    {
        private InputSettings originalSettings;
        private InputSettings testSettings;
        private HideFlags originalFlags;

        [SetUp]
        public void SetUp()
        {
            originalSettings = InputSystem.settings;
            originalFlags = originalSettings.hideFlags;
            // Input System destroys its generated HideAndDontSave settings when replacing them.
            // Preserve that instance for restoration after this test.
            originalSettings.hideFlags = HideFlags.None;
            testSettings = Object.Instantiate(originalSettings);
            testSettings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            testSettings.SetInternalFeatureFlag("RUN_PLAYER_UPDATES_IN_EDIT_MODE", true);
            InputSystem.settings = testSettings;
        }

        [TearDown]
        public void TearDown()
        {
            InputSystem.settings = originalSettings;
            originalSettings.hideFlags = originalFlags;
            if (testSettings != null) Object.DestroyImmediate(testSettings);
        }

        [Test]
        public void HeldButtonRequiresReleaseAndFreshPressAfterReset()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var map = new InputActionMap("Test");
            var jump = map.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");
            try
            {
                map.Enable();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                InputSystem.Update();
                var gate = new InputReleaseGate();
                gate.Reset(map, 10);
                Assert.That(gate.CanRead(jump, 11), Is.False);
                Assert.That(gate.CanRead(jump, 12), Is.False);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                Assert.That(gate.CanRead(jump, 12), Is.True);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                InputSystem.Update();
                Assert.That(gate.CanRead(jump, 13), Is.True);
                Assert.That(jump.WasPressedThisFrame(), Is.True);
            }
            finally { map.Dispose(); InputSystem.RemoveDevice(keyboard); }
        }

        [Test]
        public void HeldMovementRequiresNeutralWhileUnheldActionsRemainAvailable()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var map = new InputActionMap("Test");
            var move = map.AddAction("Move", InputActionType.Value, "<Keyboard>/w");
            var echo = map.AddAction("Echo", InputActionType.Button, "<Keyboard>/q");
            try
            {
                map.Enable();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                InputSystem.Update();
                var gate = new InputReleaseGate();
                gate.Reset(map, 10);
                Assert.That(gate.CanRead(move, 12), Is.False);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Q));
                InputSystem.Update();
                Assert.That(gate.CanRead(echo, 12), Is.True);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                Assert.That(gate.CanRead(move, 13), Is.True);
            }
            finally { map.Dispose(); InputSystem.RemoveDevice(keyboard); }
        }
    }
}

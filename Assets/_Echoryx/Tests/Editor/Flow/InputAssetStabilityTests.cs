using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoryx.Flow.Tests
{
    /// <summary>The game's input asset keeps its shape while it is enabled, disabled and devices come and go.</summary>
    public sealed class InputAssetStabilityTests
    {
        [Test]
        public void TheCatalogUsesTheProjectWideActionsWithAllMapsDeclared()
        {
            var actions = LevelCatalog.Current.InputActions;
            Assert.That(InputSystem.actions, Is.SameAs(actions), "One input asset for gameplay, flow and project settings.");
            Assert.That(actions.actionMaps.Select(m => m.name), Is.EqualTo(new[] { "Player", "UI", "System" }),
                "Everything the game needs is declared in the asset, not added from code.");
        }

        [Test]
        public void EnablingDisablingAndDiscoveringDevicesLogsNothingAndKeepsTheShape()
        {
            var actions = LevelCatalog.Current.InputActions;
            var wereEnabled = actions.actionMaps.Where(m => m.enabled).ToList();
            var before = Shape(actions);
            var errors = new List<string>();
            var devices = new List<InputDevice>();
            Application.LogCallback collect = (message, stack, type) =>
            {
                if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception) errors.Add(message);
            };
            Application.logMessageReceived += collect;
            try
            {
                // Resolve bindings first: only an asset with live action state is re-resolved on discovery.
                actions.Enable();
                Discover(devices);
                Assert.That(Shape(actions), Is.EqualTo(before));
                foreach (var map in actions.actionMaps)
                {
                    map.Disable();
                    Discover(devices);
                    map.Enable();
                    Discover(devices);
                }
                actions.Disable();
                Discover(devices);
                Assert.That(Shape(actions), Is.EqualTo(before), "Enable and Disable never add or remove anything.");
                Assert.That(errors, Is.Empty, string.Join("\n", errors));
            }
            finally
            {
                foreach (var device in devices) InputSystem.RemoveDevice(device);
                actions.Disable();
                foreach (var map in wereEnabled) map.Enable();
                Application.logMessageReceived -= collect;
            }
        }

        private static void Discover(List<InputDevice> devices)
        {
            devices.Add(InputSystem.AddDevice<Keyboard>());
            devices.Add(InputSystem.AddDevice<Mouse>());
            devices.Add(InputSystem.AddDevice<Gamepad>());
        }

        private static string Shape(InputActionAsset asset) =>
            string.Join("; ", asset.actionMaps.Select(m => m.name + ": " + m.actions.Count + " actions, " + m.bindings.Count + " bindings"));
    }
}

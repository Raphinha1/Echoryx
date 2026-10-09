using Echoryx.Player;
using Echoryx.Rewind.Platforms;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Echoryx.Rewind.Tests
{
    public sealed class RewindInteractionWiringTests
    {
        [Test]
        public void PlayerPrefabUsesExistingInputActionsForRmb()
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");
            var rewind = actions.FindAction("Player/Rewind");
            Assert.That(rewind, Is.Not.Null);
            var rightMouse = false;
            foreach (var binding in rewind.bindings)
                rightMouse |= binding.path == "<Mouse>/rightButton";
            Assert.That(rightMouse, Is.True);
            Assert.That(actions.FindAction("Player/Look"), Is.Not.Null);

            var player = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Echoryx/Prefabs/Player.prefab");
            var interactor = player.GetComponent<RewindInteractor>();
            Assert.That(interactor, Is.Not.Null);
            Assert.That(new SerializedObject(interactor).FindProperty("actions").objectReferenceValue,
                Is.SameAs(actions));
        }

        [Test]
        public void PrototypeRetainsEarlierContentAndHasKinematicTestRotor()
        {
            var previous = SceneManager.GetActiveScene().path;
            try
            {
                EditorSceneManager.OpenScene("Assets/_Echoryx/Scenes/Prototype.unity", OpenSceneMode.Single);
                Assert.That(GameObject.Find("S01 Test Circuit"), Is.Not.Null);
                Assert.That(GameObject.Find("S02-C Echo Puzzle"), Is.Not.Null);
                var rotor = GameObject.Find("S03-B Rewind Test Rotor");
                Assert.That(rotor, Is.Not.Null);
                Assert.That(rotor.GetComponent<RewindTestRotor>(), Is.Not.Null);
                Assert.That(rotor.GetComponent<Collider>(), Is.Null);
                Assert.That(rotor.GetComponentInChildren<BoxCollider>(), Is.Not.Null);
                Assert.That(rotor.GetComponentsInChildren<Rigidbody>(), Is.Empty);
            }
            finally
            {
                if (!string.IsNullOrEmpty(previous))
                    EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }
        }

        [Test]
        public void PrototypeCrossingHasSolidSelectableSurfaceAndPlayerCarrier()
        {
            var previous = SceneManager.GetActiveScene().path;
            try
            {
                EditorSceneManager.OpenScene("Assets/_Echoryx/Scenes/Prototype.unity", OpenSceneMode.Single);
                var root = GameObject.Find("S03-C Rewind Crossing");
                Assert.That(root, Is.Not.Null);
                var player = GameObject.Find("Player");
                var carrier = player.GetComponent<PlayerPlatformCarrier>();
                Assert.That(carrier, Is.Not.Null);
                var platform = root.GetComponentInChildren<RewindMovingPlatform>();
                Assert.That(platform, Is.Not.Null);
                var surface = (BoxCollider)new SerializedObject(platform)
                    .FindProperty("surface").objectReferenceValue;
                Assert.That(surface, Is.Not.Null);
                Assert.That(surface.isTrigger, Is.False);
                Assert.That(surface.GetComponentInParent<RewindMovingPlatform>(), Is.SameAs(platform));
                Assert.That(root.GetComponentsInChildren<Rigidbody>(), Is.Empty);
                Assert.That(new SerializedObject(platform).FindProperty("passenger").objectReferenceValue,
                    Is.SameAs(carrier));

                var start = root.transform.Find("Platform Start");
                var end = root.transform.Find("Platform End");
                var departure = root.transform.Find("Departure Dock").GetComponent<BoxCollider>();
                var arrival = root.transform.Find("Arrival Dock").GetComponent<BoxCollider>();
                var approach = root.transform.Find("Approach Floor").GetComponent<BoxCollider>();
                var startFloor = GameObject.Find("Start Floor").GetComponent<BoxCollider>();
                var platformSettings = new SerializedObject(platform);
                var speed = platformSettings.FindProperty("speed").floatValue;
                var historySeconds = platformSettings.FindProperty("historySeconds").floatValue;
                Assert.That(MovingPlatformPath.TravelSeconds(start.position, end.position, speed),
                    Is.LessThan(historySeconds), "The whole trip must fit in Rewind history.");
                Assert.That(start.position.x - surface.bounds.extents.x,
                    Is.LessThan(departure.bounds.max.x), "The platform must meet the departure dock.");
                Assert.That(end.position.x + surface.bounds.extents.x,
                    Is.GreaterThan(arrival.bounds.min.x), "The platform must meet the arrival dock.");
                var motor = player.GetComponent<PlayerMotor>();
                var motorSettings = new SerializedObject(motor);
                var moveSpeed = motorSettings.FindProperty("moveSpeed").floatValue;
                var jumpHeight = motorSettings.FindProperty("jumpHeight").floatValue;
                var gravity = Mathf.Abs(motorSettings.FindProperty("gravity").floatValue);
                var coyoteTime = motorSettings.FindProperty("coyoteTime").floatValue;
                var jumpReach = moveSpeed *
                    (2f * Mathf.Sqrt(2f * jumpHeight / gravity) + coyoteTime)
                    + player.GetComponent<CharacterController>().radius + 0.2f;
                Assert.That(end.position.x - surface.bounds.extents.x - departure.bounds.max.x,
                    Is.GreaterThan(jumpReach),
                    "The stopped platform must be out of jump and coyote-time reach.");
                Assert.That(approach.bounds.Intersects(departure.bounds), Is.True);
                Assert.That(approach.bounds.Intersects(startFloor.bounds), Is.True);
                Assert.That(startFloor.bounds.min.z - departure.bounds.max.z,
                    Is.GreaterThan(moveSpeed * MovingPlatformPath.TravelSeconds(
                        start.position, end.position, speed)),
                    "The player must not reach the dock before the first trip ends.");

                Physics.SyncTransforms();
                var nearOrigin = departure.bounds.center + new Vector3(0f, 2.1f, -3f);
                var ray = new Ray(nearOrigin,
                    (surface.bounds.center - nearOrigin).normalized);
                Assert.That(Physics.Raycast(ray, out var hit, 12f, ~0,
                    QueryTriggerInteraction.Ignore), Is.True);
                Assert.That(hit.collider, Is.SameAs(surface),
                    "Departure geometry must leave a clear aiming line to the platform.");
                platform.transform.position = end.position;
                Physics.SyncTransforms();
                var farOrigin = departure.bounds.center + new Vector3(1f, 2.1f, -3f);
                var farRay = new Ray(farOrigin,
                    (surface.bounds.center - farOrigin).normalized);
                Assert.That(Physics.Raycast(farRay, out var farHit, 12f, ~0,
                    QueryTriggerInteraction.Ignore), Is.True);
                Assert.That(farHit.collider, Is.SameAs(surface),
                    "The parked platform must remain visible to the Rewind raycast.");
            }
            finally
            {
                if (!string.IsNullOrEmpty(previous))
                    EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }
        }
    }
}

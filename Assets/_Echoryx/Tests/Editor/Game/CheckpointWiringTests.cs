using Echoryx.Player;
using Echoryx.Rewind.Platforms;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Echoryx.Game.Tests
{
    public sealed class CheckpointWiringTests
    {
        [Test]
        public void RoomBRespawnHasSafeApproachLongerThanInitialPlatformTrip()
        {
            var previous = SceneManager.GetActiveScene().path;
            try
            {
                EditorSceneManager.OpenScene("Assets/_Echoryx/Scenes/Prototype.unity", OpenSceneMode.Single);
                Physics.SyncTransforms();
                var checkpoint = GameObject.Find("Checkpoint Room B").GetComponent<CheckpointTrigger>();
                var crossing = GameObject.Find("S03-C Rewind Crossing").transform;
                var approach = crossing.Find("Approach Floor").GetComponent<BoxCollider>();
                var platform = crossing.GetComponentInChildren<RewindMovingPlatform>();
                var settings = new SerializedObject(platform);
                var surface = (BoxCollider)settings.FindProperty("surface").objectReferenceValue;
                var player = GameObject.Find("Player");
                var capsule = player.GetComponent<CharacterController>();
                var speed = new SerializedObject(player.GetComponent<PlayerMotor>()).FindProperty("moveSpeed").floatValue;
                var trip = MovingPlatformPath.TravelSeconds(crossing.Find("Platform Start").position,
                    crossing.Find("Platform End").position, settings.FindProperty("speed").floatValue);
                var spawn = checkpoint.SpawnPoint.position;
                // Z alone is a lower bound, even cutting corners, jumping or moving diagonally.
                var earliestContact = (spawn.z - surface.bounds.max.z - capsule.radius - capsule.skinWidth) / speed;
                Assert.That(earliestContact, Is.GreaterThan(trip + 0.25d));
                Assert.That(spawn.x, Is.InRange(approach.bounds.min.x + capsule.radius, approach.bounds.max.x - capsule.radius));
                Assert.That(spawn.z, Is.InRange(approach.bounds.min.z + capsule.radius, approach.bounds.max.z - capsule.radius));
                Assert.That(spawn.y, Is.EqualTo(approach.bounds.max.y + 0.05f).Within(0.001f));
                Assert.That(Vector3.Distance(checkpoint.transform.position, spawn), Is.LessThan(0.1f));
                Assert.That(checkpoint.Room, Is.SameAs(platform.GetComponentInParent<RoomResetScope>()));
            }
            finally
            {
                if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }
        }

        [Test]
        public void PrototypeHasInitialSpawnAndSeparateRoomsRestartAndFallZone()
        {
            var previous = SceneManager.GetActiveScene().path;
            try
            {
                EditorSceneManager.OpenScene("Assets/_Echoryx/Scenes/Prototype.unity", OpenSceneMode.Single);
                var root = GameObject.Find("S04-A Checkpoints");
                Assert.That(root, Is.Not.Null);
                var manager = root.GetComponent<CheckpointManager>();
                var player = GameObject.Find("Player").GetComponent<PlayerMotor>();
                Assert.That(manager.Player, Is.SameAs(player));
                var managerData = new SerializedObject(manager);
                Assert.That(managerData.FindProperty("initialSpawn").objectReferenceValue, Is.Not.Null);
                Assert.That(managerData.FindProperty("initialRoom").objectReferenceValue, Is.Not.Null);
                var checkpoints = root.GetComponentsInChildren<CheckpointTrigger>();
                Assert.That(checkpoints.Length, Is.EqualTo(3));
                Assert.That(checkpoints[0].Room, Is.Not.SameAs(checkpoints[1].Room));
                foreach (var checkpoint in checkpoints)
                {
                    Assert.That(checkpoint.Manager, Is.SameAs(manager));
                    Assert.That(checkpoint.SpawnPoint, Is.Not.Null);
                    Assert.That(checkpoint.GetComponent<BoxCollider>().isTrigger, Is.True);
                    // The original S04-A rooms retain their probes; S04-C registers real mechanisms only.
                    if (checkpoint.Room.name == "Room A" || checkpoint.Room.name == "Room B")
                        Assert.That(checkpoint.Room.GetComponentInChildren<AttemptResetTransform>(), Is.Not.Null);
                }
                Assert.That(root.GetComponentInChildren<RespawnZone>(), Is.Not.Null);
                Assert.That(root.GetComponent<RestartInput>(), Is.Not.Null);
                Assert.That(root.GetComponent<RewindAttemptRelease>(), Is.Not.Null);
                var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
                var action = actions.FindAction("Player/Restart", true);
                var bound = false;
                foreach (var binding in action.bindings) bound |= binding.path == "<Keyboard>/r";
                Assert.That(bound, Is.True);
                Assert.That(GameObject.Find("S01 Test Circuit"), Is.Not.Null);
                Assert.That(GameObject.Find("S02-C Echo Puzzle"), Is.Not.Null);
                Assert.That(GameObject.Find("S03-C Rewind Crossing"), Is.Not.Null);
            }
            finally
            {
                if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }
        }
    }
}

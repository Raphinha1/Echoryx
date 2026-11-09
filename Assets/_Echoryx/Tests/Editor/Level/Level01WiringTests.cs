using System.Linq;
using Echoryx.Game;
using Echoryx.Interactions;
using Echoryx.Rewind.Platforms;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoryx.Level.Tests
{
    public sealed class Level01WiringTests
    {
        private const string ScenePath = "Assets/_Echoryx/Scenes/Level_01_Tutorial.unity";

        private static void WithScene(System.Action body)
        {
            var previous = SceneManager.GetActiveScene().path;
            try
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                body();
            }
            finally
            {
                if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }
        }

        private static SerializedProperty Prop(Object target, string name) => new SerializedObject(target).FindProperty(name);

        [Test]
        public void SceneIsInBuildSettings()
        {
            Assert.That(EditorBuildSettings.scenes.Any(s => s.path == ScenePath && s.enabled), Is.True);
        }

        [Test]
        public void ReusablePiecesExistAsPrefabs()
        {
            foreach (var name in new[] { "PressurePlate", "Door", "Hazard", "Goal", "TutorialHint" })
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Echoryx/Prefabs/Level/" + name + ".prefab"), Is.Not.Null, name);
        }

        [Test]
        public void CheckpointsRoomsAndHazardsAreWired()
        {
            WithScene(() =>
            {
                var manager = Object.FindFirstObjectByType<CheckpointManager>();
                Assert.That(manager, Is.Not.Null);
                var checkpoints = Object.FindObjectsByType<CheckpointTrigger>(FindObjectsSortMode.None);
                Assert.That(checkpoints.Length, Is.EqualTo(2));
                foreach (var checkpoint in checkpoints)
                {
                    Assert.That(checkpoint.Manager, Is.SameAs(manager));
                    Assert.That(checkpoint.Room, Is.Not.Null);
                    Assert.That(checkpoint.SpawnPoint, Is.Not.Null);
                }
                var zones = Object.FindObjectsByType<RespawnZone>(FindObjectsSortMode.None);
                Assert.That(zones.Length, Is.EqualTo(2));
                foreach (var zone in zones)
                    Assert.That(Prop(zone, "manager").objectReferenceValue, Is.SameAs(manager));
                Assert.That(Prop(manager, "echoReset").objectReferenceValue, Is.Not.Null);
                Assert.That(Prop(manager, "rewindRelease").objectReferenceValue, Is.Not.Null);
            });
        }

        [Test]
        public void EchoRoomAndRewindRoomMechanismsResetWithTheirRoom()
        {
            WithScene(() =>
            {
                var plate = Object.FindFirstObjectByType<PressurePlate>();
                var door = Object.FindFirstObjectByType<DoorController>();
                var platform = Object.FindFirstObjectByType<RewindMovingPlatform>();
                Assert.That(Prop(door, "plate").objectReferenceValue, Is.SameAs(plate));
                var echoRoom = plate.GetComponentInParent<RoomResetScope>();
                Assert.That(door.GetComponentInParent<RoomResetScope>(), Is.SameAs(echoRoom));
                var rewindRoom = platform.GetComponentInParent<RoomResetScope>();
                Assert.That(rewindRoom, Is.Not.SameAs(echoRoom));
                Assert.That(Participants(echoRoom), Does.Contain(plate.GetComponent<PressurePlateAttemptReset>()));
                Assert.That(Participants(echoRoom), Does.Contain(door.GetComponent<DoorAttemptReset>()));
                Assert.That(Participants(rewindRoom), Does.Contain(platform.GetComponent<RewindMechanismAttemptReset>()));
                foreach (var room in Object.FindObjectsByType<RoomResetScope>(FindObjectsSortMode.None))
                    Assert.That(Participants(room).Any(p => p is LevelStatsAttemptCounter), Is.True, room.name + " counts attempts");
            });
        }

        [Test]
        public void HintsGoalAndEndPanelAreWired()
        {
            WithScene(() =>
            {
                var hints = Object.FindObjectsByType<TutorialHint>(FindObjectsSortMode.None);
                Assert.That(hints.Length, Is.GreaterThanOrEqualTo(6));
                foreach (var hint in hints)
                {
                    Assert.That(hint.Message, Is.Not.Empty, hint.name);
                    Assert.That(Prop(hint, "display").objectReferenceValue, Is.Not.Null, hint.name);
                }
                Assert.That(hints.Any(h => h.Message == "¿Cómo puedes mantener la placa presionada y cruzar al mismo tiempo?"), Is.True);
                var stats = Object.FindFirstObjectByType<LevelStats>();
                Assert.That(Prop(Object.FindFirstObjectByType<GoalZone>(), "stats").objectReferenceValue, Is.SameAs(stats));
                var panel = Object.FindFirstObjectByType<LevelCompletePanel>();
                Assert.That(Prop(panel, "stats").objectReferenceValue, Is.SameAs(stats));
                Assert.That(Prop(panel, "panel").objectReferenceValue, Is.Not.Null);
                Assert.That(Prop(stats, "watchedMechanisms").arraySize, Is.EqualTo(1));
            });
        }

        private static Object[] Participants(RoomResetScope room)
        {
            var list = Prop(room, "participants");
            return Enumerable.Range(0, list.arraySize).Select(i => list.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        }
    }
}

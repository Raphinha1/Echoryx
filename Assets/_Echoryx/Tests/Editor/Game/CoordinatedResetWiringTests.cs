using Echoryx.Echo;
using Echoryx.Interactions;
using Echoryx.Rewind;
using Echoryx.Rewind.Platforms;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoryx.Game.Tests
{
    public sealed class CoordinatedResetWiringTests
    {
        [Test]
        public void PrototypeRegistersGlobalEchoAndSeparateRoomMechanismsWithoutDuplicates()
        {
            var previous = SceneManager.GetActiveScene().path;
            try
            {
                EditorSceneManager.OpenScene("Assets/_Echoryx/Scenes/Prototype.unity", OpenSceneMode.Single);
                var root = GameObject.Find("S04-A Checkpoints");
                var manager = root.GetComponent<CheckpointManager>();
                var roomA = root.transform.Find("Room A").GetComponent<RoomResetScope>();
                var roomB = root.transform.Find("Room B").GetComponent<RoomResetScope>();
                var echoReset = root.GetComponent<EchoAttemptReset>();
                Assert.That(echoReset, Is.Not.Null);
                Assert.That(root.GetComponents<EchoAttemptReset>().Length, Is.EqualTo(1));
                Assert.That(new SerializedObject(manager).FindProperty("echoReset").objectReferenceValue, Is.SameAs(echoReset));
                Assert.That(new SerializedObject(manager).FindProperty("initialRoom").objectReferenceValue, Is.SameAs(roomA));
                Assert.That(new SerializedObject(echoReset).FindProperty("playback").objectReferenceValue,
                    Is.SameAs(manager.Player.GetComponent<EchoPlayback>()));
                var puzzle = GameObject.Find("S02-C Echo Puzzle");
                var plate = puzzle.GetComponentInChildren<PressurePlate>();
                var door = puzzle.GetComponentInChildren<DoorController>();
                var rotor = GameObject.Find("S03-B Rewind Test Rotor").GetComponent<RewindTestRotor>();
                var platform = GameObject.Find("S03-C Rewind Crossing").GetComponentInChildren<RewindMovingPlatform>();
                CheckParticipant(roomA, plate.GetComponent<PressurePlateAttemptReset>());
                CheckParticipant(roomA, door.GetComponent<DoorAttemptReset>());
                CheckParticipant(roomA, rotor.GetComponent<RewindMechanismAttemptReset>());
                CheckParticipant(roomB, platform.GetComponent<RewindMechanismAttemptReset>());
                foreach (var room in new[] { roomA, roomB })
                {
                    var list = new SerializedObject(room).FindProperty("participants");
                    for (var i = 0; i < list.arraySize; i++)
                    {
                        var participant = list.GetArrayElementAtIndex(i).objectReferenceValue as MonoBehaviour;
                        Assert.That(participant, Is.Not.Null);
                        Assert.That(participant.GetComponentInParent<RoomResetScope>(), Is.SameAs(room));
                        Assert.That(participant, Is.Not.SameAs(echoReset));
                        for (var j = 0; j < i; j++)
                            Assert.That(list.GetArrayElementAtIndex(j).objectReferenceValue, Is.Not.SameAs(participant));
                    }
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }
        }

        private static void CheckParticipant(RoomResetScope room, MonoBehaviour participant)
        {
            Assert.That(participant, Is.Not.Null);
            var list = new SerializedObject(room).FindProperty("participants");
            var occurrences = 0;
            for (var i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == participant) occurrences++;
            Assert.That(occurrences, Is.EqualTo(1));
        }
    }
}

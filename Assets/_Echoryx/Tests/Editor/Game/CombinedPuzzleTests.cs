using System.Reflection;
using Echoryx.Echo;
using Echoryx.Interactions;
using Echoryx.Player;
using Echoryx.Rewind;
using Echoryx.Rewind.Platforms;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoryx.Game.Tests
{
    public sealed class CombinedPuzzleTests
    {
        private string previous;
        private Transform room;
        private CheckpointManager manager;
        private CheckpointTrigger checkpoint;
        private RewindMovingPlatform platform;

        [SetUp]
        public void SetUp()
        {
            previous = SceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene("Assets/_Echoryx/Scenes/Prototype.unity", OpenSceneMode.Single);
            room = GameObject.Find("S04-C Combined Puzzle").transform;
            manager = GameObject.Find("S04-A Checkpoints").GetComponent<CheckpointManager>();
            checkpoint = room.GetComponentInChildren<CheckpointTrigger>();
            platform = room.GetComponentInChildren<RewindMovingPlatform>();
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        }

        [Test]
        public void CombinedRoomHasOwnCheckpointMechanismsAndOrderedParticipants()
        {
            var scope = room.GetComponent<RoomResetScope>();
            Assert.That(checkpoint.Manager, Is.SameAs(manager));
            Assert.That(checkpoint.Room, Is.SameAs(scope));
            Assert.That(checkpoint.SpawnPoint, Is.SameAs(room.Find("Respawn Point")));
            var plate = room.GetComponentInChildren<PressurePlate>();
            var door = room.GetComponentInChildren<DoorController>();
            Assert.That(Reference(door, "plate"), Is.SameAs(plate));
            Assert.That(Reference(platform, "passenger"), Is.SameAs(manager.Player.GetComponent<PlayerPlatformCarrier>()));
            var participants = new SerializedObject(scope).FindProperty("participants");
            Assert.That(participants.arraySize, Is.EqualTo(3));
            Assert.That(participants.GetArrayElementAtIndex(0).objectReferenceValue, Is.TypeOf<PressurePlateAttemptReset>());
            Assert.That(participants.GetArrayElementAtIndex(1).objectReferenceValue, Is.TypeOf<DoorAttemptReset>());
            Assert.That(participants.GetArrayElementAtIndex(2).objectReferenceValue, Is.TypeOf<RewindMechanismAttemptReset>());
            for (var i = 0; i < participants.arraySize; i++)
                Assert.That(((MonoBehaviour)participants.GetArrayElementAtIndex(i).objectReferenceValue)
                    .GetComponentInParent<RoomResetScope>(), Is.SameAs(scope));
            Assert.That(Object.FindObjectsByType<EchoPlayback>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<EchoAttemptReset>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(room.GetComponentsInChildren<Rigidbody>(), Is.Empty);
            Assert.That(Reference(room.GetComponentInChildren<RespawnZone>(), "manager"), Is.SameAs(manager));
        }

        [Test]
        public void RepeatedRespawnCannotReachFirstDepartureEvenIgnoringClosedEchoDoor()
        {
            var speed = Float(manager.Player, "moveSpeed");
            var duration = MovingPlatformPath.TravelSeconds(room.Find("Platform Start").position,
                room.Find("Platform End").position, Float(platform, "speed"));
            var dock = Box("Safe Departure Floor");
            var distance = dock.bounds.max.z - checkpoint.SpawnPoint.position.z;
            Assert.That(distance / speed, Is.GreaterThan(duration + 1f),
                "Even instantaneous acceleration and no first barrier cannot catch the initial trip after R.");
            Assert.That(checkpoint.SpawnPoint.position.z, Is.LessThan(room.Find("Combined Echo Plate").position.z));
        }

        [Test]
        public void ParkedPlatformAndArrivalAreOutsideJumpAndCoyoteReach()
        {
            var reach = JumpReach();
            var dock = Box("Safe Departure Floor");
            var surface = (BoxCollider)Reference(platform, "surface");
            var end = room.Find("Platform End");
            Assert.That(end.position.z - surface.bounds.extents.z - dock.bounds.max.z, Is.GreaterThan(reach));
            Assert.That(Box("Arrival Floor").bounds.min.z - dock.bounds.max.z, Is.GreaterThan(reach));
            Assert.That(MovingPlatformPath.TravelSeconds(room.Find("Platform Start").position, end.position,
                Float(platform, "speed")), Is.LessThan(Float(platform, "historySeconds")));
        }

        [Test]
        public void LeavingPlateCannotReachThresholdBeforeDoorCloses()
        {
            var plate = room.GetComponentInChildren<PressurePlate>();
            var door = room.GetComponentInChildren<DoorController>();
            var detection = new SerializedObject(plate).FindProperty("detectionSize").vector3Value;
            var threshold = (Transform)Reference(door, "threshold");
            var thresholdSize = new SerializedObject(door).FindProperty("thresholdSize").vector3Value;
            var distance = threshold.position.z - thresholdSize.z * 0.5f - (plate.transform.position.z + detection.z * 0.5f);
            var closeSeconds = new SerializedObject(door).FindProperty("openOffset").vector3Value.y / Float(door, "moveSpeed");
            Assert.That(distance, Is.GreaterThan(Float(manager.Player, "moveSpeed") * closeSeconds
                + manager.Player.GetComponent<CharacterController>().radius + 0.5f));
            var fromDoorToSafeFloor = Box("Safe Departure Floor").bounds.center.z - threshold.position.z;
            Assert.That(closeSeconds + fromDoorToSafeFloor / Float(manager.Player, "moveSpeed") + 0.5f,
                Is.LessThan(EchoCapture.HardMaximumSeconds - 2f), "Comfortable Echo window, with a safe place after the door.");
        }

        [Test]
        public void WallsRoofAndDoorPreventJumpAroundBothBarriers()
        {
            var maxFeet = Float(manager.Player, "jumpHeight");
            foreach (var name in new[] { "Left Wall", "Right Wall", "Front Wall", "Back Wall" })
                Assert.That(Box(name).bounds.max.y, Is.GreaterThan(maxFeet + 2f));
            Assert.That(Box("Left Wall").bounds.min.z, Is.LessThan(room.Find("Combined Echo Plate").position.z));
            Assert.That(Box("Right Wall").bounds.min.z, Is.LessThan(checkpoint.SpawnPoint.position.z));
            Assert.That(Box("Left Wall").bounds.max.z, Is.GreaterThanOrEqualTo(Box("Arrival Floor").bounds.max.z));
            var panel = room.Find("Combined Echo Door/Door Panel").GetComponent<BoxCollider>();
            Assert.That(panel.isTrigger, Is.False);
            Assert.That(panel.bounds.max.y, Is.EqualTo(Box("Roof").bounds.min.y).Within(0.01f));
            Assert.That(panel.bounds.min.x, Is.EqualTo(Box("Left Wall").bounds.max.x).Within(0.01f));
            Assert.That(panel.bounds.max.x, Is.EqualTo(Box("Right Wall").bounds.min.x).Within(0.01f));
            Assert.That(panel.GetComponentInParent<RewindableMechanism>(), Is.Null, "Rewind alone cannot open the first barrier.");
            Assert.That(room.Find("Combined Exit Marker").GetComponent<Collider>(), Is.Null);
        }

        [Test]
        public void EntranceCheckpointVolumeCoversJumpAndSpawnIsOutsideDoorAndHazard()
        {
            var volume = checkpoint.GetComponent<BoxCollider>();
            var half = volume.size * 0.5f;
            Assert.That(half.x, Is.GreaterThan(Box("Entry Floor").bounds.extents.x));
            Assert.That(volume.center.y + half.y, Is.GreaterThan(Float(manager.Player, "jumpHeight") + 1f));
            Assert.That(checkpoint.SpawnPoint.localPosition.z, Is.LessThan(0f));
            Assert.That(room.Find("Combined Fall Zone").GetComponent<BoxCollider>().bounds.max.y, Is.LessThan(-4f));
            Assert.That(Box("Entrance Bridge").bounds.Intersects(GameObject.Find("Start Floor").GetComponent<BoxCollider>().bounds), Is.True);
            Assert.That(Box("Entrance Bridge").bounds.Intersects(Box("Entry Floor").bounds), Is.True);
        }

        [Test]
        public void RecoveredPlatformConnectsDocksAndIsSelectableAtTheAimMark()
        {
            var dock = Box("Safe Departure Floor");
            var arrival = Box("Arrival Floor");
            var surface = (BoxCollider)Reference(platform, "surface");
            Assert.That(surface.isTrigger, Is.False);
            Assert.That(surface.bounds.min.z, Is.LessThan(dock.bounds.max.z));
            Assert.That(room.Find("Platform End").position.z + surface.bounds.extents.z, Is.GreaterThan(arrival.bounds.min.z));
            var near = room.TransformPoint(new Vector3(0f, 2f, 12.7f));
            platform.transform.position = room.Find("Platform End").position;
            Physics.SyncTransforms();
            Assert.That(Physics.Raycast(near, (surface.bounds.center - near).normalized,
                out var hit, Float(manager.Player.GetComponent<RewindInteractor>(), "maximumDistance"), ~0,
                QueryTriggerInteraction.Ignore), Is.True);
            Assert.That(hit.collider, Is.SameAs(surface), "Walls, floor and signs must not occlude the Rewind target.");
        }

        [Test]
        public void CombinedResetRestoresOnlyItsMechanismsAndEchoAvailable()
        {
            CombinedTestAccess.Invoke(manager, "Awake");
            var other = GameObject.Find("S03-B Rewind Test Rotor").GetComponent<RewindTestRotor>();
            CombinedTestAccess.Invoke(other, "Awake");
            other.transform.rotation = Quaternion.Euler(0f, 110f, 0f);
            var otherHistory = (RewindHistory)CombinedTestAccess.Get(other, "history");
            otherHistory.AdvanceNormal(2d, new RewindPose(other.transform.position, other.transform.rotation));
            CombinedTestAccess.Invoke(platform, "Awake");
            var history = (RewindHistory)CombinedTestAccess.Get(platform, "history");
            platform.transform.position = room.Find("Platform End").position;
            history.AdvanceNormal(2d, new RewindPose(platform.transform.position, platform.transform.rotation));
            platform.BeginRewind();
            var door = room.GetComponentInChildren<DoorController>();
            CombinedTestAccess.Invoke(door, "Awake");
            ((Transform)Reference(door, "doorPanel")).localPosition += Vector3.up * 8f;
            var recorder = manager.Player.GetComponent<EchoRecorder>();
            CombinedTestAccess.Invoke(recorder, "Awake");
            recorder.Begin();
            Assert.That(manager.ActivateCheckpoint(checkpoint), Is.True);
            for (var i = 0; i < 3; i++) Assert.That(manager.RespawnNow(), Is.True);
            Assert.That(manager.ActiveRoom, Is.SameAs(room.GetComponent<RoomResetScope>()));
            Assert.That(recorder.State, Is.EqualTo(EchoState.Available));
            Assert.That(recorder.ReadyRecording, Is.Null);
            Assert.That(platform.Mode, Is.EqualTo(RewindMode.Normal));
            Assert.That(history.Time, Is.Zero);
            Assert.That(history.Count, Is.EqualTo(1));
            Assert.That(platform.transform.position, Is.EqualTo(room.Find("Platform Start").position));
            Assert.That(door.IsOpen, Is.False);
            Assert.That(room.GetComponentInChildren<PressurePlate>().IsPressed, Is.False);
            Assert.That(otherHistory.Time, Is.EqualTo(2d));
            Assert.That(Quaternion.Angle(other.transform.rotation, Quaternion.Euler(0f, 110f, 0f)), Is.LessThan(0.01f));
            Assert.That(manager.Player.transform.position, Is.EqualTo(checkpoint.SpawnPoint.position));
        }

        private float JumpReach() => Float(manager.Player, "moveSpeed") * (2f * Mathf.Sqrt(
            2f * Float(manager.Player, "jumpHeight") / Mathf.Abs(Float(manager.Player, "gravity")))
            + Float(manager.Player, "coyoteTime")) + manager.Player.GetComponent<CharacterController>().radius + 0.3f;
        private BoxCollider Box(string name) => room.Find(name).GetComponent<BoxCollider>();
        private static Object Reference(Object target, string field) => new SerializedObject(target).FindProperty(field).objectReferenceValue;
        private static float Float(Object target, string field) => new SerializedObject(target).FindProperty(field).floatValue;
    }

    internal static class CombinedTestAccess
    {
        public static object Get(object target, string field)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var info = type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (info != null) return info.GetValue(target);
            }
            throw new System.MissingFieldException(field);
        }
        public static void Invoke(object target, string name)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (method == null) continue;
                method.Invoke(target, null);
                return;
            }
            throw new System.MissingMethodException(name);
        }
    }
}

using System.Collections.Generic;
using System.Reflection;
using Echoryx.Player;
using Echoryx.Rewind;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Echoryx.Game.Tests
{
    public sealed class ResetOrderProbe : MonoBehaviour, IAttemptResetParticipant
    {
        public List<string> Events;
        public string Label;
        public Transform Player;
        public Vector3 PositionDuringReset;
        public void ReleaseForReset() => Events.Add("release " + Label);
        public void ResetForAttempt()
        {
            PositionDuringReset = Player.position;
            Events.Add("reset " + Label);
        }
    }

    public sealed class CheckpointTests
    {
        private GameObject root;
        private PlayerMotor motor;
        private CheckpointManager manager;
        private Transform initialSpawn;
        private RoomResetScope initialRoom;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Checkpoint Test");
            motor = Child(root.transform, "Player").AddComponent<PlayerMotor>();
            initialRoom = Child(root.transform, "Initial Room").AddComponent<RoomResetScope>();
            initialSpawn = Child(initialRoom.transform, "Initial Spawn").transform;
            initialSpawn.position = new Vector3(1f, 0.05f, 2f);
            manager = Child(root.transform, "Manager").AddComponent<CheckpointManager>();
            Set(manager, "player", motor);
            Set(manager, "initialSpawn", initialSpawn);
            Set(manager, "initialRoom", initialRoom);
            Invoke(manager, "Awake");
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void InitialRespawnUsesStableAuthoredPoint()
        {
            var expected = initialSpawn.position;
            initialSpawn.position += Vector3.right * 10f;
            motor.transform.position = Vector3.up * 12f;
            Assert.That(manager.RespawnNow(), Is.True);
            Assert.That(motor.transform.position, Is.EqualTo(expected));
            Assert.That(manager.ActiveRoom, Is.SameAs(initialRoom));
            Assert.That(manager.ActiveCheckpoint, Is.Null);
        }

        [Test]
        public void OnlyOneCheckpointActiveAndRepeatEntryDoesNotReplaceSnapshot()
        {
            var first = MakeCheckpoint("A", new Vector3(3f, 0f, 0f));
            var second = MakeCheckpoint("B", new Vector3(7f, 0f, 0f));
            Assert.That(manager.ActivateCheckpoint(first), Is.True);
            var firstSpawn = first.SpawnPoint.position;
            first.SpawnPoint.position += Vector3.right;
            Assert.That(manager.ActivateCheckpoint(first), Is.False);
            manager.RespawnNow();
            Assert.That(motor.transform.position, Is.EqualTo(firstSpawn));
            Assert.That(manager.ActivateCheckpoint(second), Is.True);
            Assert.That(first.IsActive, Is.False);
            Assert.That(second.IsActive, Is.True);
            Assert.That(manager.ActiveRoom, Is.SameAs(second.Room));
        }

        [Test]
        public void VolumeEntryActivatesPlayerAndExitAllowsLaterReactivation()
        {
            var first = MakeCheckpoint("A", new Vector3(3f, 0f, 0f));
            var second = MakeCheckpoint("B", new Vector3(7f, 0f, 0f));
            motor.transform.position = first.transform.position;
            first.CheckPlayerEntry();
            Assert.That(manager.ActiveCheckpoint, Is.SameAs(first));
            motor.transform.position = second.transform.position;
            first.CheckPlayerEntry();
            second.CheckPlayerEntry();
            Assert.That(manager.ActiveCheckpoint, Is.SameAs(second));
            motor.transform.position = first.transform.position;
            first.CheckPlayerEntry();
            Assert.That(manager.ActiveCheckpoint, Is.SameAs(first));
        }

        [Test]
        public void DestroyedCheckpointDoesNotLoseCapturedRespawnPose()
        {
            var checkpoint = MakeCheckpoint("A", new Vector3(3f, 0f, 0f));
            manager.ActivateCheckpoint(checkpoint);
            var pose = checkpoint.SpawnPoint.position;
            Object.DestroyImmediate(checkpoint.gameObject);
            Assert.DoesNotThrow(() => manager.RespawnNow());
            Assert.That(motor.transform.position, Is.EqualTo(pose));
        }

        [Test]
        public void RoomResetsInExplicitPhaseOrderBeforePlayerMovesAndCoalescesRequests()
        {
            var checkpoint = MakeCheckpoint("A", new Vector3(3f, 0f, 0f));
            manager.ActivateCheckpoint(checkpoint);
            var events = new List<string>();
            var a = MakeProbe(checkpoint.Room, "A", events);
            var b = MakeProbe(checkpoint.Room, "B", events);
            Set(checkpoint.Room, "participants", new MonoBehaviour[] { a, b, a });
            motor.transform.position = Vector3.up * 9f;
            manager.RequestRespawn();
            manager.RequestRespawn();
            Invoke(manager, "LateUpdate");
            Invoke(manager, "LateUpdate");
            CollectionAssert.AreEqual(new[] { "release A", "release B", "reset A", "reset B" }, events);
            Assert.That(a.PositionDuringReset, Is.EqualTo(Vector3.up * 9f));
            Assert.That(motor.transform.position, Is.EqualTo(checkpoint.SpawnPoint.position));
        }

        [Test]
        public void ActiveRoomDoesNotResetOtherRoomsOrAcceptForeignParticipants()
        {
            var first = MakeCheckpoint("A", Vector3.right * 3f);
            var second = MakeCheckpoint("B", Vector3.right * 7f);
            var events = new List<string>();
            var a = MakeProbe(first.Room, "A", events);
            var b = MakeProbe(second.Room, "B", events);
            Set(first.Room, "participants", new MonoBehaviour[] { a });
            Set(second.Room, "participants", new MonoBehaviour[] { b });
            manager.ActivateCheckpoint(first);
            manager.RespawnNow();
            CollectionAssert.AreEqual(new[] { "release A", "reset A" }, events);
            events.Clear();
            Set(first.Room, "participants", new MonoBehaviour[] { b });
            LogAssert.Expect(LogType.Error, "Reset participant must implement IAttemptResetParticipant and belong to this room.");
            first.Room.ResetMechanisms();
            Assert.That(events, Is.Empty);
        }

        [Test]
        public void HazardRequestsRespawnAndDisabledVolumeDoesNot()
        {
            var hazard = Child(root.transform, "Danger").AddComponent<RespawnZone>();
            Set(hazard, "manager", manager);
            var box = hazard.GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = Vector3.one * 2f;
            motor.transform.position = Vector3.zero;
            box.enabled = false;
            hazard.CheckPlayer();
            Invoke(manager, "LateUpdate");
            Assert.That(motor.transform.position, Is.EqualTo(Vector3.zero));
            box.enabled = true;
            hazard.CheckPlayer();
            Invoke(manager, "LateUpdate");
            Assert.That(motor.transform.position, Is.EqualTo(initialSpawn.position));
        }

        [Test]
        public void RespawnClearsVelocityJumpWindowAndPlatformDisplacement()
        {
            var carrier = motor.gameObject.AddComponent<PlayerPlatformCarrier>();
            Set(motor, "horizontalVelocity", Vector3.forward * 6f);
            Set(motor, "verticalVelocity", -30f);
            var jump = (JumpWindow)Get(motor, "jumpWindow");
            jump.SetGrounded(true, 1f);
            jump.Press(1f);
            carrier.OfferMotion(Vector3.right * 3f);
            manager.RespawnNow();
            Assert.That(Get(motor, "horizontalVelocity"), Is.EqualTo(Vector3.zero));
            Assert.That(Get(motor, "verticalVelocity"), Is.EqualTo(0f));
            Assert.That(jump.TryConsume(1f, 0.12f, 0.12f), Is.False);
            Assert.That(carrier.ConsumeMotion(), Is.EqualTo(Vector3.zero));
            carrier.OfferMotion(Vector3.right);
            Assert.That(carrier.ConsumeMotion(), Is.EqualTo(Vector3.zero));
            carrier.CompletePlayerMove();
            carrier.OfferMotion(Vector3.right);
            Assert.That(carrier.ConsumeMotion(), Is.EqualTo(Vector3.right));
            Assert.That(motor.GetComponent<CharacterController>().enabled, Is.True);
        }

        [Test]
        public void OptInTransformRestoresItsInitialPose()
        {
            var item = Child(initialRoom.transform, "Resettable");
            item.transform.localPosition = Vector3.forward * 2f;
            var participant = item.AddComponent<AttemptResetTransform>();
            Invoke(participant, "Awake");
            Set(initialRoom, "participants", new MonoBehaviour[] { participant, null });
            item.transform.localPosition = Vector3.right * 20f;
            manager.RespawnNow();
            Assert.That(item.transform.localPosition, Is.EqualTo(Vector3.forward * 2f));
        }

        [Test]
        public void RewindReleaseEndsActiveInteractionWithoutResettingHistory()
        {
            var interactor = motor.gameObject.AddComponent<RewindInteractor>();
            var session = (RewindSelectionSession)Get(interactor, "session");
            var target = new FakeRewind();
            session.Update(true, target);
            var release = manager.gameObject.AddComponent<RewindAttemptRelease>();
            Set(release, "interactor", interactor);
            Set(manager, "rewindRelease", release);
            manager.RespawnNow();
            Assert.That(target.Ends, Is.EqualTo(1));
            Assert.That(target.Resets, Is.Zero);
            Assert.That(session.IsHolding, Is.False);
        }

        private CheckpointTrigger MakeCheckpoint(string name, Vector3 position)
        {
            var room = Child(root.transform, "Room " + name).AddComponent<RoomResetScope>();
            var spawn = Child(room.transform, "Spawn").transform;
            spawn.position = position + Vector3.up * 0.05f;
            var trigger = Child(room.transform, name).AddComponent<CheckpointTrigger>();
            trigger.transform.position = position;
            trigger.GetComponent<BoxCollider>().size = Vector3.one * 2f;
            Set(trigger, "manager", manager);
            Set(trigger, "room", room);
            Set(trigger, "spawnPoint", spawn);
            return trigger;
        }

        private ResetOrderProbe MakeProbe(RoomResetScope room, string label, List<string> events)
        {
            var probe = Child(room.transform, label).AddComponent<ResetOrderProbe>();
            probe.Label = label;
            probe.Events = events;
            probe.Player = motor.transform;
            return probe;
        }

        private static GameObject Child(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static object Get(object target, string field) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void Invoke(object target, string method) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

        private sealed class FakeRewind : IRewindableMechanism
        {
            public int Ends;
            public int Resets;
            public RewindMode Mode { get; private set; }
            public bool IsAvailable => true;
            public bool CanRewind => true;
            public bool BeginRewind() { Mode = RewindMode.Rewinding; return true; }
            public bool EndRewind() { Ends++; Mode = RewindMode.Normal; return true; }
            public void ResetHistory() => Resets++;
        }
    }
}

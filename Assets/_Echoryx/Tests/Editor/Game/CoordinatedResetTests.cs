using System;
using System.Reflection;
using Echoryx.Echo;
using Echoryx.Interactions;
using Echoryx.Player;
using Echoryx.Rewind;
using Echoryx.Rewind.Platforms;
using NUnit.Framework;
using UnityEngine;

namespace Echoryx.Game.Tests
{
    public sealed class ResetMotionProbe : RewindableMechanism
    {
        public int AppliedPoses;
        public int Advances;
        protected override void ApplyPose(RewindPose pose) { AppliedPoses++; base.ApplyPose(pose); }
        protected override void AdvanceNormal(float deltaTime) { Advances++; }
    }

    public sealed class CoordinatedResetTests
    {
        private GameObject root;
        private PlayerMotor motor;
        private CheckpointManager manager;
        private RoomResetScope room;
        private EchoRecorder recorder;
        private EchoCapture capture;
        private EchoPlayback playback;
        private EchoAttemptReset echoReset;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Coordinated reset test");
            motor = Child(root.transform, "Player").AddComponent<PlayerMotor>();
            recorder = motor.gameObject.AddComponent<EchoRecorder>();
            capture = new EchoCapture();
            Set(recorder, "capture", capture);
            Set(recorder, "logTransitions", false);
            playback = motor.gameObject.AddComponent<EchoPlayback>();
            Invoke(playback, "Awake");
            var template = Child(root.transform, "Actor template").AddComponent<EchoActor>();
            var identity = template.gameObject.AddComponent<MechanismActor>();
            Set(identity, "kind", MechanismActorKind.Echo);
            Set(playback, "actorPrefab", template);
            room = Child(root.transform, "Room A").AddComponent<RoomResetScope>();
            var spawn = Child(root.transform, "Spawn").transform;
            spawn.position = Vector3.right * 20f;
            manager = Child(root.transform, "Manager").AddComponent<CheckpointManager>();
            echoReset = manager.gameObject.AddComponent<EchoAttemptReset>();
            Set(echoReset, "playback", playback);
            Set(manager, "echoReset", echoReset);
            Set(manager, "player", motor);
            Set(manager, "initialSpawn", spawn);
            Set(manager, "initialRoom", room);
            Invoke(manager, "Awake");
        }

        [TearDown]
        public void TearDown()
        {
            playback.ResetForAttempt();
            UnityEngine.Object.DestroyImmediate(root);
        }

        [TestCase(EchoState.Recording)]
        [TestCase(EchoState.Ready)]
        [TestCase(EchoState.Playing)]
        public void RequestImmediatelyReleasesEchoBeforeQueuedTeleport(EchoState state)
        {
            PrepareEcho(state);
            var actor = playback.ActiveActor;
            motor.transform.position = Vector3.forward * 4f;
            manager.RequestRespawn();
            Assert.That(motor.transform.position, Is.EqualTo(Vector3.forward * 4f));
            AssertEchoReleased(state);
            Assert.That(actor == null, Is.True, "EditMode destroys the actor immediately; PlayMode deactivates it immediately.");
            Invoke(recorder, "Update");
            Invoke(playback, "Update");
            AssertEchoReleased(state);
            Invoke(manager, "LateUpdate");
            Invoke(recorder, "Update");
            AssertEchoReleased(state);
            Assert.That(recorder.Begin(), Is.True);
        }

        [Test]
        public void HazardUsesImmediateEchoCancellationPath()
        {
            PrepareEcho(EchoState.Recording);
            var hazard = Child(root.transform, "Danger").AddComponent<RespawnZone>();
            Set(hazard, "manager", manager);
            hazard.GetComponent<BoxCollider>().size = Vector3.one * 4f;
            hazard.CheckPlayer();
            AssertEchoReleased(EchoState.Recording);
            Invoke(manager, "LateUpdate");
            Assert.That(motor.transform.position, Is.EqualTo(Vector3.right * 20f));
        }

        [Test]
        public void DirectAndRepeatedRespawnCleanCloneAndKeepRecording()
        {
            PrepareEcho(EchoState.Playing);
            Assert.That(manager.RespawnNow(), Is.True);
            Assert.That(manager.RespawnNow(), Is.True);
            echoReset.ReleaseForReset();
            echoReset.ResetForAttempt();
            AssertEchoReleased(EchoState.Playing);
            Assert.That(recorder.Begin(), Is.True);
        }

        [Test]
        public void PendingResetCannotSwitchToAnotherCheckpoint()
        {
            var checkpoint = Child(room.transform, "Checkpoint").AddComponent<CheckpointTrigger>();
            Set(checkpoint, "manager", manager);
            Set(checkpoint, "room", room);
            Set(checkpoint, "spawnPoint", checkpoint.transform);
            manager.RequestRespawn();
            Assert.That(manager.ActivateCheckpoint(checkpoint), Is.False);
            manager.RespawnNow();
            Assert.That(motor.transform.position, Is.EqualTo(Vector3.right * 20f));
        }

        [Test]
        public void RewindSessionReleasesImmediatelyBeforeScopedRestoration()
        {
            var rotor = MakeRotor(room);
            AdvanceRotor(rotor, 2d);
            var interactor = motor.gameObject.AddComponent<RewindInteractor>();
            var session = (RewindSelectionSession)Get(interactor, "session");
            session.Update(true, rotor);
            var release = manager.gameObject.AddComponent<RewindAttemptRelease>();
            Set(release, "interactor", interactor);
            Set(manager, "rewindRelease", release);
            Set(room, "participants", new MonoBehaviour[] { rotor.gameObject.AddComponent<RewindMechanismAttemptReset>() });
            manager.RequestRespawn();
            Assert.That(session.IsHolding, Is.False);
            Assert.That(rotor.Mode, Is.EqualTo(RewindMode.Normal));
            Assert.That(History(rotor).Time, Is.EqualTo(2d), "Release does not restore a room before its restoration phase.");
            Invoke(rotor, "TickTimeline");
            Assert.That(History(rotor).Time, Is.EqualTo(2d), "No sampling between release and restoration.");
            Assert.That(rotor.BeginRewind(), Is.False);
            manager.RespawnNow();
            Assert.That(History(rotor).Time, Is.Zero);
            Assert.That(rotor.CanRewind, Is.False);
            Assert.That(rotor.transform.rotation, Is.EqualTo(Quaternion.Euler(0f, 25f, 0f)));
        }

        [Test]
        public void MechanismResetDropsFutureAndStartsWithOneInitialPose()
        {
            var rotor = MakeRotor(room);
            AdvanceRotor(rotor, 2d);
            rotor.BeginRewind();
            var history = History(rotor);
            history.StepRewind(1d, out var recovered);
            Invoke(rotor, "ApplyPose", recovered);
            rotor.ResetForAttempt();
            Assert.That(history.Mode, Is.EqualTo(RewindMode.Normal));
            Assert.That(history.Count, Is.EqualTo(1));
            Assert.That(history.Time, Is.Zero);
            Assert.That(history.TryEvaluate(99d, out var pose), Is.True);
            Assert.That(pose.Rotation, Is.EqualTo(Quaternion.Euler(0f, 25f, 0f)));
            var other = MakeRotor(room);
            AdvanceRotor(other, 1d);
            Assert.That(other.BeginRewind(), Is.True, "Reset releases the global mechanism lock.");
        }

        [Test]
        public void PlatformRestoresStartAndProgressThenResumesFreshTrip()
        {
            var start = Child(room.transform, "Start").transform;
            var end = Child(room.transform, "End").transform;
            start.position = new Vector3(2f, 0f, 4f);
            end.position = new Vector3(9f, 0f, 4f);
            var platform = Child(room.transform, "Platform").AddComponent<RewindMovingPlatform>();
            Set(platform, "startPoint", start);
            Set(platform, "endPoint", end);
            Set(platform, "surface", platform.gameObject.AddComponent<BoxCollider>());
            var carrier = motor.gameObject.AddComponent<PlayerPlatformCarrier>();
            Set(platform, "passenger", carrier);
            Invoke(platform, "Awake");
            var history = History(platform);
            AdvancePlatform(platform, history, 2f);
            Assert.That(platform.transform.position, Is.EqualTo(end.position));
            platform.BeginRewind();
            Set(room, "participants", new MonoBehaviour[] { platform.gameObject.AddComponent<RewindMechanismAttemptReset>() });
            carrier.OfferMotion(Vector3.forward);
            manager.RespawnNow();
            Assert.That(platform.transform.position, Is.EqualTo(start.position));
            Assert.That(history.Count, Is.EqualTo(1));
            Assert.That(history.Time, Is.Zero);
            Assert.That(carrier.ConsumeMotion(), Is.EqualTo(Vector3.zero));
            Invoke(platform, "Update");
            Assert.That(platform.transform.position, Is.EqualTo(start.position), "No ticking later in the reset frame.");
            AdvancePlatform(platform, history, 0.5f);
            Assert.That(platform.transform.position, Is.EqualTo(start.position + Vector3.right * 1.75f));
            Assert.That(history.Time, Is.EqualTo(0.5d));
        }

        [Test]
        public void RestorationBypassesPassengerPoseApplicationAndSuppressesSameFrameTick()
        {
            var mechanism = Child(room.transform, "Probe").AddComponent<ResetMotionProbe>();
            Invoke(mechanism, "Awake");
            mechanism.transform.position = Vector3.one * 10f;
            mechanism.ResetForAttempt();
            Invoke(mechanism, "TickTimeline");
            Assert.That(mechanism.AppliedPoses, Is.Zero);
            Assert.That(mechanism.Advances, Is.Zero);
            Assert.That(mechanism.transform.position, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void PlateAndDoorClearOccupancyAndThresholdLatchBeforePlayerRelocation()
        {
            PrepareEcho(EchoState.Playing);
            var actor = playback.ActiveActor.GetComponent<MechanismActor>();
            var player = motor.gameObject.AddComponent<MechanismActor>();
            var plate = Child(room.transform, "Plate").AddComponent<PressurePlate>();
            var door = Child(room.transform, "Door").AddComponent<DoorController>();
            var panel = Child(door.transform, "Panel").transform;
            panel.localPosition = Vector3.one;
            Set(door, "doorPanel", panel);
            Set(door, "plate", plate);
            Set(door, "threshold", door.transform);
            Invoke(door, "Awake");
            panel.localPosition += Vector3.up * 4.2f;
            var gate = (DoorGateState)Get(door, "gate");
            gate.ShouldOpen(true, true, false);
            plate.RefreshFrom(new[] { player, actor });
            Assert.That(plate.PlayerPresent, Is.True);
            Assert.That(plate.EchoPresent, Is.True);
            Set(room, "participants", new MonoBehaviour[] {
                plate.gameObject.AddComponent<PressurePlateAttemptReset>(), door.gameObject.AddComponent<DoorAttemptReset>() });
            manager.RequestRespawn();
            Assert.That(plate.IsPressed, Is.False);
            plate.RefreshFrom(new[] { player, actor });
            Invoke(door, "LateUpdate");
            Assert.That(plate.IsPressed, Is.False, "No occupancy polling between reset phases.");
            Assert.That(panel.localPosition, Is.EqualTo(Vector3.one + Vector3.up * 4.2f), "Door movement is suspended before relocation.");
            manager.RespawnNow();
            plate.RefreshFrom(new[] { player, actor });
            Invoke(door, "LateUpdate");
            Assert.That(plate.IsPressed, Is.False);
            Assert.That(plate.PlayerPresent, Is.False);
            Assert.That(plate.EchoPresent, Is.False);
            Assert.That(panel.localPosition, Is.EqualTo(Vector3.one));
            Assert.That(gate.ShouldOpen(false, true, false), Is.False);
            Set(plate, "resetFrame", -1); // Simulate the next gameplay frame after the teleport.
            plate.RefreshFrom(new[] { player, actor });
            Assert.That(plate.IsPressed, Is.False);
        }

        [Test]
        public void RoomIsolationPreservesOtherMechanismPoseHistoryPlateAndDoor()
        {
            var otherRoom = Child(root.transform, "Room B").AddComponent<RoomResetScope>();
            var first = MakeRotor(room);
            var other = MakeRotor(otherRoom);
            AdvanceRotor(first, 2d);
            AdvanceRotor(other, 3d);
            var pose = other.transform.rotation;
            var plate = Child(otherRoom.transform, "Plate").AddComponent<PressurePlate>();
            var identity = Child(otherRoom.transform, "Actor").AddComponent<MechanismActor>();
            plate.RefreshFrom(new[] { identity });
            var door = Child(otherRoom.transform, "Door").AddComponent<DoorController>();
            var panel = Child(door.transform, "Panel").transform;
            Set(door, "doorPanel", panel);
            Invoke(door, "Awake");
            panel.localPosition = Vector3.up * 4.2f;
            Set(room, "participants", new MonoBehaviour[] { first.gameObject.AddComponent<RewindMechanismAttemptReset>() });
            Set(otherRoom, "participants", new MonoBehaviour[] {
                other.gameObject.AddComponent<RewindMechanismAttemptReset>(),
                plate.gameObject.AddComponent<PressurePlateAttemptReset>(), door.gameObject.AddComponent<DoorAttemptReset>() });
            manager.RespawnNow();
            Assert.That(History(first).Time, Is.Zero);
            Assert.That(History(other).Time, Is.EqualTo(3d));
            Assert.That(other.transform.rotation, Is.EqualTo(pose));
            Assert.That(plate.IsPressed, Is.True);
            Assert.That(door.IsOpen, Is.True);
        }

        private void PrepareEcho(EchoState state)
        {
            capture.Begin(0d, Vector3.zero, Quaternion.identity);
            capture.Advance(0.5d, Vector3.zero, Quaternion.identity);
            if (state == EchoState.Recording) return;
            capture.Stop(1d, Vector3.zero, Quaternion.identity);
            if (state == EchoState.Playing) Assert.That(playback.TryStart(), Is.True);
        }

        /// <summary>A reset removes the clone and aborts an unfinished capture, but keeps a finished recording.</summary>
        private void AssertEchoReleased(EchoState before)
        {
            Assert.That(playback.ActiveActor, Is.Null);
            if (before == EchoState.Recording)
            {
                Assert.That(recorder.State, Is.EqualTo(EchoState.Available));
                Assert.That(recorder.SampleCount, Is.Zero);
                Assert.That(recorder.ReadyRecording, Is.Null);
                return;
            }
            Assert.That(recorder.State, Is.EqualTo(EchoState.Ready), "The finished recording survives the respawn.");
            Assert.That(recorder.ReadyRecording, Is.Not.Null);
            Assert.That(recorder.ReadyRecording.SampleCount, Is.GreaterThan(0));
        }

        private RewindTestRotor MakeRotor(RoomResetScope scope)
        {
            var owner = Child(scope.transform, "Rotor");
            owner.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
            var rotor = owner.AddComponent<RewindTestRotor>();
            Invoke(rotor, "Awake");
            return rotor;
        }

        private static void AdvanceRotor(RewindTestRotor rotor, double seconds)
        {
            rotor.transform.rotation = Quaternion.Euler(0f, 150f, 0f);
            History(rotor).AdvanceNormal(seconds, new RewindPose(rotor.transform.position, rotor.transform.rotation));
        }

        private static void AdvancePlatform(RewindMovingPlatform platform, RewindHistory history, float seconds)
        {
            Invoke(platform, "AdvanceNormal", seconds);
            history.AdvanceNormal(seconds, new RewindPose(platform.transform.position, platform.transform.rotation));
        }

        private static RewindHistory History(RewindableMechanism mechanism) => (RewindHistory)Get(mechanism, "history");
        private static GameObject Child(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static FieldInfo Field(object target, string field)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var info = type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (info != null) return info;
            }
            throw new MissingFieldException(field);
        }
        private static void Set(object target, string field, object value) => Field(target, field).SetValue(target, value);
        private static object Get(object target, string field) => Field(target, field).GetValue(target);
        private static void Invoke(object target, string name, params object[] args)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (method == null) continue;
                method.Invoke(target, args);
                return;
            }
            throw new MissingMethodException(name);
        }
    }
}

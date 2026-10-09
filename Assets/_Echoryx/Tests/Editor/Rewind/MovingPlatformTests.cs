using Echoryx.Player;
using Echoryx.Rewind.Platforms;
using NUnit.Framework;
using UnityEngine;

namespace Echoryx.Rewind.Tests
{
    public sealed class MovingPlatformTests
    {
        private static readonly Vector3 Start = new Vector3(8.5f, 0.05f, 0f);
        private static readonly Vector3 End = new Vector3(15.5f, 0.05f, 0f);

        [Test]
        public void PathArrivesAndStaysAtDestination()
        {
            Assert.That(MovingPlatformPath.TravelSeconds(Start, End, 3.5f), Is.EqualTo(2d).Within(1e-6));
            Assert.That(MovingPlatformPath.Evaluate(Start, End, 3.5f, 0d).Position,
                Is.EqualTo(Start));
            Assert.That(MovingPlatformPath.Evaluate(Start, End, 3.5f, 1d).Position.x,
                Is.EqualTo(12f).Within(1e-5));
            var far = MovingPlatformPath.Evaluate(Start, End, 3.5f, 2d);
            Assert.That(far.Position, Is.EqualTo(End));
            Assert.That(far.Direction, Is.EqualTo(0));
            Assert.That(MovingPlatformPath.HasRemainingTravel(Start, End, 3.5f, 2d), Is.False);
            Assert.That(MovingPlatformPath.Evaluate(Start, End, 3.5f, 600d).Position,
                Is.EqualTo(End));
        }

        [Test]
        public void RecoveredTimelineResumesOnSameBranchWithoutJump()
        {
            var history = new RewindHistory();
            history.Reset(new RewindPose(Start, Quaternion.identity));
            for (var frame = 1; frame <= 120; frame++)
            {
                var nextTime = history.Time + 1d / 60d;
                var next = MovingPlatformPath.Evaluate(Start, End, 3.5f, nextTime);
                history.AdvanceNormal(1d / 60d, new RewindPose(next.Position, Quaternion.identity));
            }

            Assert.That(history.BeginRewind(new RewindPose(
                MovingPlatformPath.Evaluate(Start, End, 3.5f, history.Time).Position,
                Quaternion.identity)), Is.True);
            history.StepRewind(1.5d, out var recovered);
            Assert.That(recovered.Position.x, Is.EqualTo(10.25f).Within(0.01f));
            Assert.That(history.EndRewind(out var committed), Is.True);
            Assert.That(committed.Position.x, Is.EqualTo(recovered.Position.x).Within(1e-5));

            var nextPose = MovingPlatformPath.Evaluate(Start, End, 3.5f, history.Time + 0.1d);
            Assert.That(Vector3.Distance(nextPose.Position, committed.Position),
                Is.EqualTo(0.35f).Within(0.01f));
            Assert.That(nextPose.Position.x, Is.GreaterThan(committed.Position.x));
            history.AdvanceNormal(0.1d, new RewindPose(nextPose.Position, Quaternion.identity));
            Assert.That(history.NewestTime, Is.LessThan(2d),
                "The discarded future must not return after normal motion resumes.");
        }

        [Test]
        public void LongWaitAtDestinationKeepsStartInHistory()
        {
            var history = new RewindHistory();
            history.Reset(new RewindPose(Start, Quaternion.identity));
            for (var frame = 0; frame < 36000; frame++) // Ten minutes of rendered frames.
            {
                if (!MovingPlatformPath.HasRemainingTravel(Start, End, 3.5f, history.Time))
                    continue; // The mechanism does not age its history while parked.
                var next = MovingPlatformPath.Evaluate(Start, End, 3.5f,
                    history.Time + 1d / 60d);
                history.AdvanceNormal(1d / 60d,
                    new RewindPose(next.Position, Quaternion.identity));
            }

            Assert.That(history.Time, Is.EqualTo(2d).Within(0.02d));
            Assert.That(history.OldestTime, Is.EqualTo(0d).Within(1e-6d));
            Assert.That(history.BeginRewind(new RewindPose(End, Quaternion.identity)), Is.True);
            Assert.That(history.StepRewind(5d, out var returned), Is.True);
            Assert.That(returned.Position, Is.EqualTo(Start));
            Assert.That(history.EndRewind(out _), Is.True);
            Assert.That(MovingPlatformPath.HasRemainingTravel(Start, End, 3.5f, history.Time), Is.True);
        }

        [Test]
        public void DegeneratePathStaysAtStart()
        {
            Assert.That(MovingPlatformPath.Evaluate(Start, Start, 2f, 10d).Position,
                Is.EqualTo(Start));
            Assert.That(MovingPlatformPath.Evaluate(Start, End, 0f, 10d).Position,
                Is.EqualTo(Start));
        }

        [Test]
        public void RidePolicyAcceptsFeetOnTopAndRejectsSideOrAir()
        {
            var bounds = new Bounds(new Vector3(10f, 0.05f, 0f),
                new Vector3(2.4f, 0.4f, 2.5f));
            var top = bounds.max.y;
            Assert.That(PlatformRidePolicy.IsSupported(
                new Vector3(10f, top + 0.05f, 0f), bounds, 0.2f, 0.1f), Is.True);
            Assert.That(PlatformRidePolicy.IsSupported(
                new Vector3(10f, top + 1f, 0f), bounds, 0.2f, 0.1f), Is.False);
            Assert.That(PlatformRidePolicy.IsSupported(
                new Vector3(12f, top, 0f), bounds, 0.2f, 0.1f), Is.False);
        }

        [Test]
        public void CarrierConsumesAtMostOnePlatformDeltaPerFrame()
        {
            var player = new GameObject("Carrier Test Player");
            try
            {
                var carrier = player.AddComponent<PlayerPlatformCarrier>();
                carrier.OfferMotion(new Vector3(0.2f, 0f, 0f));
                carrier.OfferMotion(new Vector3(5f, 0f, 0f));
                Assert.That(carrier.ConsumeMotion().x, Is.EqualTo(0.2f));
                Assert.That(carrier.ConsumeMotion(), Is.EqualTo(Vector3.zero));
                carrier.OfferMotion(Vector3.right);
                carrier.enabled = false;
                Assert.That(carrier.ConsumeMotion(), Is.EqualTo(Vector3.zero));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }
    }
}

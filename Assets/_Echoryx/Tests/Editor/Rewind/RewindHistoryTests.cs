using System;
using NUnit.Framework;
using UnityEngine;

namespace Echoryx.Rewind.Tests
{
    public sealed class RewindHistoryTests
    {
        private static RewindPose Pose(float x, float yaw = 0f) =>
            new RewindPose(new Vector3(x, 0f, 0f), Quaternion.Euler(0f, yaw, 0f));

        [Test]
        public void EmptyAndSingleSampleHistoryCannotRewind()
        {
            var history = new RewindHistory();
            Assert.That(history.Count, Is.Zero);
            Assert.That(history.TryEvaluate(0d, out _), Is.False);
            Assert.That(history.BeginRewind(Pose(0f)), Is.False);
            Assert.That(history.StepRewind(1d, out _), Is.False);
            Assert.That(history.EndRewind(out _), Is.False);

            history.Reset(Pose(0f));
            Assert.That(history.BeginRewind(Pose(0f)), Is.False);
            Assert.That(history.Mode, Is.EqualTo(RewindMode.Normal));
            Assert.That(history.Count, Is.EqualTo(1));
        }

        [Test]
        public void SamplingStaysWithinFiveSecondWindowAndFixedCapacity()
        {
            var history = new RewindHistory(5f, 20);
            history.Reset(Pose(0f));
            for (var frame = 1; frame <= 600; frame++)
                history.AdvanceNormal(1d / 60d, Pose(frame / 60f));

            Assert.That(history.Time, Is.EqualTo(10d).Within(1e-6));
            Assert.That(history.Count, Is.LessThanOrEqualTo(history.Capacity));
            Assert.That(history.NewestTime - history.OldestTime, Is.LessThanOrEqualTo(5d + 1e-6));
            Assert.That(history.OldestTime, Is.EqualTo(5d).Within(0.051d));
            Assert.That(history.BeginRewind(Pose(10f)), Is.True);
            Assert.That(history.Count, Is.LessThanOrEqualTo(history.Capacity));
        }

        [Test]
        public void IrregularFramesInterpolatePositionAndRotationByTime()
        {
            var history = new RewindHistory(5f, 10);
            history.Reset(Pose(0f));
            history.AdvanceNormal(0.07d, Pose(0.7f, 21f));
            history.AdvanceNormal(0.18d, Pose(2.5f, 75f));

            Assert.That(history.TryEvaluate(0.15d, out var pose), Is.True);
            Assert.That(pose.Position.x, Is.EqualTo(1.5f).Within(1e-4));
            Assert.That(Quaternion.Angle(pose.Rotation, Quaternion.Euler(0f, 45f, 0f)),
                Is.LessThan(0.1f));
            Assert.That(history.TryEvaluate(-1d, out var before), Is.True);
            Assert.That(before.Position.x, Is.Zero);
            Assert.That(history.TryEvaluate(100d, out var after), Is.True);
            Assert.That(after.Position.x, Is.EqualTo(2f).Within(1e-4),
                "The newest regular sample is at 0.2 seconds before Rewind starts.");
        }

        [Test]
        public void RewindSuspendsNormalSamplingAndUsesElapsedTime()
        {
            var history = new RewindHistory(5f, 20);
            history.Reset(Pose(0f));
            history.AdvanceNormal(2d, Pose(2f));
            Assert.That(history.BeginRewind(Pose(2f)), Is.True);
            Assert.That(history.BeginRewind(Pose(2f)), Is.False);
            var count = history.Count;
            Assert.That(history.AdvanceNormal(1d, Pose(100f)), Is.False);
            Assert.That(history.Count, Is.EqualTo(count));

            Assert.That(history.StepRewind(0.3d, out var first), Is.True);
            Assert.That(first.Position.x, Is.EqualTo(1.7f).Within(1e-4));
            Assert.That(history.StepRewind(0.4d, out var second), Is.True);
            Assert.That(second.Position.x, Is.EqualTo(1.3f).Within(1e-4));
            Assert.That(history.Mode, Is.EqualTo(RewindMode.Rewinding));
        }

        [Test]
        public void ReleaseKeepsRecoveredPoseAndDiscardsFutureBranch()
        {
            var history = new RewindHistory(5f, 20);
            history.Reset(Pose(0f));
            history.AdvanceNormal(2d, Pose(2f));
            history.BeginRewind(Pose(2f));
            history.StepRewind(0.75d, out var recovered);
            Assert.That(recovered.Position.x, Is.EqualTo(1.25f).Within(1e-4));

            Assert.That(history.EndRewind(out var committed), Is.True);
            Assert.That(committed.Position.x, Is.EqualTo(1.25f).Within(1e-4));
            Assert.That(history.Mode, Is.EqualTo(RewindMode.Normal));
            Assert.That(history.Time, Is.EqualTo(1.25d).Within(1e-6));
            Assert.That(history.NewestTime, Is.EqualTo(1.25d).Within(1e-6));
            Assert.That(history.EndRewind(out _), Is.False);

            // A new direction starts at the recovered state, with no old x=2 branch.
            history.AdvanceNormal(0.5d, Pose(0.75f));
            Assert.That(history.TryEvaluate(1.5d, out var branched), Is.True);
            Assert.That(branched.Position.x, Is.EqualTo(1f).Within(1e-4));
            Assert.That(history.BeginRewind(Pose(0.75f)), Is.True);
            history.StepRewind(0.25d, out var rewoundNewBranch);
            Assert.That(rewoundNewBranch.Position.x, Is.EqualTo(1f).Within(1e-4));
        }

        [Test]
        public void RewindClampsToOldestAvailablePose()
        {
            var history = new RewindHistory(5f, 20);
            history.Reset(Pose(0f));
            history.AdvanceNormal(10d, Pose(10f));
            history.BeginRewind(Pose(10f));

            Assert.That(history.StepRewind(100d, out var first), Is.True);
            Assert.That(history.StepRewind(1d, out var second), Is.True);
            Assert.That(second.Position, Is.EqualTo(first.Position));
            Assert.That(history.EndRewind(out var committed), Is.True);
            Assert.That(committed.Position, Is.EqualTo(first.Position));
        }

        [Test]
        public void ResetDuringRewindClearsOldTimeline()
        {
            var history = new RewindHistory();
            history.Reset(Pose(0f));
            history.AdvanceNormal(1d, Pose(1f));
            history.BeginRewind(Pose(1f));
            history.Reset(Pose(9f));

            Assert.That(history.Mode, Is.EqualTo(RewindMode.Normal));
            Assert.That(history.Count, Is.EqualTo(1));
            Assert.That(history.Time, Is.Zero);
            Assert.That(history.EndRewind(out _), Is.False);
            Assert.That(history.TryEvaluate(10d, out var pose), Is.True);
            Assert.That(pose.Position.x, Is.EqualTo(9f));
        }

        [Test]
        public void LongFrameAndInvalidDeltasRemainBounded()
        {
            var history = new RewindHistory();
            history.Reset(Pose(0f));
            history.AdvanceNormal(10000d, Pose(10000f));
            Assert.That(history.Count, Is.LessThanOrEqualTo(history.Capacity));
            Assert.That(history.NewestTime - history.OldestTime, Is.LessThanOrEqualTo(5d + 1e-6));
            var time = history.Time;
            history.AdvanceNormal(-1d, Pose(10000f));
            history.AdvanceNormal(double.NaN, Pose(10000f));
            Assert.That(history.Time, Is.EqualTo(time));
        }

        [Test]
        public void ConfigurationRejectsInvalidDurationAndRate()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RewindHistory(0f, 20));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RewindHistory(5.1f, 20));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RewindHistory(5f, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RewindHistory(5f, 121));
        }
    }
}

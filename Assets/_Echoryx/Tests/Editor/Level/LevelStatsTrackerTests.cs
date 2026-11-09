using NUnit.Framework;

namespace Echoryx.Level.Tests
{
    public sealed class LevelStatsTrackerTests
    {
        [Test]
        public void CountsAttemptsAndRewindsAndMeasuresElapsedTime()
        {
            var tracker = new LevelStatsTracker();
            tracker.Start(10d);
            tracker.RegisterAttempt();
            tracker.RegisterAttempt();
            tracker.RegisterRewind();
            Assert.That(tracker.Attempts, Is.EqualTo(2));
            Assert.That(tracker.RewindUses, Is.EqualTo(1));
            Assert.That(tracker.ElapsedAt(14.5d), Is.EqualTo(4.5d).Within(1e-9));
        }

        [Test]
        public void CompletionFreezesCountersAndTime()
        {
            var tracker = new LevelStatsTracker();
            tracker.Start(0d);
            tracker.RegisterAttempt();
            Assert.That(tracker.Complete(20d), Is.True);
            tracker.RegisterAttempt();
            tracker.RegisterRewind();
            Assert.That(tracker.Complete(30d), Is.False, "Completion happens once.");
            Assert.That(tracker.Attempts, Is.EqualTo(1));
            Assert.That(tracker.RewindUses, Is.EqualTo(0));
            Assert.That(tracker.ElapsedAt(99d), Is.EqualTo(20d).Within(1e-9));
        }

        [Test]
        public void ElapsedIsZeroBeforeStartAndTimeIsFormattedAsMinutesSeconds()
        {
            Assert.That(new LevelStatsTracker().ElapsedAt(50d), Is.EqualTo(0d));
            Assert.That(LevelStatsTracker.FormatTime(0d), Is.EqualTo("0:00"));
            Assert.That(LevelStatsTracker.FormatTime(65.9d), Is.EqualTo("1:05"));
            Assert.That(LevelStatsTracker.FormatTime(-3d), Is.EqualTo("0:00"));
        }
    }
}

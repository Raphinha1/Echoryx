using System;

namespace Echoryx.Level
{
    /// <summary>Plain counters for one level run; no persistence.</summary>
    public sealed class LevelStatsTracker
    {
        private double startTime;
        private double endTime;
        private bool started;

        public int Attempts { get; private set; }
        public int RewindUses { get; private set; }
        public bool Completed { get; private set; }

        public void Start(double now)
        {
            startTime = now;
            started = true;
        }

        public double ElapsedAt(double now)
        {
            if (!started) return 0d;
            return Math.Max(0d, (Completed ? endTime : now) - startTime);
        }

        /// <summary>A death or manual restart. Ignored once the level is complete.</summary>
        public void RegisterAttempt()
        {
            if (!Completed) Attempts++;
        }

        public void RegisterRewind()
        {
            if (!Completed) RewindUses++;
        }

        /// <summary>Freezes all counters. Returns false when already complete.</summary>
        public bool Complete(double now)
        {
            if (Completed) return false;
            endTime = now;
            Completed = true;
            return true;
        }

        public static string FormatTime(double seconds)
        {
            var total = (int)Math.Floor(Math.Max(0d, seconds));
            return (total / 60) + ":" + (total % 60).ToString("00");
        }
    }
}

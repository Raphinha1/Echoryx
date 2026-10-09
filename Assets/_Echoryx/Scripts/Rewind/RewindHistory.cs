using System;

namespace Echoryx.Rewind
{
    /// <summary>Fixed-capacity, time-sampled pose history and reversible timeline.</summary>
    public sealed class RewindHistory
    {
        public const float HardMaximumSeconds = 5f;

        private readonly struct Sample
        {
            public readonly double Time;
            public readonly RewindPose Pose;

            public Sample(double time, RewindPose pose)
            {
                Time = time;
                Pose = pose;
            }
        }

        private readonly Sample[] samples;
        private readonly double duration;
        private readonly double interval;
        private int head;
        private int count;
        private bool initialized;
        private double time;
        private double nextSampleTime;
        private double cursorTime;
        private RewindPose previousPose;

        public RewindHistory(float durationSeconds = HardMaximumSeconds, int samplesPerSecond = 20)
        {
            if (durationSeconds <= 0f || durationSeconds > HardMaximumSeconds)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            if (samplesPerSecond <= 0 || samplesPerSecond > 120)
                throw new ArgumentOutOfRangeException(nameof(samplesPerSecond));

            duration = durationSeconds;
            interval = 1d / samplesPerSecond;
            // Regular samples plus one exact pose at the instant rewind starts or ends.
            samples = new Sample[(int)Math.Ceiling(durationSeconds * samplesPerSecond) + 2];
        }

        public RewindMode Mode { get; private set; } = RewindMode.Normal;
        public bool CanRewind => initialized && Mode == RewindMode.Normal
            && time - OldestTime > 1e-9d;
        public int Count => count;
        public int Capacity => samples.Length;
        public double Time => time;
        public double OldestTime => count > 0 ? At(0).Time : 0d;
        public double NewestTime => count > 0 ? At(count - 1).Time : 0d;

        public void Reset(RewindPose initialPose)
        {
            head = 0;
            count = 0;
            time = cursorTime = 0d;
            nextSampleTime = interval;
            previousPose = initialPose;
            initialized = true;
            Mode = RewindMode.Normal;
            Append(0d, initialPose);
        }

        /// <summary>Called after normal motion advances by elapsed real seconds.</summary>
        public bool AdvanceNormal(double elapsedSeconds, RewindPose currentPose)
        {
            if (Mode != RewindMode.Normal) return false;
            if (!initialized)
            {
                Reset(currentPose);
                return true;
            }

            var delta = ValidDelta(elapsedSeconds);
            if (delta <= 0d) return true;
            var end = time + delta;
            var cutoff = end - duration;

            // A long stalled frame cannot create an unbounded catch-up loop.
            if (nextSampleTime < cutoff)
                nextSampleTime += Math.Ceiling((cutoff - nextSampleTime) / interval) * interval;

            while (nextSampleTime <= end + 1e-9d)
            {
                var fraction = (float)Math.Max(0d, Math.Min(1d,
                    (nextSampleTime - time) / delta));
                Append(nextSampleTime, RewindPose.Interpolate(previousPose, currentPose, fraction));
                nextSampleTime += interval;
            }

            time = end;
            previousPose = currentPose;
            PruneBefore(cutoff);
            if (count == 1 && OldestTime < cutoff)
            {
                // Durations shorter than one sampling interval still obey the window.
                head = count = 0;
                Append(time, currentPose);
                nextSampleTime = time + interval;
            }
            return true;
        }

        public bool BeginRewind(RewindPose currentPose)
        {
            if (!initialized || Mode != RewindMode.Normal) return false;
            if (time > NewestTime + 1e-9d) Append(time, currentPose);
            else ReplaceNewest(time, currentPose);
            PruneBefore(time - duration);
            if (count < 2 || NewestTime - OldestTime <= 1e-9d) return false;

            cursorTime = time;
            Mode = RewindMode.Rewinding;
            return true;
        }

        public bool StepRewind(double elapsedSeconds, out RewindPose pose)
        {
            pose = default;
            if (Mode != RewindMode.Rewinding) return false;
            cursorTime = Math.Max(OldestTime, cursorTime - ValidDelta(elapsedSeconds));
            return TryEvaluate(cursorTime, out pose);
        }

        /// <summary>Commits the recovered pose and removes all later samples.</summary>
        public bool EndRewind(out RewindPose pose)
        {
            pose = default;
            if (Mode != RewindMode.Rewinding || !TryEvaluate(cursorTime, out pose))
                return false;

            while (count > 0 && NewestTime > cursorTime + 1e-9d) count--;
            if (count == 0 || NewestTime < cursorTime - 1e-9d)
                Append(cursorTime, pose);
            else
                ReplaceNewest(cursorTime, pose);

            time = cursorTime;
            previousPose = pose;
            nextSampleTime = time + interval;
            Mode = RewindMode.Normal;
            return true;
        }

        public bool TryEvaluate(double queryTime, out RewindPose pose)
        {
            pose = default;
            if (count == 0) return false;
            if (count == 1 || queryTime <= OldestTime)
            {
                pose = At(0).Pose;
                return true;
            }
            if (queryTime >= NewestTime)
            {
                pose = At(count - 1).Pose;
                return true;
            }

            var low = 0;
            var high = count - 1;
            while (high - low > 1)
            {
                var middle = (low + high) / 2;
                if (At(middle).Time <= queryTime) low = middle;
                else high = middle;
            }

            var from = At(low);
            var to = At(high);
            var fraction = (float)((queryTime - from.Time) / (to.Time - from.Time));
            pose = RewindPose.Interpolate(from.Pose, to.Pose, fraction);
            return true;
        }

        private static double ValidDelta(double delta)
        {
            return double.IsNaN(delta) || double.IsInfinity(delta) || delta < 0d ? 0d : delta;
        }

        private Sample At(int index) => samples[(head + index) % samples.Length];

        private void Append(double sampleTime, RewindPose pose)
        {
            if (count == samples.Length)
            {
                head = (head + 1) % samples.Length;
                count--;
            }
            samples[(head + count) % samples.Length] = new Sample(sampleTime, pose);
            count++;
        }

        private void ReplaceNewest(double sampleTime, RewindPose pose)
        {
            samples[(head + count - 1) % samples.Length] = new Sample(sampleTime, pose);
        }

        private void PruneBefore(double cutoff)
        {
            while (count > 1 && OldestTime < cutoff - 1e-9d)
            {
                head = (head + 1) % samples.Length;
                count--;
            }
        }
    }
}

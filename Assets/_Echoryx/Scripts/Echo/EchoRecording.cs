using UnityEngine;

namespace Echoryx.Echo
{
    /// <summary>Completed snapshot; replacing the recorder buffer cannot change this recording.</summary>
    public sealed class EchoRecording
    {
        private readonly EchoSample[] samples;

        internal EchoRecording(EchoSample[] samples, double duration)
        {
            this.samples = samples;
            Duration = duration;
        }

        public double Duration { get; }
        public int SampleCount => samples.Length;
        public EchoSample this[int index] => samples[index];

        /// <summary>Evaluates a pose at elapsed seconds, independent of rendered frames.</summary>
        public bool TryEvaluate(double elapsed, out EchoSample pose)
        {
            pose = default;
            if (samples.Length == 0)
                return false;

            if (samples.Length == 1 || elapsed <= samples[0].Time)
            {
                pose = samples[0];
                return true;
            }

            var last = samples.Length - 1;
            if (elapsed >= samples[last].Time)
            {
                pose = samples[last];
                return true;
            }

            var low = 0;
            var high = last;
            while (high - low > 1)
            {
                var middle = (low + high) / 2;
                if (samples[middle].Time <= elapsed)
                    low = middle;
                else
                    high = middle;
            }

            var start = samples[low];
            var end = samples[high];
            var span = end.Time - start.Time;
            var fraction = span > 0d ? (float)((elapsed - start.Time) / span) : 1f;
            pose = new EchoSample(elapsed,
                Vector3.Lerp(start.Position, end.Position, fraction),
                Quaternion.Slerp(start.Rotation, end.Rotation, fraction));
            return true;
        }
    }
}

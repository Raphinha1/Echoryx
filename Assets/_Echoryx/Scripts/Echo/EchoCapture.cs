using System;
using UnityEngine;

namespace Echoryx.Echo
{
    /// <summary>Bounded recorder and state machine. All sampling uses one supplied clock.</summary>
    public sealed class EchoCapture
    {
        public const float HardMaximumSeconds = 5f;

        private readonly EchoSample[] buffer;
        private readonly double maximumSeconds;
        private readonly double sampleInterval;
        private double startedAt;
        private double previousTime;
        private Vector3 previousPosition;
        private Quaternion previousRotation;
        private int nextSampleIndex;
        private int count;
        // The recording kept while a replacement is being captured; restored if that capture is aborted.
        private EchoRecording replaced;

        public EchoCapture(float maximumSeconds = HardMaximumSeconds, int samplesPerSecond = 20)
        {
            if (maximumSeconds <= 0f || maximumSeconds > HardMaximumSeconds)
                throw new ArgumentOutOfRangeException(nameof(maximumSeconds));
            if (samplesPerSecond <= 0)
                throw new ArgumentOutOfRangeException(nameof(samplesPerSecond));

            this.maximumSeconds = maximumSeconds;
            sampleInterval = 1d / samplesPerSecond;
            // Initial sample, scheduled samples and a possible final partial interval.
            buffer = new EchoSample[(int)Math.Ceiling(maximumSeconds * samplesPerSecond) + 2];
        }

        public EchoState State { get; private set; } = EchoState.Available;
        public EchoRecording ReadyRecording { get; private set; }
        public int SampleCount => count;

        public bool Begin(double time, Vector3 position, Quaternion rotation)
        {
            if (State == EchoState.Recording || State == EchoState.Playing)
                return false;

            // A new recording replaces the previous one only once it completes.
            replaced = State == EchoState.Ready ? ReadyRecording : null;
            ReadyRecording = null;
            startedAt = previousTime = time;
            previousPosition = position;
            previousRotation = rotation;
            count = 0;
            nextSampleIndex = 1;
            buffer[count++] = new EchoSample(0d, position, rotation);
            State = EchoState.Recording;
            return true;
        }

        /// <returns>True when the duration limit completed the recording.</returns>
        public bool Advance(double time, Vector3 position, Quaternion rotation)
        {
            if (State != EchoState.Recording)
                return false;

            time = Math.Max(time, previousTime);
            var elapsed = Math.Min(time - startedAt, maximumSeconds);
            while (nextSampleIndex * sampleInterval <= elapsed + 1e-9d)
            {
                var sampleTime = nextSampleIndex * sampleInterval;
                var alpha = InterpolationFraction(startedAt + sampleTime, time);
                buffer[count++] = new EchoSample(
                    sampleTime,
                    Vector3.LerpUnclamped(previousPosition, position, alpha),
                    Quaternion.SlerpUnclamped(previousRotation, rotation, alpha));
                nextSampleIndex++;
            }

            if (elapsed >= maximumSeconds)
            {
                var alpha = InterpolationFraction(startedAt + maximumSeconds, time);
                Complete(maximumSeconds,
                    Vector3.LerpUnclamped(previousPosition, position, alpha),
                    Quaternion.SlerpUnclamped(previousRotation, rotation, alpha));
                return true;
            }

            previousTime = time;
            previousPosition = position;
            previousRotation = rotation;
            return false;
        }

        public bool Stop(double time, Vector3 position, Quaternion rotation)
        {
            if (State != EchoState.Recording)
                return false;

            if (Advance(time, position, rotation))
                return true;

            var elapsed = Math.Max(0d, previousTime - startedAt);
            Complete(elapsed, position, rotation);
            return true;
        }

        public bool TryBeginPlayback()
        {
            if (State != EchoState.Ready || ReadyRecording == null
                || ReadyRecording.SampleCount == 0)
                return false;

            State = EchoState.Playing;
            return true;
        }

        /// <summary>Playback finished: the recording is kept, so it can be played again.</summary>
        public bool CompletePlayback()
        {
            if (State != EchoState.Playing)
                return false;

            State = EchoState.Ready;
            return true;
        }

        /// <summary>
        /// Stops the current activity without discarding what the player already has:
        /// Playing returns to Ready; an unfinished Recording is dropped and the previous recording (if any) returns.
        /// </summary>
        public bool ReleaseActivity()
        {
            if (State == EchoState.Playing)
            {
                State = EchoState.Ready;
                return true;
            }
            if (State != EchoState.Recording)
                return false;

            ReadyRecording = replaced;
            replaced = null;
            count = ReadyRecording != null ? ReadyRecording.SampleCount : 0;
            State = ReadyRecording != null ? EchoState.Ready : EchoState.Available;
            return true;
        }

        public bool Cancel()
        {
            if (State == EchoState.Available)
                return false;

            ReadyRecording = null;
            replaced = null;
            count = 0;
            State = EchoState.Available;
            return true;
        }

        private float InterpolationFraction(double sampleTime, double currentTime)
        {
            var span = currentTime - previousTime;
            return span > 0d
                ? (float)Math.Max(0d, Math.Min(1d, (sampleTime - previousTime) / span))
                : 1f;
        }

        private void Complete(double duration, Vector3 position, Quaternion rotation)
        {
            if (duration - buffer[count - 1].Time > 1e-8d)
                buffer[count++] = new EchoSample(duration, position, rotation);

            var snapshot = new EchoSample[count];
            Array.Copy(buffer, snapshot, count);
            ReadyRecording = new EchoRecording(snapshot, duration);
            replaced = null;
            State = EchoState.Ready;
        }
    }
}

using UnityEngine;

namespace Echoryx.Echo
{
    /// <summary>Samples the player's final pose after PlayerMotor.Update.</summary>
    [DefaultExecutionOrder(50)]
    public sealed class EchoRecorder : MonoBehaviour
    {
        [SerializeField, Range(0.1f, EchoCapture.HardMaximumSeconds)]
        private float maximumSeconds = EchoCapture.HardMaximumSeconds;
        [SerializeField, Range(1, 60)] private int samplesPerSecond = 20;
        [SerializeField] private bool logTransitions = true;

        private EchoCapture capture;

        public EchoState State => capture?.State ?? EchoState.Available;
        public EchoRecording ReadyRecording => capture?.ReadyRecording;
        public int SampleCount => capture?.SampleCount ?? 0;

        private void Awake()
        {
            capture = new EchoCapture(maximumSeconds, samplesPerSecond);
        }

        private void Update()
        {
            if (capture == null || State != EchoState.Recording)
                return;

            if (capture.Advance(Time.timeAsDouble, transform.position, transform.rotation))
                LogCompletion("limit");
        }

        public void Toggle()
        {
            if (capture == null)
                return;

            if (State == EchoState.Recording)
                Stop();
            else if (State == EchoState.Available || State == EchoState.Ready)
                Begin();
        }

        public bool Begin()
        {
            if (capture == null || !capture.Begin(Time.timeAsDouble, transform.position, transform.rotation))
                return false;

            if (logTransitions)
                Debug.Log($"Echo recording started at {Time.timeAsDouble:F2}s.", this);
            return true;
        }

        public bool Stop()
        {
            if (capture == null || !capture.Stop(Time.timeAsDouble, transform.position, transform.rotation))
                return false;

            LogCompletion("manual");
            return true;
        }

        public bool TryBeginPlayback(out EchoRecording recording)
        {
            recording = null;
            if (capture == null || !capture.TryBeginPlayback())
                return false;

            recording = capture.ReadyRecording;
            if (logTransitions)
                Debug.Log($"Echo playback started: {recording.Duration:F2}s, " +
                    $"{recording.SampleCount} samples.", this);
            return true;
        }

        public bool CompletePlayback()
        {
            if (capture == null || !capture.CompletePlayback())
                return false;

            if (logTransitions)
                Debug.Log("Echo playback completed.", this);
            return true;
        }

        [ContextMenu("Cancel Echo Recording")]
        public void Cancel()
        {
            var previousState = State;
            if (capture != null && capture.Cancel() && logTransitions)
                Debug.Log(previousState == EchoState.Playing
                    ? "Echo playback cancelled."
                    : "Echo recording cancelled.", this);
        }

        private void LogCompletion(string reason)
        {
            if (!logTransitions) return;
            var recording = ReadyRecording;
            Debug.Log($"Echo recording stopped ({reason}): {recording.Duration:F2}s, " +
                $"{recording.SampleCount} samples.", this);
        }
    }
}

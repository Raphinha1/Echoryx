using UnityEngine;

namespace Echoryx.Rewind
{
    /// <summary>Base for kinematic mechanisms; normal motion runs only outside Rewind.</summary>
    [DefaultExecutionOrder(140)]
    public abstract class RewindableMechanism : MonoBehaviour, IRewindableMechanism
    {
        [SerializeField, Range(0.05f, RewindHistory.HardMaximumSeconds)]
        private float historySeconds = RewindHistory.HardMaximumSeconds;
        [SerializeField, Range(1, 120)] private int samplesPerSecond = 20;
        [SerializeField, Min(0.01f)] private float rewindSpeed = 1f;

        private RewindHistory history;
        private RewindPose initialPose;
        private int resetFrame = -1;
        private bool resetPending;
        private static RewindableMechanism activeMechanism;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetActiveMechanism() => activeMechanism = null;

        public RewindMode Mode => history?.Mode ?? RewindMode.Normal;
        public bool IsAvailable => isActiveAndEnabled;
        public bool CanRewind => !resetPending && history != null && history.CanRewind;
        protected double TimelineTime => history?.Time ?? 0d;

        protected virtual RewindPose CapturePose() =>
            new RewindPose(transform.position, transform.rotation);

        protected virtual void ApplyPose(RewindPose pose) =>
            transform.SetPositionAndRotation(pose.Position, pose.Rotation);

        /// <summary>Implement normal motion here; it is never called during Rewind.</summary>
        protected abstract void AdvanceNormal(float deltaTime);

        protected virtual void Awake()
        {
            history = new RewindHistory(historySeconds, samplesPerSecond);
            initialPose = CapturePose();
            history.Reset(initialPose);
        }

        protected virtual void LateUpdate() => TickTimeline();

        /// <summary>Allows a passenger-carrying mechanism to tick before PlayerMotor.Update.</summary>
        protected void TickTimeline()
        {
            if (history == null || resetPending || resetFrame == Time.frameCount) return;
            if (history.Mode == RewindMode.Rewinding)
            {
                if (history.StepRewind(Time.deltaTime * rewindSpeed, out var pose))
                    ApplyPose(pose);
                return;
            }

            AdvanceNormal(Time.deltaTime);
            history.AdvanceNormal(Time.deltaTime, CapturePose());
        }

        public bool BeginRewind()
        {
            if (resetPending || history == null || (activeMechanism != null && activeMechanism != this)
                || !history.BeginRewind(CapturePose())) return false;
            activeMechanism = this;
            return true;
        }

        public bool EndRewind()
        {
            if (history == null || !history.EndRewind(out var pose)) return false;
            ApplyPose(pose);
            if (activeMechanism == this) activeMechanism = null;
            return true;
        }

        public void ResetHistory()
        {
            if (activeMechanism == this) activeMechanism = null;
            history?.Reset(CapturePose());
        }

        public void SuspendForReset()
        {
            EndRewind();
            resetPending = true;
        }

        /// <summary>Restores the authored pose and starts a fresh local timeline.
        /// Bypasses ApplyPose: a room reset must never offer teleport motion to a passenger.</summary>
        public void ResetForAttempt()
        {
            if (history == null) return;
            if (activeMechanism == this) activeMechanism = null;
            transform.SetPositionAndRotation(initialPose.Position, initialPose.Rotation);
            history.Reset(initialPose);
            resetPending = false;
            resetFrame = Time.frameCount;
        }

        protected virtual void OnDisable()
        {
            if (Mode == RewindMode.Rewinding) EndRewind();
        }

        protected virtual void OnValidate()
        {
            historySeconds = Mathf.Clamp(historySeconds, 0.05f, RewindHistory.HardMaximumSeconds);
            samplesPerSecond = Mathf.Clamp(samplesPerSecond, 1, 120);
            rewindSpeed = Mathf.Max(0.01f, rewindSpeed);
        }
    }
}

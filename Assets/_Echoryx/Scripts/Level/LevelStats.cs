using System;
using Echoryx.Rewind;
using UnityEngine;

namespace Echoryx.Level
{
    /// <summary>Counts restarts, Rewind holds and time. Reads existing state; changes none of it.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class LevelStats : MonoBehaviour
    {
        [SerializeField] private RewindableMechanism[] watchedMechanisms = Array.Empty<RewindableMechanism>();

        private readonly LevelStatsTracker tracker = new LevelStatsTracker();
        private bool[] wasRewinding;

        public event Action Completed;

        public int Attempts => tracker.Attempts;
        public int RewindUses => tracker.RewindUses;
        public bool IsCompleted => tracker.Completed;
        public double ElapsedSeconds => tracker.ElapsedAt(Time.timeAsDouble);

        private void Start()
        {
            tracker.Start(Time.timeAsDouble);
            wasRewinding = new bool[watchedMechanisms.Length];
        }

        private void LateUpdate()
        {
            if (wasRewinding == null) return;
            for (var i = 0; i < watchedMechanisms.Length; i++)
            {
                var mechanism = watchedMechanisms[i];
                var rewinding = mechanism != null && mechanism.Mode == RewindMode.Rewinding;
                if (rewinding && !wasRewinding[i]) tracker.RegisterRewind();
                wasRewinding[i] = rewinding;
            }
        }

        public void RegisterAttempt() => tracker.RegisterAttempt();

        public void Complete()
        {
            if (tracker.Complete(Time.timeAsDouble)) Completed?.Invoke();
        }
    }
}

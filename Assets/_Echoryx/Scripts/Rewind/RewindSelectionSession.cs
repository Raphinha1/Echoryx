using UnityEngine;

namespace Echoryx.Rewind
{
    /// <summary>One RMB press can acquire at most one mechanism.</summary>
    public sealed class RewindSelectionSession
    {
        private IRewindableMechanism target;
        private IRewindableMechanism attempted;
        private bool lockedOut;

        public bool IsHolding { get; private set; }
        public IRewindableMechanism Target => IsValid(target) ? target : null;
        public IRewindableMechanism AttemptedTarget => IsValid(attempted) ? attempted : null;

        public void Update(bool pressed, IRewindableMechanism aimed)
        {
            if (!pressed)
            {
                Release();
                return;
            }

            if (!IsHolding)
            {
                IsHolding = true;
                attempted = IsValid(aimed) ? aimed : null;
                lockedOut = attempted == null;
                TryStartAttempted();
                return;
            }

            if (target != null && (!IsValid(target) || target.Mode != RewindMode.Rewinding))
            {
                EndTarget();
                target = null;
                lockedOut = true;
            }
            if (target == null && !lockedOut) TryStartAttempted();
        }

        public void Release()
        {
            EndTarget();
            target = null;
            attempted = null;
            lockedOut = false;
            IsHolding = false;
        }

        private void TryStartAttempted()
        {
            if (!IsValid(attempted))
            {
                lockedOut = true;
                return;
            }
            if (attempted.CanRewind && attempted.BeginRewind()) target = attempted;
        }

        private void EndTarget()
        {
            if (target is Object unityObject && unityObject == null) return;
            if (target != null && target.Mode == RewindMode.Rewinding)
                target.EndRewind();
        }

        private static bool IsValid(IRewindableMechanism mechanism)
        {
            if (mechanism == null) return false;
            if (mechanism is Object unityObject && unityObject == null) return false;
            return mechanism.IsAvailable;
        }
    }
}

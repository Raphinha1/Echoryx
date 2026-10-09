using UnityEngine;

namespace Echoryx.Game
{
    /// <summary>Minimal opt-in mechanism that restores its authored local pose.</summary>
    public sealed class AttemptResetTransform : MonoBehaviour, IAttemptResetParticipant
    {
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private bool initialized;

        private void Awake()
        {
            initialPosition = transform.localPosition;
            initialRotation = transform.localRotation;
            initialized = true;
        }

        public void ReleaseForReset() { }

        public void ResetForAttempt()
        {
            if (!initialized) return;
            transform.SetLocalPositionAndRotation(initialPosition, initialRotation);
        }
    }
}

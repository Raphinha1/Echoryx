using Echoryx.Game;
using Echoryx.Rewind.Platforms;
using UnityEngine;

namespace Echoryx.Level
{
    /// <summary>
    /// Room reset participant for a one-way platform: after a reset it is restored AND parked at its
    /// destination (out of reach), the same state a player finds on arriving at the room. Reaching it
    /// then always requires Rewind. Replaces RewindMechanismAttemptReset for this platform.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RewindMovingPlatform))]
    public sealed class ParkedPlatformAttemptReset : MonoBehaviour, IAttemptResetParticipant
    {
        public void ReleaseForReset() => GetComponent<RewindMovingPlatform>().SuspendForReset();

        public void ResetForAttempt()
        {
            var platform = GetComponent<RewindMovingPlatform>();
            platform.ResetForAttempt();
            platform.ParkAtDestination();
        }
    }
}

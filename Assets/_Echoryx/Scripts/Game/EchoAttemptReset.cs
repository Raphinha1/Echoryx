using Echoryx.Echo;
using UnityEngine;

namespace Echoryx.Game
{
    /// <summary>Global player ability; never registered in a room's participant list.</summary>
    [DisallowMultipleComponent]
    public sealed class EchoAttemptReset : MonoBehaviour, IAttemptResetParticipant
    {
        [SerializeField] private EchoPlayback playback;
        public void ReleaseForReset() => playback?.ResetForAttempt();
        // Echo is already cleared in the immediate release phase.
        public void ResetForAttempt() { }
    }
}

using Echoryx.Rewind;
using UnityEngine;

namespace Echoryx.Game
{
    [DisallowMultipleComponent]
    public sealed class RewindMechanismAttemptReset : MonoBehaviour, IAttemptResetParticipant
    {
        public void ReleaseForReset() => GetComponent<RewindableMechanism>()?.SuspendForReset();
        public void ResetForAttempt() => GetComponent<RewindableMechanism>()?.ResetForAttempt();
    }
}

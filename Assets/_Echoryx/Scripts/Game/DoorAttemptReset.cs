using Echoryx.Interactions;
using UnityEngine;

namespace Echoryx.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DoorController))]
    public sealed class DoorAttemptReset : MonoBehaviour, IAttemptResetParticipant
    {
        public void ReleaseForReset() => GetComponent<DoorController>().SuspendForReset();
        public void ResetForAttempt() => GetComponent<DoorController>().ResetForAttempt();
    }
}

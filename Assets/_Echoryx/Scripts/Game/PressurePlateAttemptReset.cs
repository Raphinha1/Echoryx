using Echoryx.Interactions;
using UnityEngine;

namespace Echoryx.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PressurePlate))]
    public sealed class PressurePlateAttemptReset : MonoBehaviour, IAttemptResetParticipant
    {
        public void ReleaseForReset() => GetComponent<PressurePlate>().SuspendForReset();
        public void ResetForAttempt() => GetComponent<PressurePlate>().ResetForAttempt();
    }
}

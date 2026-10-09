using Echoryx.Rewind;
using UnityEngine;

namespace Echoryx.Game
{
    /// <summary>S04-A releases the player's target; temporal mechanism resets belong to S04-B.</summary>
    public sealed class RewindAttemptRelease : MonoBehaviour, IAttemptResetParticipant
    {
        [SerializeField] private RewindInteractor interactor;

        public void ReleaseForReset() => interactor?.ReleaseForRespawn();
        public void ResetForAttempt() { }
    }
}

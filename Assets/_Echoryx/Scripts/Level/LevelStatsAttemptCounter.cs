using Echoryx.Game;
using UnityEngine;

namespace Echoryx.Level
{
    /// <summary>Room reset participant: every respawn resets the active room once, so it counts one attempt.</summary>
    public sealed class LevelStatsAttemptCounter : MonoBehaviour, IAttemptResetParticipant
    {
        [SerializeField] private LevelStats stats;

        public void ReleaseForReset() { }
        public void ResetForAttempt() => stats?.RegisterAttempt();
    }
}

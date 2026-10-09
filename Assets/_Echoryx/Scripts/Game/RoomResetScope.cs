using System;
using UnityEngine;

namespace Echoryx.Game
{
    /// <summary>Explicit, ordered participants owned by this room; no scene-wide search.</summary>
    public sealed class RoomResetScope : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour[] participants = Array.Empty<MonoBehaviour>();

        public void ReleaseInteractions() => RunPhase(true);
        public void ResetMechanisms() => RunPhase(false);

        private void RunPhase(bool release)
        {
            for (var i = 0; i < participants.Length; i++)
            {
                var component = participants[i];
                if (component == null) continue;
                var duplicate = false;
                for (var earlier = 0; earlier < i; earlier++)
                    if (participants[earlier] == component) { duplicate = true; break; }
                if (duplicate) continue;
                if (!(component is IAttemptResetParticipant participant)
                    || component.GetComponentInParent<RoomResetScope>(true) != this)
                {
                    Debug.LogError("Reset participant must implement IAttemptResetParticipant and belong to this room.", component);
                    continue;
                }
                try
                {
                    if (release) participant.ReleaseForReset();
                    else participant.ResetForAttempt();
                }
                catch (Exception exception) { Debug.LogException(exception, component); }
            }
        }
    }
}

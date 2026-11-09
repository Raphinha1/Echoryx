using Echoryx.Rewind;
using UnityEngine;

namespace Echoryx.Level
{
    /// <summary>
    /// Violet beacon for a rewindable mechanism (same violet as RewindMechanismFeedback): strongest while it
    /// is rewinding, bright when it can be rewound and the player is within aiming range, dim otherwise.
    /// Reads RewindableMechanism state only.
    /// </summary>
    public sealed class RewindBeaconDriver : MonoBehaviour
    {
        [SerializeField] private RewindableMechanism mechanism;
        [SerializeField] private InteractionBeacon beacon;
        [SerializeField, Min(0f)] private float aimRange = 10f;
        [SerializeField] private Color color = new Color(0.78f, 0.38f, 1f);

        public static float Evaluate(bool canRewind, bool rewinding, bool inRange) =>
            rewinding ? 1f : !canRewind ? 0.1f : inRange ? 0.75f : 0.25f;

        private void Awake()
        {
            if (mechanism == null) mechanism = GetComponentInParent<RewindableMechanism>();
            if (beacon == null) beacon = GetComponentInChildren<InteractionBeacon>(true);
        }

        private void LateUpdate()
        {
            if (beacon == null || mechanism == null) return;
            var inRange = PlayerProbe.TryGetPlayerPosition(out var player)
                && (player - mechanism.transform.position).sqrMagnitude <= aimRange * aimRange;
            beacon.Apply(color, Evaluate(mechanism.CanRewind, mechanism.Mode == RewindMode.Rewinding, inRange));
        }
    }
}

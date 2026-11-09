using Echoryx.Interactions;
using UnityEngine;

namespace Echoryx.Level
{
    /// <summary>
    /// Colors the plate's beacon with the game's convention: orange = idle, green = held by the player,
    /// cyan = held only by an Echo. Brighter when the player is close. Reads PressurePlate only.
    /// </summary>
    [RequireComponent(typeof(PressurePlate))]
    public sealed class PlateBeaconDriver : MonoBehaviour
    {
        [SerializeField] private InteractionBeacon beacon;
        [SerializeField, Min(0f)] private float nearRange = 6f;
        [SerializeField] private Color idleColor = new Color(1f, 0.5f, 0.12f);
        [SerializeField] private Color playerColor = new Color(0.2f, 1f, 0.45f);
        [SerializeField] private Color echoColor = new Color(0.2f, 0.9f, 1f);

        private PressurePlate plate;

        public static float Evaluate(bool pressed, bool playerNear) => pressed ? 0.85f : playerNear ? 0.5f : 0.3f;

        private void Awake()
        {
            plate = GetComponent<PressurePlate>();
            if (beacon == null) beacon = GetComponentInChildren<InteractionBeacon>(true);
        }

        private void LateUpdate()
        {
            if (beacon == null) return;
            var near = PlayerProbe.TryGetPlayerPosition(out var player)
                && (player - transform.position).sqrMagnitude <= nearRange * nearRange;
            var color = !plate.IsPressed ? idleColor : plate.PlayerPresent ? playerColor : echoColor;
            beacon.Apply(color, Evaluate(plate.IsPressed, near));
        }
    }
}

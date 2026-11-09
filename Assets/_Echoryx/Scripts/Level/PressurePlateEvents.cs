using Echoryx.Interactions;
using UnityEngine;
using UnityEngine.Events;

namespace Echoryx.Level
{
    /// <summary>Raises events when a PressurePlate changes state (player or Echo). Does not alter the plate.</summary>
    [DefaultExecutionOrder(130)]
    [RequireComponent(typeof(PressurePlate))]
    public sealed class PressurePlateEvents : MonoBehaviour
    {
        [SerializeField] private UnityEvent activated = new UnityEvent();
        [SerializeField] private UnityEvent deactivated = new UnityEvent();
        private PressurePlate plate;
        private bool wasPressed;

        public UnityEvent Activated => activated;
        public UnityEvent Deactivated => deactivated;

        private void Awake() => plate = GetComponent<PressurePlate>();

        private void LateUpdate()
        {
            var pressed = plate.IsPressed;
            if (pressed == wasPressed) return;
            wasPressed = pressed;
            if (pressed) activated.Invoke();
            else deactivated.Invoke();
        }
    }
}

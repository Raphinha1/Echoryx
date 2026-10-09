using UnityEngine;

namespace Echoryx.Player
{
    /// <summary>Queues one platform's frame displacement for PlayerMotor's next Move.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerPlatformCarrier : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float verticalTolerance = 0.2f;
        [SerializeField, Min(0f)] private float horizontalMargin = 0.1f;

        private CharacterController controller;
        private Vector3 pendingDisplacement;
        private bool hasPending;
        private bool awaitingMoveAfterReset;

        private void Awake() => controller = GetComponent<CharacterController>();

        public bool IsStandingOn(BoxCollider surface)
        {
            return !awaitingMoveAfterReset && isActiveAndEnabled && controller != null && controller.isGrounded
                && surface != null && surface.enabled && surface.gameObject.activeInHierarchy
                && PlatformRidePolicy.IsSupported(transform.position, surface.bounds,
                    verticalTolerance, horizontalMargin);
        }

        public void OfferMotion(Vector3 displacement)
        {
            if (!isActiveAndEnabled || awaitingMoveAfterReset || hasPending) return;
            pendingDisplacement = displacement;
            hasPending = true;
        }

        public Vector3 ConsumeMotion()
        {
            var result = isActiveAndEnabled && hasPending ? pendingDisplacement : Vector3.zero;
            pendingDisplacement = Vector3.zero;
            hasPending = false;
            return result;
        }

        private void OnDisable() => ConsumeMotion();

        public void ResetTransport()
        {
            ConsumeMotion();
            awaitingMoveAfterReset = true;
        }

        public void CompletePlayerMove() => awaitingMoveAfterReset = false;
    }
}

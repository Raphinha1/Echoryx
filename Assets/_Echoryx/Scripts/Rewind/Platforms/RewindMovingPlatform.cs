using Echoryx.Player;
using UnityEngine;

namespace Echoryx.Rewind.Platforms
{
    /// <summary>Moves before PlayerMotor and offers grounded riders the same frame delta.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class RewindMovingPlatform : RewindableMechanism
    {
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform endPoint;
        [SerializeField] private BoxCollider surface;
        [SerializeField] private PlayerPlatformCarrier passenger;
        [SerializeField, Min(0f)] private float speed = 3.5f;

        protected override void Awake()
        {
            if (startPoint != null) transform.position = startPoint.position;
            base.Awake();
        }

        private void Update()
        {
            // Do not age away the trip while parked at the destination.
            // Rewind still ticks, then normal travel resumes from the recovered time.
            if (Mode == RewindMode.Rewinding ||
                (startPoint != null && endPoint != null &&
                    MovingPlatformPath.HasRemainingTravel(startPoint.position,
                        endPoint.position, speed, TimelineTime)))
                TickTimeline();
        }

        // The platform ticks in Update, before PlayerMotor. Other mechanisms keep LateUpdate.
        protected override void LateUpdate() { }

        protected override void AdvanceNormal(float deltaTime)
        {
            if (startPoint == null || endPoint == null) return;
            var next = MovingPlatformPath.Evaluate(startPoint.position, endPoint.position,
                speed, TimelineTime + deltaTime);
            MoveTo(next.Position);
        }

        protected override void ApplyPose(RewindPose pose)
        {
            MoveTo(pose.Position);
            transform.rotation = pose.Rotation;
        }

        private void MoveTo(Vector3 destination)
        {
            var before = transform.position;
            var riding = passenger != null && passenger.IsStandingOn(surface);
            transform.position = destination;
            var delta = destination - before;
            if (delta.sqrMagnitude <= 1e-10f) return;
            Physics.SyncTransforms();
            if (riding) passenger.OfferMotion(delta);
        }

        private void OnDrawGizmosSelected()
        {
            if (startPoint == null || endPoint == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(startPoint.position, endPoint.position);
            Gizmos.DrawWireSphere(startPoint.position, 0.15f);
            Gizmos.DrawWireSphere(endPoint.position, 0.15f);
        }
    }
}

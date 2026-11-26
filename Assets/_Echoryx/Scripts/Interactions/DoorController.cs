using UnityEngine;

namespace Echoryx.Interactions
{
    /// <summary>Moves a solid door; threshold occupancy prevents closure through the player.</summary>
    [DefaultExecutionOrder(140)]
    public sealed class DoorController : MonoBehaviour
    {
        [SerializeField] private PressurePlate plate;
        [SerializeField] private Transform doorPanel;
        [SerializeField] private Transform threshold;
        [SerializeField] private Vector3 thresholdSize = new Vector3(3.5f, 3f, 2f);
        [SerializeField] private Vector3 openOffset = new Vector3(0f, 4.2f, 0f);
        [SerializeField, Min(0.01f)] private float moveSpeed = 12f;

        private readonly DoorGateState gate = new DoorGateState();
        private Vector3 closedLocalPosition;
        private int resetFrame = -1;
        private bool resetPending;

        /// <summary>The moving panel, read-only for presentation (e.g. door sounds).</summary>
        public Transform Panel => doorPanel;

        public bool IsOpen => doorPanel != null
            && Vector3.Distance(doorPanel.localPosition, closedLocalPosition + openOffset) < 0.001f;

        private void Awake()
        {
            if (doorPanel != null) closedLocalPosition = doorPanel.localPosition;
        }

        private void LateUpdate()
        {
            if (resetPending || resetFrame == Time.frameCount || plate == null || doorPanel == null || threshold == null) return;
            var fullyClosed = Vector3.Distance(doorPanel.localPosition, closedLocalPosition) < 0.001f;
            var open = gate.ShouldOpen(plate.IsPressed, PlayerInThreshold(), fullyClosed);
            var target = closedLocalPosition + (open ? openOffset : Vector3.zero);
            doorPanel.localPosition = Vector3.MoveTowards(
                doorPanel.localPosition, target, moveSpeed * Time.deltaTime);
        }

        private bool PlayerInThreshold()
        {
            var actors = MechanismActor.ActiveActors;
            for (var i = 0; i < actors.Count; i++)
            {
                var actor = actors[i];
                if (actor == null || !actor.isActiveAndEnabled
                    || actor.Kind != MechanismActorKind.Player) continue;
                if (PlateOccupancy.Contains(
                    threshold.InverseTransformPoint(actor.transform.position), Vector3.zero, thresholdSize))
                    return true;
            }
            return false;
        }

        private void OnDisable() => gate.Reset();

        public void ResetForAttempt()
        {
            gate.Reset();
            if (doorPanel != null) doorPanel.localPosition = closedLocalPosition;
            resetPending = false;
            resetFrame = Time.frameCount;
        }

        public void SuspendForReset() => resetPending = true;

        private void OnDrawGizmosSelected()
        {
            if (threshold == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.matrix = threshold.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, thresholdSize);
        }
    }
}

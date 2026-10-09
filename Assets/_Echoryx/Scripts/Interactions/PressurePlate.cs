using System.Collections.Generic;
using UnityEngine;

namespace Echoryx.Interactions
{
    /// <summary>Polls actor positions so an Echo needs no collider or Rigidbody.</summary>
    [DefaultExecutionOrder(120)]
    public sealed class PressurePlate : MonoBehaviour
    {
        [SerializeField] private Vector3 detectionCenter = new Vector3(0f, 0.65f, 0f);
        [SerializeField] private Vector3 detectionSize = new Vector3(2f, 1.5f, 2f);
        [SerializeField] private Renderer indicator;
        [SerializeField] private Color idleColor = new Color(0.85f, 0.28f, 0.08f);
        [SerializeField] private Color pressedColor = new Color(0.1f, 0.9f, 0.4f);

        private readonly PlateOccupancy occupancy = new PlateOccupancy();
        private MaterialPropertyBlock properties;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public bool IsPressed => occupancy.IsPressed;
        public bool PlayerPresent => occupancy.PlayerPresent;
        public bool EchoPresent => occupancy.EchoPresent;

        private void Awake()
        {
            properties = new MaterialPropertyBlock();
            UpdateIndicator();
        }

        private void LateUpdate() => Refresh();

        public void Refresh() => RefreshFrom(MechanismActor.ActiveActors);

        public void RefreshFrom(IReadOnlyList<MechanismActor> actors)
        {
            var player = false;
            var echo = false;
            for (var i = 0; i < actors.Count; i++)
            {
                var actor = actors[i];
                if (actor == null || !actor.isActiveAndEnabled) continue;
                var local = transform.InverseTransformPoint(actor.transform.position);
                if (!PlateOccupancy.Contains(local, detectionCenter, detectionSize)) continue;
                if (actor.Kind == MechanismActorKind.Player) player = true;
                else if (actor.Kind == MechanismActorKind.Echo) echo = true;
            }

            if (player == occupancy.PlayerPresent && echo == occupancy.EchoPresent) return;
            occupancy.Set(player, echo);
            UpdateIndicator();
        }

        private void OnDisable()
        {
            occupancy.Clear();
            UpdateIndicator();
        }

        private void UpdateIndicator()
        {
            if (indicator == null || properties == null) return;
            indicator.GetPropertyBlock(properties);
            properties.SetColor(BaseColor, occupancy.IsPressed ? pressedColor : idleColor);
            indicator.SetPropertyBlock(properties);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(detectionCenter, detectionSize);
        }
    }
}

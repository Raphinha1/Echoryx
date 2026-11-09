using Echoryx.Rewind;
using UnityEngine;

namespace Echoryx.Level
{
    /// <summary>Glows while the mechanism is being rewound. Reads RewindableMechanism.Mode only.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class RewindMechanismFeedback : MonoBehaviour
    {
        [SerializeField] private RewindableMechanism mechanism;
        [SerializeField] private Renderer[] renderers = System.Array.Empty<Renderer>();
        [SerializeField, ColorUsage(false, true)] private Color rewindEmission = new Color(1.6f, 0.35f, 2.4f);
        [SerializeField] private Color rewindTint = new Color(0.85f, 0.6f, 1f);
        [SerializeField, Min(0f)] private float pulseSpeed = 6f;

        private MaterialPropertyBlock block;
        private bool applied;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        public bool Glowing => applied;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            if (mechanism == null) mechanism = GetComponentInParent<RewindableMechanism>();
        }

        private void LateUpdate()
        {
            if (mechanism == null) return;
            var rewinding = mechanism.Mode == RewindMode.Rewinding;
            if (!rewinding && !applied) return;
            var pulse = rewinding ? 0.75f + 0.25f * Mathf.Sin(Time.time * pulseSpeed) : 0f;
            foreach (var target in renderers)
            {
                if (target == null) continue;
                target.GetPropertyBlock(block);
                block.SetColor(BaseColor, rewinding ? rewindTint : Color.white);
                block.SetColor(EmissionColor, rewinding ? rewindEmission * pulse : Color.black);
                target.SetPropertyBlock(block);
            }
            applied = rewinding;
        }
    }
}

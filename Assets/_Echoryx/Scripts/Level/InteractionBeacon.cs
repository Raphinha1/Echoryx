using UnityEngine;

namespace Echoryx.Level
{
    /// <summary>
    /// Subtle constant "trail" that marks an interactive object: a pulsing glow disc plus a few rising
    /// particles (local space, so it follows a moving object). A driver sets color and intensity
    /// (0 = dim idle, 1 = fully available); this component only presents. Never static, no physics.
    /// </summary>
    [DefaultExecutionOrder(210)]
    public sealed class InteractionBeacon : MonoBehaviour
    {
        [SerializeField] private Renderer halo;
        [SerializeField] private ParticleSystem trail;
        [SerializeField, Range(0f, 1f)] private float idleHaloAlpha = 0.07f;
        [SerializeField, Range(0f, 1f)] private float activeHaloAlpha = 0.5f;
        [SerializeField, Min(0f)] private float idleRate = 1.5f;
        [SerializeField, Min(0f)] private float activeRate = 9f;
        [SerializeField, Min(0f)] private float pulseSpeed = 2.2f;
        [SerializeField, Range(0f, 1f)] private float pulseDepth = 0.3f;
        [SerializeField, Min(0.01f)] private float smoothTime = 0.2f;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock block;
        private Color targetColor = Color.white;
        private Color shownColor = Color.white;
        private float targetIntensity;
        private float intensity;
        private float intensityVelocity;
        private float lastRate = -1f;
        private bool colorInitialized;

        public Color Color => shownColor;
        public Color TargetColor => targetColor;
        public float Intensity => intensity;
        public float TargetIntensity => targetIntensity;
        public ParticleSystem Trail => trail;

        /// <summary>Sets what to show; the beacon eases toward it.</summary>
        public void Apply(Color color, float intensity01)
        {
            targetColor = color;
            targetIntensity = Mathf.Clamp01(intensity01);
            if (!colorInitialized)
            {
                shownColor = color;
                intensity = targetIntensity;
                colorInitialized = true;
            }
        }

        private void Awake() => block = new MaterialPropertyBlock();

        private void OnEnable()
        {
            if (trail != null && !trail.isPlaying) trail.Play();
        }

        private void LateUpdate()
        {
            var dt = Time.deltaTime;
            intensity = Mathf.SmoothDamp(intensity, targetIntensity, ref intensityVelocity, smoothTime, Mathf.Infinity, dt);
            shownColor = Color.Lerp(shownColor, targetColor, 1f - Mathf.Exp(-dt / 0.12f));

            var pulse = 1f - pulseDepth * 0.5f * (1f + Mathf.Sin(Time.time * pulseSpeed));
            if (halo != null)
            {
                var alpha = Mathf.Lerp(idleHaloAlpha, activeHaloAlpha, intensity) * pulse;
                halo.GetPropertyBlock(block);
                block.SetColor(BaseColor, new Color(shownColor.r, shownColor.g, shownColor.b, alpha));
                halo.SetPropertyBlock(block);
            }

            if (trail == null) return;
            var main = trail.main;
            main.startColor = new ParticleSystem.MinMaxGradient(shownColor);
            var rate = Mathf.Lerp(idleRate, activeRate, intensity);
            if (Mathf.Abs(rate - lastRate) > 0.05f)
            {
                var emission = trail.emission;
                emission.rateOverTime = rate;
                lastRate = rate;
            }
        }

#if UNITY_EDITOR
        /// <summary>Editor-time sizing: the glow disc and the emission rectangle cover the object's footprint.</summary>
        public void ApplyFootprint(Vector2 size)
        {
            if (halo != null) halo.transform.localScale = new Vector3(size.x, size.y, 1f);
            if (trail == null) return;
            var shape = trail.shape;
            shape.scale = new Vector3(size.x * 0.85f, size.y * 0.85f, 1f);
        }
#endif
    }
}

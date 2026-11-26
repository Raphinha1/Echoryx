using TMPro;
using UnityEngine;

namespace Echoryx.Flow
{
    /// <summary>Fades the game title in and, when the glow material exists, lets its glow breathe.</summary>
    public sealed class TitleEffect : MonoBehaviour
    {
        public const float FadeSeconds = 1.4f;

        private TMP_Text label;
        private Material glow; // private copy, so the asset is never modified
        private float time;

        private void Awake()
        {
            label = GetComponent<TMP_Text>();
            label.alpha = 0f;
        }

        private void Start()
        {
            var shared = UiFonts.TitleGlow;
            if (shared == null || label.font == null || shared.mainTexture != label.font.atlasTexture) return;
            glow = new Material(shared);
            label.fontSharedMaterial = glow;
            label.UpdateMeshPadding();
        }

        private void Update()
        {
            time += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            label.alpha = Mathf.SmoothStep(0f, 1f, time / FadeSeconds);
            if (glow != null)
                glow.SetFloat(ShaderUtilities.ID_GlowPower, 0.35f + 0.15f * Mathf.Sin(time * 1.6f));
        }

        private void OnDestroy()
        {
            if (glow != null) Destroy(glow);
        }
    }
}

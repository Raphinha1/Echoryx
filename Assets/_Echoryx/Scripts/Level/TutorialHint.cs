using UnityEngine;

namespace Echoryx.Level
{
    /// <summary>Shows a short message while the player stands inside its volume.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class TutorialHint : MonoBehaviour
    {
        [SerializeField, TextArea] private string message;
        [SerializeField] private TutorialHintDisplay display;
        private BoxCollider volume;
        private bool inside;

        public string Message => message;

        private void Awake() => volume = GetComponent<BoxCollider>();

        private void LateUpdate()
        {
            if (display == null) return;
            var now = PlayerProbe.PlayerInside(volume);
            if (now == inside) return;
            inside = now;
            if (inside) display.Show(this, message);
            else display.Hide(this);
        }

        private void OnDisable()
        {
            if (inside && display != null) display.Hide(this);
            inside = false;
        }

        private void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null) return;
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.6f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}

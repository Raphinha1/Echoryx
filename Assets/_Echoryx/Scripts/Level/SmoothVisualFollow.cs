using UnityEngine;

namespace Echoryx.Level
{
    /// <summary>
    /// Cosmetic follower: eases a visual toward a gameplay transform (e.g. a door panel that moves
    /// linearly). It never moves the source, so colliders and logic are untouched.
    /// </summary>
    [DefaultExecutionOrder(150)]
    public sealed class SmoothVisualFollow : MonoBehaviour
    {
        [SerializeField] private Transform source;
        [SerializeField] private Vector3 offset;
        [SerializeField, Min(0.001f)] private float smoothTime = 0.12f;
        private Vector3 velocity;

        private void OnEnable() => Snap();

        public void Snap()
        {
            velocity = Vector3.zero;
            if (source != null) transform.position = Target();
        }

        // World-space offset: the source may be non-uniformly scaled (a cube collider), which must not scale it.
        private Vector3 Target() => source.position + source.rotation * offset;

        private void LateUpdate()
        {
            if (source == null) return;
            transform.position = Vector3.SmoothDamp(transform.position, Target(), ref velocity, smoothTime);
        }
    }
}

using UnityEngine;

namespace Echoryx.Level
{
    /// <summary>Level exit: completes the level once when the player enters the volume.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class GoalZone : MonoBehaviour
    {
        [SerializeField] private LevelStats stats;
        private BoxCollider volume;

        private void Awake() => volume = GetComponent<BoxCollider>();

        private void LateUpdate()
        {
            if (stats == null || stats.IsCompleted) return;
            if (PlayerProbe.PlayerInside(volume)) stats.Complete();
        }

        private void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null) return;
            Gizmos.color = Color.green;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}

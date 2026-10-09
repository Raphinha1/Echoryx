using UnityEngine;

namespace Echoryx.Game
{
    /// <summary>Detects player entry spatially; Echo and physics trigger callbacks cannot activate it.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CheckpointTrigger : MonoBehaviour
    {
        [SerializeField] private CheckpointManager manager;
        [SerializeField] private RoomResetScope room;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Renderer indicator;
        private BoxCollider volume;
        private bool wasInside;
        private MaterialPropertyBlock properties;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public CheckpointManager Manager => manager;
        public RoomResetScope Room => room;
        public Transform SpawnPoint => spawnPoint;
        public bool IsActive { get; private set; }

        private void Awake() => volume = GetComponent<BoxCollider>();
        private void LateUpdate() => CheckPlayerEntry();

        public void CheckPlayerEntry()
        {
            if (manager == null || manager.Player == null) return;
            if (volume == null) volume = GetComponent<BoxCollider>();
            var inside = PlayerVolume.Contains(volume, manager.Player.transform.position);
            if (inside && !wasInside) manager.ActivateCheckpoint(this);
            wasInside = inside;
        }

        public void SetActive(bool value)
        {
            IsActive = value;
            if (indicator == null) return;
            if (properties == null) properties = new MaterialPropertyBlock();
            indicator.GetPropertyBlock(properties);
            properties.SetColor(BaseColor, value ? Color.green : new Color(1f, 0.65f, 0.15f));
            indicator.SetPropertyBlock(properties);
        }

        private void OnDisable() => wasInside = false;
    }

    internal static class PlayerVolume
    {
        public static bool Contains(BoxCollider volume, Vector3 position)
        {
            if (volume == null || !volume.enabled || !volume.gameObject.activeInHierarchy) return false;
            var local = volume.transform.InverseTransformPoint(position) - volume.center;
            var half = volume.size * 0.5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y
                && Mathf.Abs(local.z) <= half.z;
        }
    }
}

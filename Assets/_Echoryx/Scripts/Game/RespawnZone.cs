using UnityEngine;

namespace Echoryx.Game
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class RespawnZone : MonoBehaviour
    {
        [SerializeField] private CheckpointManager manager;
        private BoxCollider volume;

        private void Awake() => volume = GetComponent<BoxCollider>();
        private void LateUpdate() => CheckPlayer();

        public void CheckPlayer()
        {
            if (manager == null || manager.Player == null) return;
            if (volume == null) volume = GetComponent<BoxCollider>();
            if (PlayerVolume.Contains(volume, manager.Player.transform.position))
                manager.RequestRespawn();
        }
    }
}

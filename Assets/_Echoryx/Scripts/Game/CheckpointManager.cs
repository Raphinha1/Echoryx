using Echoryx.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace Echoryx.Game
{
    /// <summary>Owns one active checkpoint and performs queued resets after gameplay updates.</summary>
    [DefaultExecutionOrder(1000)]
    public sealed class CheckpointManager : MonoBehaviour
    {
        [SerializeField] private PlayerMotor player;
        [SerializeField] private Transform initialSpawn;
        [SerializeField] private RoomResetScope initialRoom;
        [SerializeField] private RewindAttemptRelease rewindRelease;
        [SerializeField] private EchoAttemptReset echoReset;
        [SerializeField] private PlayerCameraOrbit cameraOrbit;
        [SerializeField] private CinemachineCamera gameplayCamera;

        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private RoomResetScope room;
        private bool initialized;
        private bool pending;
        private bool resetting;

        public PlayerMotor Player => player;
        public CheckpointTrigger ActiveCheckpoint { get; private set; }
        public RoomResetScope ActiveRoom => room;

        private void Awake()
        {
            if (player == null || initialSpawn == null || initialRoom == null)
            {
                Debug.LogError("CheckpointManager needs Player, Initial Spawn and Initial Room.", this);
                enabled = false;
                return;
            }
            spawnPosition = initialSpawn.position;
            spawnRotation = initialSpawn.rotation;
            room = initialRoom;
            initialized = true;
        }

        public bool ActivateCheckpoint(CheckpointTrigger checkpoint)
        {
            if (!initialized || resetting || pending || checkpoint == null || !checkpoint.isActiveAndEnabled
                || checkpoint.Manager != this || checkpoint.SpawnPoint == null || checkpoint.Room == null
                || checkpoint == ActiveCheckpoint) return false;

            ActiveCheckpoint?.SetActive(false);
            ActiveCheckpoint = checkpoint;
            checkpoint.SetActive(true);
            spawnPosition = checkpoint.SpawnPoint.position;
            spawnRotation = checkpoint.SpawnPoint.rotation;
            room = checkpoint.Room;
            return true;
        }

        public void RequestRespawn()
        {
            if (!initialized || resetting || pending || player == null) return;
            pending = true;
            ReleaseAttempt();
        }

        private void LateUpdate()
        {
            if (pending) RespawnNow();
        }

        public bool RespawnNow()
        {
            if (!initialized || resetting || player == null) return false;
            var alreadyReleased = pending;
            pending = false;
            resetting = true;
            try
            {
                if (!alreadyReleased) ReleaseAttempt();
                room?.ResetMechanisms();
                player.Respawn(spawnPosition, spawnRotation);
                if (cameraOrbit != null)
                {
                    var previous = cameraOrbit.transform.position;
                    cameraOrbit.SnapToAnchor();
                    if (gameplayCamera != null)
                    {
                        gameplayCamera.OnTargetObjectWarped(cameraOrbit.transform,
                            cameraOrbit.transform.position - previous);
                        gameplayCamera.PreviousStateIsValid = false;
                    }
                }
                return true;
            }
            finally { resetting = false; }
        }

        private void ReleaseAttempt()
        {
            player.GetComponent<PlayerInputReader>()?.ResetInputState();
            echoReset?.ReleaseForReset();
            rewindRelease?.ReleaseForReset();
            room?.ReleaseInteractions();
        }
    }
}

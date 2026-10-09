using Echoryx.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoryx.Game
{
    [DefaultExecutionOrder(-200)]
    public sealed class RestartInput : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private CheckpointManager manager;
        [SerializeField] private PlayerInputReader input;
        private InputAction restart;

        private void OnEnable()
        {
            restart = actions?.FindAction("Player/Restart");
            if (restart == null || manager == null || input == null)
            {
                Debug.LogError("RestartInput needs Player/Restart, Manager and Player Input Reader.", this);
                enabled = false;
                return;
            }
            restart.Enable();
        }

        private void OnDisable() => restart?.Disable();

        private void Update()
        {
            if (input.CanReadAction(restart) && restart.WasPressedThisFrame())
                manager.RequestRespawn();
        }
    }
}

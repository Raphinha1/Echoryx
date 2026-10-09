using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoryx.Echo
{
    [DefaultExecutionOrder(40)]
    [RequireComponent(typeof(EchoPlayback))]
    public sealed class EchoPlaybackInput : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;

        private InputAction playAction;
        private EchoPlayback playback;

        private void Awake()
        {
            playback = GetComponent<EchoPlayback>();
        }

        private void OnEnable()
        {
            playAction = actions?.FindAction("Player/EchoPlay");
            if (playAction == null)
            {
                Debug.LogError("Assign an Input Action Asset with Player/EchoPlay.", this);
                enabled = false;
                return;
            }
            playAction.Enable();
        }

        private void OnDisable()
        {
            playAction?.Disable();
        }

        private void Update()
        {
            if (playAction.WasPressedThisFrame())
                playback.TryStart();
        }
    }
}

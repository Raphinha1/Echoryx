using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoryx.Echo
{
    /// <summary>Maps the existing Player action map's EchoRecord action to the recorder.</summary>
    [DefaultExecutionOrder(40)]
    [RequireComponent(typeof(EchoRecorder))]
    public sealed class EchoRecordInput : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;

        private InputAction recordAction;
        private EchoRecorder recorder;

        private void Awake()
        {
            recorder = GetComponent<EchoRecorder>();
        }

        private void OnEnable()
        {
            recordAction = actions?.FindAction("Player/EchoRecord");
            if (recordAction == null)
            {
                Debug.LogError("Assign an Input Action Asset with Player/EchoRecord.", this);
                enabled = false;
                return;
            }
            recordAction.Enable();
        }

        private void OnDisable()
        {
            recordAction?.Disable();
        }

        private void Update()
        {
            if (recordAction.WasPressedThisFrame())
                recorder.Toggle();
        }
    }
}

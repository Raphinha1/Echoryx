using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoryx.Player
{
    /// <summary>Owns the Player action map. Gameplay code only reads its current state.</summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;

        private InputActionMap playerMap;
        private InputAction move;
        private InputAction look;
        private InputAction jump;
        private readonly InputReleaseGate releaseGate = new InputReleaseGate();

        public Vector2 Move => CanReadAction(move) ? move.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 Look => CanReadAction(look) ? look.ReadValue<Vector2>() : Vector2.zero;
        public bool JumpPressed => CanReadAction(jump) && jump.WasPressedThisFrame();
        public bool LookIsPointer => look?.activeControl?.device is Pointer;
        public bool ReleaseCursorPressed => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

        public bool CanReadAction(InputAction action) => releaseGate.CanRead(action, Time.frameCount);

        public void ResetInputState() => releaseGate.Reset(playerMap, Time.frameCount);

        private void OnEnable()
        {
            if (actions == null)
            {
                Debug.LogError("Assign an Input Action Asset to PlayerInputReader.", this);
                enabled = false;
                return;
            }

            playerMap = actions.FindActionMap("Player");
            move = playerMap?.FindAction("Move");
            look = playerMap?.FindAction("Look");
            jump = playerMap?.FindAction("Jump");
            if (move == null || look == null || jump == null)
            {
                Debug.LogError("The input asset needs Player/Move, Player/Look and Player/Jump.", this);
                enabled = false;
                return;
            }

            playerMap.Enable();
        }

        private void OnDisable()
        {
            playerMap?.Disable();
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoryx.Player
{
    /// <summary>Rotates a separate Cinemachine follow target around the player.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class PlayerCameraOrbit : MonoBehaviour
    {
        [SerializeField] private Transform anchor;
        [SerializeField] private PlayerInputReader input;
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.15f;
        [SerializeField, Min(0f)] private float stickSensitivity = 120f;
        [SerializeField] private bool invertY;
        [SerializeField, Range(-89f, 89f)] private float minPitch = -55f;
        [SerializeField, Range(-89f, 89f)] private float maxPitch = 70f;
        [SerializeField] private bool lockCursorOnPlay = true;

        private float yaw;
        private float pitch = 12f;

        public void SetTargets(Transform followAnchor, PlayerInputReader inputReader)
        {
            anchor = followAnchor;
            input = inputReader;
        }

        public void SnapToAnchor()
        {
            if (anchor != null) transform.position = anchor.position;
        }

        private void OnEnable()
        {
            if (Application.isPlaying && lockCursorOnPlay)
                Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnDisable()
        {
            if (Application.isPlaying && lockCursorOnPlay)
                Cursor.lockState = CursorLockMode.None;
        }

        private void Update()
        {
            if (anchor == null || input == null) return;
            if (input.ReleaseCursorPressed)
                Cursor.lockState = CursorLockMode.None;
            if (lockCursorOnPlay && Cursor.lockState != CursorLockMode.Locked
                && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                Cursor.lockState = CursorLockMode.Locked;

            var look = input.Look;
            var amount = input.LookIsPointer ? mouseSensitivity : stickSensitivity * Time.deltaTime;
            if (!input.LookIsPointer || !lockCursorOnPlay || Cursor.lockState == CursorLockMode.Locked)
            {
                yaw += look.x * amount;
                pitch = Mathf.Clamp(pitch + look.y * amount * (invertY ? 1f : -1f), minPitch, maxPitch);
            }

            transform.SetPositionAndRotation(anchor.position, Quaternion.Euler(pitch, yaw, 0f));
        }

        private void OnValidate()
        {
            maxPitch = Mathf.Max(minPitch, maxPitch);
        }
    }
}

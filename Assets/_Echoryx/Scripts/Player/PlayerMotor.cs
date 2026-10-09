using UnityEngine;

namespace Echoryx.Player
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputReader))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private Transform cameraReference;
        [SerializeField, Min(0f)] private float moveSpeed = 6f;
        [SerializeField, Min(0f)] private float acceleration = 20f;
        [SerializeField, Min(0f)] private float deceleration = 25f;
        [SerializeField, Range(0f, 1f)] private float airControl = 0.55f;
        [SerializeField, Min(0f)] private float turnSpeed = 720f;

        [Header("Jump")]
        [SerializeField, Min(0f)] private float jumpHeight = 1.65f;
        [SerializeField] private float gravity = -24f;
        [SerializeField, Min(0f)] private float coyoteTime = 0.12f;
        [SerializeField, Min(0f)] private float jumpBuffer = 0.12f;
        [SerializeField, Min(0f)] private float groundedPull = 2f;

        private CharacterController controller;
        private PlayerInputReader input;
        private PlayerPlatformCarrier platformCarrier;
        private readonly JumpWindow jumpWindow = new JumpWindow();
        private Vector3 horizontalVelocity;
        private float verticalVelocity;

        public void SetCameraReference(Transform value) => cameraReference = value;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputReader>();
            platformCarrier = GetComponent<PlayerPlatformCarrier>();
        }

        private void Update()
        {
            var time = Time.time;
            var grounded = controller.isGrounded;
            jumpWindow.SetGrounded(grounded, time);
            if (input.JumpPressed) jumpWindow.Press(time);

            var moveInput = Vector2.ClampMagnitude(input.Move, 1f);
            var basis = cameraReference != null ? cameraReference : Camera.main?.transform;
            var forward = basis != null ? basis.forward : Vector3.forward;
            var right = basis != null ? basis.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            var desired = Vector3.ClampMagnitude(forward * moveInput.y + right * moveInput.x, 1f);
            var targetVelocity = desired * moveSpeed;
            var rate = (moveInput.sqrMagnitude > 0.0001f ? acceleration : deceleration)
                * (grounded ? 1f : airControl);
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, rate * Time.deltaTime);

            if (grounded && verticalVelocity < 0f)
                verticalVelocity = -groundedPull;
            if (jumpWindow.TryConsume(time, coyoteTime, jumpBuffer))
                verticalVelocity = Mathf.Sqrt(2f * Mathf.Max(0f, jumpHeight) * Mathf.Abs(gravity));
            verticalVelocity += gravity * Time.deltaTime;

            var displacement = (horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime
                + (platformCarrier != null ? platformCarrier.ConsumeMotion() : Vector3.zero);
            var collision = controller.Move(displacement);
            if ((collision & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
                verticalVelocity = 0f;
            if ((collision & CollisionFlags.Below) != 0 && verticalVelocity < 0f)
                verticalVelocity = -groundedPull;

            if (desired.sqrMagnitude > 0.0001f)
            {
                var facing = Quaternion.LookRotation(desired);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, facing, turnSpeed * Time.deltaTime);
            }
        }

        private void OnValidate()
        {
            gravity = Mathf.Min(-0.01f, gravity);
        }
    }
}

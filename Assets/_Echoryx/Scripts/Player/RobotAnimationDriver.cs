using UnityEngine;

namespace Echoryx.Player
{
    /// <summary>
    /// Read-only bridge from real movement to the locomotion Animator (Speed, Grounded).
    /// Player: PlayerMotor.PlanarSpeed, which is the CharacterController's resolved displacement
    /// minus platform transport and wall-blocked motion, so it does not animate running against a
    /// wall or when carried by a platform. Echo (no motor): speed from the root's own displacement.
    /// It never moves or rotates anything.
    /// </summary>
    [DefaultExecutionOrder(20)]
    public sealed class RobotAnimationDriver : MonoBehaviour
    {
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private CharacterController controller;
        [SerializeField] private Animator animator;
        [SerializeField, Min(0f)] private float maxSpeed = 6f;
        [SerializeField, Min(0f)] private float speedDampTime = 0.08f;
        [SerializeField, Min(0f)] private float stopThreshold = 0.15f;
        [SerializeField, Min(0f)] private float groundedGrace = 0.08f;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");

        private Transform trackedRoot;
        private Vector3 previousRootPosition;
        private float speedVelocity;
        private float airborneTime;

        public float Speed { get; private set; }
        public bool Grounded { get; private set; } = true;

        private void Awake()
        {
            if (motor == null) motor = GetComponentInParent<PlayerMotor>();
            if (controller == null) controller = GetComponentInParent<CharacterController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            trackedRoot = motor != null ? motor.transform : transform.parent != null ? transform.parent : transform;
            previousRootPosition = trackedRoot.position;
        }

        private void OnEnable()
        {
            if (trackedRoot != null) previousRootPosition = trackedRoot.position;
            Speed = 0f;
            speedVelocity = 0f;
            airborneTime = 0f;
        }

        public static float Smooth(float current, float target, ref float velocity, float dampTime, float deltaTime) =>
            dampTime <= 0f ? target : Mathf.SmoothDamp(current, target, ref velocity, dampTime, Mathf.Infinity, deltaTime);

        private void Update()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            var dt = Time.deltaTime;
            var target = SampleSpeed(dt);
            target = target < stopThreshold ? 0f : Mathf.Min(target, maxSpeed);
            Speed = Smooth(Speed, target, ref speedVelocity, speedDampTime, dt);
            if (Speed < 0.01f && target == 0f) Speed = 0f;

            var grounded = controller == null || controller.isGrounded;
            airborneTime = grounded ? 0f : airborneTime + dt;
            // Brief grace avoids a jump pose on step-offs and one-frame isGrounded drops.
            Grounded = airborneTime <= groundedGrace;
            animator.SetFloat(SpeedHash, Speed);
            animator.SetBool(GroundedHash, Grounded);
        }

        private float SampleSpeed(float dt)
        {
            if (motor != null)
                return motor.isActiveAndEnabled && motor.MotionSampleFrame == Time.frameCount ? motor.PlanarSpeed : 0f;
            var position = trackedRoot.position;
            var delta = position - previousRootPosition;
            previousRootPosition = position;
            delta.y = 0f;
            return dt > 0f ? delta.magnitude / dt : 0f;
        }
    }
}

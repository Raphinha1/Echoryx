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
        [Header("Speed matching (no foot sliding)")]
        [SerializeField] private bool speedMatching = true;
        [Tooltip("Ground speed (m/s) the Walk clip covers at playback rate 1, including the visual scale.")]
        [SerializeField, Min(0.01f)] private float walkNominalSpeed = 2f;
        [Tooltip("Ground speed (m/s) the Run clip covers at playback rate 1, including the visual scale.")]
        [SerializeField, Min(0.01f)] private float runNominalSpeed = 3.9f;
        [SerializeField, Min(0.1f)] private float minPlaybackRate = 0.6f;
        [SerializeField, Min(0.1f)] private float maxPlaybackRate = 1.6f;
        [Header("Jump takeoff")]
        [Tooltip("Upward speed above which an airborne controller counts as a jump takeoff, with no grace delay.")]
        [SerializeField, Min(0f)] private float takeoffVelocity = 0.5f;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");

        private Transform trackedRoot;
        private Vector3 previousRootPosition;
        private float speedVelocity;
        private float airborneTime;

        public float Speed { get; private set; }
        public float PlaybackRate { get; private set; } = 1f;
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

        /// <summary>
        /// Playback rate that makes the feet cover the ground at the real speed: real speed divided by the
        /// nominal stride speed of the clip(s) currently blended (Walk at the walk threshold, Run at the
        /// run threshold, linear in between, like the 1D blend tree).
        /// </summary>
        public static float SpeedMatchRate(float speed, float walkNominal, float runNominal, float runThreshold,
            float minRate, float maxRate)
        {
            if (speed <= 0f) return 1f;
            var t = Mathf.Clamp01(Mathf.InverseLerp(walkNominal, runThreshold, speed));
            var nominal = Mathf.Lerp(walkNominal, runNominal, t);
            return Mathf.Clamp(speed / Mathf.Max(0.01f, nominal), minRate, maxRate);
        }

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
            // A jump takeoff (leaving the ground while moving up) is airborne at once: PlayerMotor accepted the
            // jump earlier in this same frame (order 0), so Jump_Start starts in that frame. Step-ups keep
            // isGrounded true and walk-offs have no upward velocity; both keep the short grace period.
            if (!grounded && controller.velocity.y > takeoffVelocity) airborneTime = Mathf.Max(airborneTime, groundedGrace + dt);
            Grounded = airborneTime <= groundedGrace;
            animator.SetFloat(SpeedHash, Speed);
            animator.SetBool(GroundedHash, Grounded);

            var rate = 1f;
            if (speedMatching && Grounded && Speed > stopThreshold
                && animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion") && !animator.IsInTransition(0))
                rate = SpeedMatchRate(Speed, walkNominalSpeed, runNominalSpeed, maxSpeed, minPlaybackRate, maxPlaybackRate);
            PlaybackRate = rate;
            animator.speed = rate;
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

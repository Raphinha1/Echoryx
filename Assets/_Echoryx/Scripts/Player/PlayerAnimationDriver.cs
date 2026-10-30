using UnityEngine;

namespace Echoryx.Player
{
    /// <summary>Ground presentation; no input, movement, platform or room authority.</summary>
    [DefaultExecutionOrder(20)]
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private Animator animator;
        [SerializeField, Min(0.01f)] private float runReferenceSpeed = 4.3729978f;

        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int LocomotionRate = Animator.StringToHash("LocomotionRate");

        public void SetTargets(PlayerMotor movement, Animator presentation)
        {
            motor = movement;
            animator = presentation;
        }

        public static float Cadence(float speed, float runSpeed) =>
            Mathf.Max(1f, Mathf.Max(0f, speed) / Mathf.Max(0.01f, runSpeed));

        private void Update()
        {
            if (motor == null || animator == null || animator.runtimeAnimatorController == null) return;
            var speed = motor.isActiveAndEnabled && motor.MotionSampleFrame == Time.frameCount
                ? motor.PlanarSpeed : 0f;
            // Normal Animator evaluation follows Update. Order 20 reads the result of
            // PlayerMotor (0) this frame, not last frame's input or root displacement.
            animator.SetFloat(Speed, speed);
            animator.SetFloat(LocomotionRate, Cadence(speed, runReferenceSpeed));
        }
    }
}

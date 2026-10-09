using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Echoryx.Game.VisualLabs
{
    /// <summary>Isolated lab: exactly one source drives the Animator at a time.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Animator))]
    public sealed class LocomotionAnimationPreview : MonoBehaviour
    {
        public enum IndividualClip { None, IdleLoop, Walk, Run, Jump, Fall, Land }
        [SerializeField] private Animator animator;
        [SerializeField] private bool automaticDemo = true;
        [SerializeField, Range(0f, 6f)] private float speed;
        [SerializeField] private bool grounded = true;
        [SerializeField] private float verticalSpeed;
        [SerializeField, Min(0.1f)] private float landingPreviewSeconds = 3f;
        [Header("Individual clip diagnosis (bypasses Animator Controller)")]
        [SerializeField] private IndividualClip individualClip;
        [Header("Inspection speed (automatic demo or individual clip)")]
        [SerializeField, Range(0.25f, 2f)] private float playbackRate = 1f;
        [SerializeField] private bool pauseClip;
        [SerializeField, Range(0f, 1f)] private float normalizedClipTime;

        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int GroundedId = Animator.StringToHash("Grounded");
        private static readonly int VerticalSpeedId = Animator.StringToHash("VerticalSpeed");
        private RuntimeAnimatorController controller;
        private AnimationClip[] clips;
        private PlayableGraph graph;
        private AnimationClipPlayable playable;
        private IndividualClip playingClip;
        private float elapsed, clipTime;
        private string phase = "Idle";

        public Animator PreviewAnimator => animator;
        public string DemoPhase => phase;
        public bool IsIndividualPreview => graph.IsValid();
        public float WalkPreviewSpeed => Clip("Walk").averageSpeed.magnitude;
        public float RunPreviewSpeed => Clip("Run").averageSpeed.magnitude;
        public float JumpPreviewSeconds => Clip("Jump").length;
        public void SetPlaybackRate(float rate) => playbackRate = Mathf.Clamp(rate, 0.25f, 2f);

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            animator.applyRootMotion = false;
            controller = animator.runtimeAnimatorController;
            clips = controller.animationClips.Distinct().ToArray();
            ApplyParameters(false);
        }

        private AnimationClip Clip(string name) => clips.Single(c => c.name == name);

        public void SetPreviewParameters(float horizontalSpeed, bool isGrounded, float ySpeed)
        {
            automaticDemo = false;
            individualClip = IndividualClip.None;
            EndIndividualPreview();
            speed = Mathf.Clamp(horizontalSpeed, 0f, 6f);
            grounded = isGrounded;
            verticalSpeed = ySpeed;
        }

        public void SetAutomaticDemo(bool enabled)
        {
            individualClip = IndividualClip.None;
            EndIndividualPreview();
            automaticDemo = enabled;
            elapsed = 0f;
        }

        public void SetIndividualClip(IndividualClip clip)
        {
            automaticDemo = false;
            individualClip = clip;
            normalizedClipTime = 0f;
            clipTime = 0f;
        }

        private void Update()
        {
            if (individualClip != IndividualClip.None)
            {
                // Selecting a clip explicitly gives it sole control, including in Inspector.
                automaticDemo = false;
                UpdateIndividualPreview();
                return;
            }
            EndIndividualPreview();
            animator.speed = automaticDemo ? playbackRate : 1f;
            if (automaticDemo)
            {
                var apexTime = 8f + JumpPreviewSeconds;
                var contactTime = apexTime + 1f;
                var settleTime = Mathf.Max(landingPreviewSeconds, Clip("Land").length + 0.3f);
                elapsed = (elapsed + Time.deltaTime * playbackRate) % (contactTime + settleTime);
                grounded = elapsed < 8f || elapsed >= contactTime;
                speed = elapsed < 2f || elapsed >= 8f ? 0f : elapsed < 5f ? WalkPreviewSpeed : RunPreviewSpeed;
                verticalSpeed = grounded ? 0f : elapsed < apexTime ? 4f : -4f;
                phase = elapsed < 2f ? "Idle" : elapsed < 5f ? "Walk" : elapsed < 8f ? "Run"
                    : elapsed < apexTime ? "Jump" : elapsed < contactTime ? "Fall" : "Land";
            }
            ApplyParameters(true);
        }

        private void UpdateIndividualPreview()
        {
            var clip = Clip(individualClip.ToString());
            if (!graph.IsValid() || playingClip != individualClip)
            {
                EndIndividualPreview();
                animator.runtimeAnimatorController = null;
                animator.speed = 1f; // The manual Playable clock already applies playbackRate.
                graph = PlayableGraph.Create("S05-B3 individual " + clip.name);
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                playable = AnimationClipPlayable.Create(graph, clip);
                playable.SetApplyFootIK(individualClip == IndividualClip.IdleLoop || individualClip == IndividualClip.Walk
                    || individualClip == IndividualClip.Run || individualClip == IndividualClip.Land);
                AnimationPlayableOutput.Create(graph, "Lab pose only", animator).SetSourcePlayable(playable);
                graph.Play();
                playingClip = individualClip;
                clipTime = 0f;
            }
            if (pauseClip) clipTime = normalizedClipTime * clip.length;
            else
            {
                clipTime += Time.deltaTime * playbackRate;
                clipTime = clip.isLooping ? Mathf.Repeat(clipTime, clip.length) : Mathf.Min(clipTime, clip.length - 0.0001f);
                normalizedClipTime = clipTime / clip.length;
            }
            playable.SetTime(clipTime);
            graph.Evaluate(0f);
            phase = clip.name;
        }

        private void EndIndividualPreview()
        {
            if (!graph.IsValid()) return;
            graph.Destroy();
            animator.runtimeAnimatorController = controller;
            animator.Rebind();
            animator.applyRootMotion = false;
            animator.speed = 1f;
            playingClip = IndividualClip.None;
        }

        private void OnDisable()
        {
            EndIndividualPreview();
            if (animator != null) animator.speed = 1f;
        }
        private void OnDestroy() => EndIndividualPreview();

        private void ApplyParameters(bool dampSpeed)
        {
            if (animator == null) return;
            if (dampSpeed) animator.SetFloat(SpeedId, speed, 0.12f, Time.deltaTime);
            else animator.SetFloat(SpeedId, speed);
            animator.SetBool(GroundedId, grounded);
            animator.SetFloat(VerticalSpeedId, verticalSpeed);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(18f, 18f, 760f, 110f), GUI.skin.box);
            GUILayout.Label("S05-B3 — " + (IsIndividualPreview ? "Individual clip: " + phase : automaticDemo ? "Automatic: " + phase : "Manual parameters"));
            GUILayout.Label(IsIndividualPreview ? $"Clip {normalizedClipTime:P0} | Rate {playbackRate:F2} | Controller bypassed"
                : $"Speed {speed:F2} | Grounded {grounded} | VerticalSpeed {verticalSpeed:F1} | Demo rate {(automaticDemo ? playbackRate : 1f):F2}");
            GUILayout.Label($"Walk {WalkPreviewSpeed:F2} / Run {RunPreviewSpeed:F2}. Root Motion OFF; no physical jump displacement.");
            GUILayout.EndArea();
        }
    }
}

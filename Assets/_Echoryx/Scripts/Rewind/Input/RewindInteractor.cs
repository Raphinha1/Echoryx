using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoryx.Rewind
{
    /// <summary>Camera aiming, RMB input and a small debug reticle; no player motion logic.</summary>
    [DefaultExecutionOrder(110)]
    public sealed class RewindInteractor : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private Camera selectionCamera;
        [SerializeField, Min(0.1f)] private float maximumDistance = 12f;
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField] private bool showReticle = true;

        private readonly RaycastHit[] hitBuffer = new RaycastHit[32];
        private readonly RewindSelectionSession session = new RewindSelectionSession();
        private InputAction rewindAction;
        private ReticleState reticle;

        private enum ReticleState { None, Insufficient, Ready, Rewinding }

        private void OnEnable()
        {
            rewindAction = actions?.FindAction("Player/Rewind");
            if (rewindAction == null)
            {
                Debug.LogError("Assign an Input Action Asset with Player/Rewind.", this);
                enabled = false;
                return;
            }
            rewindAction.Enable();
        }

        private void OnDisable()
        {
            session.Release();
            rewindAction?.Disable();
            reticle = ReticleState.None;
        }

        private void LateUpdate()
        {
            if (selectionCamera == null) selectionCamera = Camera.main;
            var pressed = rewindAction != null && rewindAction.IsPressed();
            var aimed = !session.IsHolding
                ? RewindTargetResolver.Raycast(selectionCamera, transform, maximumDistance,
                    targetLayers, hitBuffer)
                : null;
            session.Update(pressed, aimed);

            var active = session.Target;
            if (active != null && active.Mode == RewindMode.Rewinding)
                reticle = ReticleState.Rewinding;
            else if (session.IsHolding)
                reticle = session.AttemptedTarget != null
                    ? ReticleState.Insufficient : ReticleState.None;
            else if (aimed == null)
                reticle = ReticleState.None;
            else
                reticle = aimed.CanRewind ? ReticleState.Ready : ReticleState.Insufficient;
        }

        private void OnGUI()
        {
            if (!showReticle || reticle == ReticleState.None) return;
            var old = GUI.color;
            GUI.color = reticle == ReticleState.Rewinding ? new Color(0.8f, 0.35f, 1f)
                : reticle == ReticleState.Ready ? new Color(0.2f, 1f, 0.8f)
                : new Color(1f, 0.72f, 0.2f);
            GUI.DrawTexture(new Rect(Screen.width * 0.5f - 3f, Screen.height * 0.5f - 3f,
                6f, 6f), Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}

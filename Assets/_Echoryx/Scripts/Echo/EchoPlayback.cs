using System;
using UnityEngine;

namespace Echoryx.Echo
{
    /// <summary>Owns the single visual actor and advances it on the recording's clock.</summary>
    [DefaultExecutionOrder(60)]
    [RequireComponent(typeof(EchoRecorder))]
    public sealed class EchoPlayback : MonoBehaviour
    {
        [SerializeField] private EchoActor actorPrefab;

        private EchoRecorder recorder;
        private EchoActor activeActor;
        private EchoRecording activeRecording;
        private double startedAt;

        public EchoActor ActiveActor => activeActor;

        private void Awake()
        {
            recorder = GetComponent<EchoRecorder>();
        }

        public bool TryStart()
        {
            if (activeActor != null || recorder == null || recorder.State != EchoState.Ready)
                return false;
            if (actorPrefab == null)
            {
                Debug.LogError("Assign an EchoActor prefab to EchoPlayback.", this);
                return false;
            }

            var recording = recorder.ReadyRecording;
            if (recording == null || recording.SampleCount == 0)
            {
                recorder.Cancel();
                return false;
            }

            var first = recording[0];
            var actor = Instantiate(actorPrefab, first.Position, first.Rotation);
            if (actor == null || !recorder.TryBeginPlayback(out activeRecording))
            {
                if (actor != null) Destroy(actor.gameObject);
                return false;
            }

            activeActor = actor;
            activeActor.ApplyPose(first);
            startedAt = Time.timeAsDouble;
            return true;
        }

        private void Update()
        {
            if (activeActor == null)
            {
                if (recorder != null && recorder.State == EchoState.Playing)
                    recorder.Cancel();
                activeRecording = null;
                return;
            }

            if (recorder == null || recorder.State != EchoState.Playing
                || activeRecording == null)
            {
                CleanupActor();
                return;
            }

            var elapsed = Math.Max(0d, Time.timeAsDouble - startedAt);
            if (!activeRecording.TryEvaluate(elapsed, out var pose))
            {
                CancelPlayback();
                return;
            }

            activeActor.ApplyPose(pose);
            if (elapsed >= activeRecording.Duration)
            {
                recorder.CompletePlayback();
                CleanupActor();
            }
        }

        private void OnDisable()
        {
            CancelPlayback();
        }

        private void CancelPlayback()
        {
            if (recorder != null && recorder.State == EchoState.Playing)
                recorder.Cancel();
            CleanupActor();
        }

        private void CleanupActor()
        {
            if (activeActor != null)
            {
                activeActor.gameObject.SetActive(false);
                Destroy(activeActor.gameObject);
            }
            activeActor = null;
            activeRecording = null;
        }
    }
}

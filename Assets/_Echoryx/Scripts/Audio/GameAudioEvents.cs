using Echoryx.Echo;
using Echoryx.Flow;
using Echoryx.Game;
using Echoryx.Interactions;
using Echoryx.Level;
using Echoryx.Player;
using Echoryx.Rewind;
using UnityEngine;

namespace Echoryx.Audio
{
    /// <summary>
    /// Turns what happens in a gameplay scene into sounds. It only reads public state and existing events.
    /// Runs after the checkpoint manager, so a respawn is already visible and its state snaps stay silent.
    /// </summary>
    [DefaultExecutionOrder(1100)]
    public sealed class GameAudioEvents : MonoBehaviour
    {
        private const float StrideMetres = 1.7f;
        private const float MinimumWalkSpeed = 0.8f;
        private const float MinimumAirSeconds = 0.15f;
        private const float TeleportMetres = 1.5f;

        private CheckpointManager checkpoints;
        private PlayerMotor player;
        private CharacterController body;
        private EchoRecorder recorder;
        private LevelStats stats;
        private LevelFlow flow;
        private PressurePlate[] plates;
        private DoorController[] doors;
        private RewindableMechanism[] mechanisms;

        private bool[] platePressed;
        private Vector3[] doorPosition;
        private bool[] doorMoving;
        private EchoState echoState;
        private CheckpointTrigger checkpoint;
        private Vector3 playerPosition;
        private bool grounded;
        private float airSeconds;
        private float stride;
        private int attempts;
        private bool ready;

        private void Start()
        {
            checkpoints = FindFirstObjectByType<CheckpointManager>();
            player = checkpoints != null ? checkpoints.Player : FindFirstObjectByType<PlayerMotor>();
            if (player != null)
            {
                body = player.GetComponent<CharacterController>();
                recorder = player.GetComponent<EchoRecorder>();
            }
            stats = FindFirstObjectByType<LevelStats>();
            if (stats != null) stats.Completed += OnCompleted;
            plates = FindObjectsByType<PressurePlate>(FindObjectsSortMode.None);
            doors = FindObjectsByType<DoorController>(FindObjectsSortMode.None);
            mechanisms = FindObjectsByType<RewindableMechanism>(FindObjectsSortMode.None);
            platePressed = new bool[plates.Length];
            doorPosition = new Vector3[doors.Length];
            doorMoving = new bool[doors.Length];
            Snapshot();
            ready = true;
        }

        private void OnDestroy()
        {
            if (stats != null) stats.Completed -= OnCompleted;
            if (AudioManager.Instance == null) return;
            AudioManager.Instance.SetRewindLoop(false);
            AudioManager.Instance.Paused = false;
        }

        private void OnCompleted() => AudioManager.Ensure().Play(SoundId.Goal);

        private void LateUpdate()
        {
            if (!ready) return;
            var audio = AudioManager.Ensure();
            if (flow == null) flow = FindFirstObjectByType<LevelFlow>();
            var paused = flow != null && flow.State == LevelFlowState.Paused;
            audio.Paused = paused;
            if (paused || Time.deltaTime <= 0f)
            {
                audio.SetRewindLoop(false);
                return;
            }

            var rewinding = false;
            foreach (var mechanism in mechanisms)
                if (mechanism != null && mechanism.Mode == RewindMode.Rewinding) rewinding = true;
            audio.SetRewindLoop(rewinding);

            // A respawn teleports and resets things: take the new state without announcing it.
            var respawned = (stats != null && stats.Attempts != attempts)
                || (player != null && Vector3.Distance(player.transform.position, playerPosition) > TeleportMetres);
            if (respawned)
            {
                Snapshot();
                return;
            }

            UpdatePlayer(audio);
            UpdateWorld(audio);
            UpdateEcho(audio);
        }

        private void UpdatePlayer(AudioManager audio)
        {
            if (player == null || body == null) return;
            playerPosition = player.transform.position;
            var now = body.isGrounded;
            if (grounded && !now && body.velocity.y > 0.5f)
            {
                audio.Play(SoundId.Jump);
                airSeconds = MinimumAirSeconds; // a real jump always gets a landing
            }
            if (!now) airSeconds += Time.deltaTime;
            else
            {
                if (!grounded && airSeconds >= MinimumAirSeconds) audio.Play(SoundId.Land);
                airSeconds = 0f;
            }
            grounded = now;

            if (!now || !player.enabled || player.PlanarSpeed < MinimumWalkSpeed)
            {
                stride = StrideMetres * 0.5f; // the first step comes soon after starting to walk
                return;
            }
            stride += player.PlanarSpeed * Time.deltaTime;
            if (stride < StrideMetres) return;
            stride -= StrideMetres;
            audio.Play(SoundId.Footstep);
        }

        private void UpdateWorld(AudioManager audio)
        {
            for (var i = 0; i < plates.Length; i++)
            {
                var pressed = plates[i] != null && plates[i].IsPressed;
                if (pressed && !platePressed[i]) audio.Play(SoundId.PressurePlate);
                platePressed[i] = pressed;
            }
            for (var i = 0; i < doors.Length; i++)
            {
                if (doors[i] == null || doors[i].Panel == null) continue;
                var position = doors[i].Panel.localPosition;
                var moving = (position - doorPosition[i]).sqrMagnitude > 1e-6f;
                if (moving && !doorMoving[i]) audio.Play(SoundId.Door);
                doorMoving[i] = moving;
                doorPosition[i] = position;
            }
            var active = checkpoints != null ? checkpoints.ActiveCheckpoint : null;
            if (active != null && active != checkpoint) audio.Play(SoundId.Checkpoint);
            checkpoint = active;
        }

        private void UpdateEcho(AudioManager audio)
        {
            if (recorder == null) return;
            var state = recorder.State;
            if (state == echoState) return;
            if (state == EchoState.Recording) audio.Play(SoundId.EchoRecord);
            else if (state == EchoState.Playing) audio.Play(SoundId.EchoPlay);
            else if (echoState == EchoState.Playing) audio.Play(SoundId.EchoEnd);
            echoState = state;
        }

        private void Snapshot()
        {
            attempts = stats != null ? stats.Attempts : 0;
            if (player != null) playerPosition = player.transform.position;
            grounded = true;
            airSeconds = 0f;
            stride = StrideMetres * 0.5f;
            for (var i = 0; i < plates.Length; i++) platePressed[i] = plates[i] != null && plates[i].IsPressed;
            for (var i = 0; i < doors.Length; i++)
            {
                if (doors[i] != null && doors[i].Panel != null) doorPosition[i] = doors[i].Panel.localPosition;
                doorMoving[i] = false;
            }
            echoState = recorder != null ? recorder.State : EchoState.Available;
            checkpoint = checkpoints != null ? checkpoints.ActiveCheckpoint : null;
        }
    }
}

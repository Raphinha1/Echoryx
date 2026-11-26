using System;
using System.Collections.Generic;
using Echoryx.Echo;
using Echoryx.Game;
using Echoryx.Level;
using Echoryx.Player;
using Echoryx.Rewind;
using UnityEngine;

namespace Echoryx.Vfx
{
    public enum VfxEvent { EchoAppear, EchoDissolve, Checkpoint, Goal, LandingDust }

    /// <summary>
    /// Visual effects of a gameplay scene. It only reads public state and LevelStats.Completed.
    /// Runs after the checkpoint manager, so respawns are already visible and trigger nothing.
    /// </summary>
    [DefaultExecutionOrder(1100)]
    public sealed class GameVfx : MonoBehaviour
    {
        public const float MinimumDustFall = 1f;
        private const float TeleportMetres = 1.5f;

        public VfxEmitter Sparks { get; private set; }
        public VfxEmitter Flashes { get; private set; }
        public VfxEmitter Rings { get; private set; }
        public VfxEmitter DustPuffs { get; private set; }
        public VfxEmitter Aura { get; private set; }
        public VfxEmitter RewindFlow { get; private set; }
        public VfxEmitter CheckpointGlow { get; private set; }
        public bool IsAuraActive { get; private set; }
        public RewindableMechanism FlowingMechanism { get; private set; }
        public CheckpointTrigger GlowingCheckpoint { get; private set; }
        public event Action<VfxEvent> Played;

        private CheckpointManager checkpoints;
        private PlayerMotor player;
        private CharacterController body;
        private EchoRecorder recorder;
        private EchoPlayback playback;
        private LevelStats stats;
        private GoalZone goal;
        private RewindableMechanism[] mechanisms;
        private Vector3[] mechanismPositions;
        private Vector3[] flowDirections;

        private EchoActor echo;
        private readonly List<Transform> echoBones = new List<Transform>();
        private readonly List<Vector3> silhouette = new List<Vector3>(); // local to the Echo root
        private Matrix4x4 echoPose;
        private Vector3 playerPosition;
        private int attempts;
        private bool grounded = true;
        private float peakHeight;
        private float auraDebt, auraGlowDebt, flowDebt, glowDebt, rewindGlowDebt, checkpointRingDebt;

        public static GameVfx Create(Transform parent)
        {
            var library = Resources.Load<VfxLibrary>(VfxLibrary.ResourceName);
            if (library == null || library.glow == null || library.dust == null || library.ring == null)
            {
                Debug.LogWarning("VFX materials missing: run Echoryx/VFX/Create Materials.");
                return null;
            }
            var go = new GameObject("Game VFX");
            if (parent != null) go.transform.SetParent(parent, false);
            var vfx = go.AddComponent<GameVfx>();
            var t = go.transform;
            const ParticleSystemRenderMode face = ParticleSystemRenderMode.Billboard;
            vfx.Sparks = new VfxEmitter(t, "Sparks", library.glow, 700, face, 1f, 0.35f);
            vfx.Flashes = new VfxEmitter(t, "Flashes", library.glow, 24, face, 0.35f, 1.25f);
            vfx.Rings = new VfxEmitter(t, "Rings", library.ring, 16, ParticleSystemRenderMode.HorizontalBillboard, 0.12f, 1f);
            vfx.DustPuffs = new VfxEmitter(t, "Dust", library.dust, 80, face, 0.45f, 1.35f, drag: 4f);
            vfx.Aura = new VfxEmitter(t, "Recording Aura", library.glow, 140, face, 1f, 0.35f);
            vfx.RewindFlow = new VfxEmitter(t, "Rewind Flow", library.glow, 220, ParticleSystemRenderMode.Stretch, 1f, 0.4f);
            vfx.CheckpointGlow = new VfxEmitter(t, "Checkpoint Glow", library.glow, 90, face, 0.7f, 1f);
            return vfx;
        }

        private void Start()
        {
            checkpoints = FindFirstObjectByType<CheckpointManager>();
            player = checkpoints != null ? checkpoints.Player : FindFirstObjectByType<PlayerMotor>();
            if (player != null)
            {
                body = player.GetComponent<CharacterController>();
                recorder = player.GetComponent<EchoRecorder>();
                playback = player.GetComponent<EchoPlayback>();
            }
            stats = FindFirstObjectByType<LevelStats>();
            if (stats != null) stats.Completed += OnCompleted;
            goal = FindFirstObjectByType<GoalZone>();
            mechanisms = FindObjectsByType<RewindableMechanism>(FindObjectsSortMode.None);
            mechanismPositions = new Vector3[mechanisms.Length];
            flowDirections = new Vector3[mechanisms.Length];
            for (var i = 0; i < mechanisms.Length; i++)
            {
                mechanismPositions[i] = mechanisms[i].transform.position;
                flowDirections[i] = -mechanisms[i].transform.forward;
            }
            Snapshot();
        }

        private void OnDestroy()
        {
            if (stats != null) stats.Completed -= OnCompleted;
        }

        private void Snapshot()
        {
            attempts = stats != null ? stats.Attempts : 0;
            if (player != null) playerPosition = player.transform.position;
            grounded = true;
            peakHeight = playerPosition.y;
        }

        private void LateUpdate()
        {
            if (mechanisms == null || Time.deltaTime <= 0f) return; // paused: particles are frozen too

            var respawned = (stats != null && stats.Attempts != attempts)
                || (player != null && Vector3.Distance(player.transform.position, playerPosition) > TeleportMetres);
            UpdateEcho(respawned);
            UpdateAura();
            UpdateRewind();
            UpdateCheckpoint();
            if (respawned) Snapshot();
            else UpdateLanding();
        }

        // ------------------------------------------------------------ Echo

        private void UpdateEcho(bool respawned)
        {
            var actor = playback != null ? playback.ActiveActor : null;
            // Compare references: a destroyed actor equals null for Unity, which would hide its disappearance.
            if (!ReferenceEquals(actor, echo))
            {
                // The old actor may already be destroyed: its silhouette was cached while it lived.
                if (!ReferenceEquals(echo, null) && !respawned) Dissolve();
                echo = actor;
                echoBones.Clear();
                if (echo != null)
                {
                    foreach (var skin in echo.GetComponentsInChildren<SkinnedMeshRenderer>())
                        foreach (var bone in skin.bones)
                            if (bone != null && !echoBones.Contains(bone)) echoBones.Add(bone);
                    CacheSilhouette();
                    Condense();
                }
            }
            else if (echo != null) CacheSilhouette();
        }

        // Points along the skeleton, which follow the animated pose without reading mesh data.
        private void CacheSilhouette()
        {
            echoPose = echo.transform.localToWorldMatrix;
            var toLocal = echo.transform.worldToLocalMatrix;
            silhouette.Clear();
            foreach (var bone in echoBones)
            {
                var point = toLocal.MultiplyPoint3x4(bone.position);
                if (!InsideBody(point)) continue; // control bones sit far from the body
                silhouette.Add(point);
                if (bone.parent == null) continue;
                var parent = toLocal.MultiplyPoint3x4(bone.parent.position);
                if (InsideBody(parent)) silhouette.Add(Vector3.Lerp(point, parent, 0.5f));
            }
            if (silhouette.Count >= 8) return;
            for (var i = 0; i < 40; i++) // fallback: a capsule the size of the robot
            {
                var circle = UnityEngine.Random.insideUnitCircle * 0.35f;
                silhouette.Add(new Vector3(circle.x, UnityEngine.Random.Range(0.1f, 1.8f), circle.y));
            }
        }

        private static bool InsideBody(Vector3 local) =>
            local.y > 0.02f && local.y < 2.2f && new Vector2(local.x, local.z).sqrMagnitude < 0.64f;

        private Vector3 SilhouettePoint() =>
            echoPose.MultiplyPoint3x4(silhouette[UnityEngine.Random.Range(0, silhouette.Count)]
                + UnityEngine.Random.insideUnitSphere * 0.07f);

        private void Condense()
        {
            for (var i = 0; i < 130; i++)
            {
                var target = SilhouettePoint();
                var offset = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(0.6f, 1.3f);
                var life = UnityEngine.Random.Range(0.32f, 0.5f);
                Sparks.Emit(target + offset, -offset / life, VfxPalette.EchoCyan, UnityEngine.Random.Range(0.05f, 0.11f), life);
            }
            Flashes.Emit(echoPose.MultiplyPoint3x4(Vector3.up), Vector3.zero, VfxPalette.EchoCore, 2.4f, 0.3f);
            Played?.Invoke(VfxEvent.EchoAppear);
        }

        private void Dissolve()
        {
            if (silhouette.Count == 0) return;
            for (var i = 0; i < 130; i++)
            {
                var velocity = UnityEngine.Random.insideUnitSphere * 0.7f + Vector3.up * UnityEngine.Random.Range(0.2f, 1.1f);
                Sparks.Emit(SilhouettePoint(), velocity, VfxPalette.EchoCyan,
                    UnityEngine.Random.Range(0.05f, 0.11f), UnityEngine.Random.Range(0.45f, 0.85f));
            }
            Flashes.Emit(echoPose.MultiplyPoint3x4(Vector3.up), Vector3.zero,
                VfxPalette.EchoCyan * new Color(1f, 1f, 1f, 0.6f), 1.8f, 0.25f);
            Played?.Invoke(VfxEvent.EchoDissolve);
        }

        private void UpdateAura()
        {
            IsAuraActive = recorder != null && recorder.State == EchoState.Recording;
            if (!IsAuraActive) { auraDebt = auraGlowDebt = 0f; return; }
            // World-space particles stay behind a moving player: aura and trail at once.
            for (auraGlowDebt += 9f * Time.deltaTime; auraGlowDebt >= 1f; auraGlowDebt -= 1f)
                Aura.Emit(player.transform.position + Vector3.up * UnityEngine.Random.Range(0.6f, 1.3f), Vector3.zero,
                    VfxPalette.EchoCyan * new Color(1f, 1f, 1f, 0.2f), UnityEngine.Random.Range(1.2f, 1.7f), 0.55f);
            for (auraDebt += 50f * Time.deltaTime; auraDebt >= 1f; auraDebt -= 1f)
            {
                var circle = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(0.35f, 0.55f);
                var position = player.transform.position + new Vector3(circle.x, UnityEngine.Random.Range(0.15f, 1.6f), circle.y);
                Aura.Emit(position, Vector3.up * 0.25f, VfxPalette.EchoCyan * new Color(1f, 1f, 1f, 0.85f),
                    UnityEngine.Random.Range(0.1f, 0.2f), 0.7f);
            }
        }

        // ------------------------------------------------------------ Rewind

        private void UpdateRewind()
        {
            FlowingMechanism = null;
            for (var i = 0; i < mechanisms.Length; i++)
            {
                var mechanism = mechanisms[i];
                if (mechanism == null) continue;
                var position = mechanism.transform.position;
                var moved = position - mechanismPositions[i];
                mechanismPositions[i] = position;
                if (mechanism.Mode != RewindMode.Rewinding) continue;
                // While rewinding it travels backwards, against its normal motion: the flow goes that way.
                // A teleport (reset) is not motion; a stalled history keeps the last direction.
                if (moved.sqrMagnitude > 1e-8f && moved.sqrMagnitude < 1f) flowDirections[i] = moved.normalized;
                FlowingMechanism = mechanism;
                Flow(mechanism, flowDirections[i]);
            }
            if (FlowingMechanism == null) { flowDebt = 0f; rewindGlowDebt = 0f; }
        }

        private void Flow(RewindableMechanism mechanism, Vector3 direction)
        {
            var area = BoundsOf(mechanism);
            for (flowDebt += 90f * Time.deltaTime; flowDebt >= 1f; flowDebt -= 1f)
            {
                var position = new Vector3(
                    UnityEngine.Random.Range(area.min.x, area.max.x),
                    area.max.y + UnityEngine.Random.Range(0.05f, 0.55f),
                    UnityEngine.Random.Range(area.min.z, area.max.z));
                var colour = UnityEngine.Random.value < 0.75f ? VfxPalette.Violet : VfxPalette.Lilac;
                RewindFlow.Emit(position - direction * 0.7f, direction * UnityEngine.Random.Range(2.8f, 4.2f), colour,
                    UnityEngine.Random.Range(0.09f, 0.17f), 0.45f);
            }
            for (rewindGlowDebt += 7f * Time.deltaTime; rewindGlowDebt >= 1f; rewindGlowDebt -= 1f)
                RewindFlow.Emit(new Vector3(area.center.x, area.max.y + 0.08f, area.center.z), direction * 2f,
                    VfxPalette.Violet * new Color(1f, 1f, 1f, 0.35f), Mathf.Max(area.size.x, area.size.z) * 0.45f, 0.4f);
        }

        private static Bounds BoundsOf(Component mechanism)
        {
            var solid = mechanism.GetComponentInChildren<Collider>();
            return solid != null ? solid.bounds : new Bounds(mechanism.transform.position, Vector3.one);
        }

        // ------------------------------------------------------------ checkpoint, goal, landing

        private void UpdateCheckpoint()
        {
            var active = checkpoints != null ? checkpoints.ActiveCheckpoint : null;
            if (active != GlowingCheckpoint)
            {
                GlowingCheckpoint = active;
                CheckpointGlow.Clear();
                glowDebt = checkpointRingDebt = 0f;
                if (active != null) CheckpointBurst(active.transform.position);
            }
            if (GlowingCheckpoint == null) return;
            var centre = GlowingCheckpoint.transform.position;
            for (glowDebt += 20f * Time.deltaTime; glowDebt >= 1f; glowDebt -= 1f)
            {
                // Soft motes along the marker (5 x 1.5 m).
                var position = centre + new Vector3(UnityEngine.Random.Range(-2.3f, 2.3f), 0.1f, UnityEngine.Random.Range(-0.6f, 0.6f));
                CheckpointGlow.Emit(position, Vector3.up * UnityEngine.Random.Range(0.3f, 0.7f),
                    UnityEngine.Random.value < 0.7f ? VfxPalette.Gold : VfxPalette.Lilac, UnityEngine.Random.Range(0.12f, 0.26f), 1.6f);
            }
            for (checkpointRingDebt += 1.6f * Time.deltaTime; checkpointRingDebt >= 1f; checkpointRingDebt -= 1f)
                for (var side = -1; side <= 1; side++) // three overlapping halos cover the long marker
                    CheckpointGlow.Emit(centre + new Vector3(side * 1.6f, 0.35f, 0f), Vector3.zero,
                        VfxPalette.Orange * new Color(1f, 1f, 1f, 0.3f), 2.1f, 1.4f);
        }

        private void CheckpointBurst(Vector3 centre)
        {
            Rings.Emit(centre + Vector3.up * 0.12f, Vector3.zero, VfxPalette.Orange, 6f, 0.7f);
            Rings.Emit(centre + Vector3.up * 0.15f, Vector3.zero, VfxPalette.Lilac, 4f, 0.9f);
            Flashes.Emit(centre + Vector3.up * 0.6f, Vector3.zero, VfxPalette.Gold * new Color(1f, 1f, 1f, 0.55f), 2.2f, 0.35f);
            for (var i = 0; i < 60; i++)
            {
                var position = centre + new Vector3(UnityEngine.Random.Range(-2.2f, 2.2f), 0.1f, UnityEngine.Random.Range(-0.65f, 0.65f));
                Sparks.Emit(position, Vector3.up * UnityEngine.Random.Range(1.1f, 2.9f),
                    i % 3 == 0 ? VfxPalette.Lilac : VfxPalette.Orange, UnityEngine.Random.Range(0.06f, 0.13f), UnityEngine.Random.Range(0.6f, 1.1f));
            }
            Played?.Invoke(VfxEvent.Checkpoint);
        }

        private void OnCompleted()
        {
            var centre = goal != null ? goal.transform.position : player != null ? player.transform.position : transform.position;
            var heart = centre + Vector3.up * 1.4f;
            Flashes.Emit(heart, Vector3.zero, VfxPalette.Gold, 4.5f, 0.5f);
            Flashes.Emit(heart, Vector3.zero, VfxPalette.Lilac * new Color(1f, 1f, 1f, 0.7f), 3f, 0.9f);
            Rings.Emit(centre + Vector3.up * 0.15f, Vector3.zero, VfxPalette.Orange, 7f, 0.8f);
            Rings.Emit(heart, Vector3.zero, VfxPalette.Lilac, 5f, 1.1f);
            for (var i = 0; i < 220; i++)
            {
                var velocity = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(1.5f, 4.8f) + Vector3.up * 1.2f;
                var colour = i % 3 == 0 ? VfxPalette.Gold : i % 3 == 1 ? VfxPalette.Orange : VfxPalette.Lilac;
                Sparks.Emit(heart, velocity, colour, UnityEngine.Random.Range(0.07f, 0.16f), UnityEngine.Random.Range(0.6f, 1.3f));
            }
            Played?.Invoke(VfxEvent.Goal);
        }

        private void UpdateLanding()
        {
            if (player == null || body == null) return;
            playerPosition = player.transform.position;
            if (!player.enabled) return;
            var now = body.isGrounded;
            if (!now)
            {
                peakHeight = grounded ? playerPosition.y : Mathf.Max(peakHeight, playerPosition.y);
            }
            else if (!grounded)
            {
                var fall = peakHeight - playerPosition.y;
                if (fall >= MinimumDustFall) Dust(playerPosition, Mathf.InverseLerp(MinimumDustFall, 3f, fall));
            }
            grounded = now;
        }

        private void Dust(Vector3 feet, float strength)
        {
            var count = Mathf.RoundToInt(Mathf.Lerp(12f, 24f, strength));
            for (var i = 0; i < count; i++)
            {
                var angle = (i + UnityEngine.Random.value) / count * Mathf.PI * 2f;
                var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var velocity = outward * UnityEngine.Random.Range(1.1f, 2.2f) * (1f + 0.5f * strength) + Vector3.up * 0.25f;
                DustPuffs.Emit(feet + outward * 0.25f + Vector3.up * 0.12f, velocity, VfxPalette.Dust,
                    UnityEngine.Random.Range(0.22f, 0.42f), UnityEngine.Random.Range(0.3f, 0.55f));
            }
            Played?.Invoke(VfxEvent.LandingDust);
        }
    }
}

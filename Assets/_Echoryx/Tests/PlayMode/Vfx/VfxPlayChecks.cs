using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Echoryx.Echo;
using Echoryx.Flow;
using Echoryx.Game;
using Echoryx.Level;
using Echoryx.Player;
using Echoryx.Rewind;
using Echoryx.Rewind.Platforms;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Echoryx.Vfx.PlayTests
{
    /// <summary>Each effect follows the public game state, reuses its pool and stays within the palette.</summary>
    public sealed class VfxPlayChecks
    {
        private GameVfx vfx;
        private CheckpointManager manager;
        private PlayerMotor player;
        private readonly List<VfxEvent> played = new List<VfxEvent>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameProgress.Storage = new MemoryProgressStorage();
            GameSettings.Storage = new MemorySettingsStorage();
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Echoryx/Scenes/Level_01_Tutorial.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("Loads the authored scene in the Editor.");
#endif
            yield return Frames(4);
            Time.captureDeltaTime = 1f / 60f;
            vfx = Object.FindObjectsByType<GameVfx>(FindObjectsSortMode.None).Single();
            manager = Object.FindFirstObjectByType<CheckpointManager>();
            player = manager.Player;
            played.Clear();
            vfx.Played += played.Add;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
            GameProgress.Storage = null;
            GameSettings.Storage = null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator EchoHasAnAuraWhileRecordingCondensesAndDissolves()
        {
            var recorder = player.GetComponent<EchoRecorder>();
            var playback = player.GetComponent<EchoPlayback>();
            var systems = SystemCount();
            Assert.That(vfx.Aura.Emitted, Is.Zero);
            recorder.Toggle();
            yield return Frames(40);
            Assert.That(vfx.IsAuraActive, Is.True);
            Assert.That(vfx.Aura.Alive, Is.GreaterThan(10));
            AssertCyan(vfx.Aura.LastColor);
            recorder.Toggle();
            yield return Frames(2);
            var auraTotal = vfx.Aura.Emitted;
            yield return Frames(50);
            Assert.That(vfx.IsAuraActive, Is.False);
            Assert.That(vfx.Aura.Emitted, Is.EqualTo(auraTotal));
            Assert.That(vfx.Aura.Alive, Is.Zero, "Short lived.");

            for (var round = 0; round < 2; round++)
            {
                played.Clear();
                Assert.That(playback.TryStart(), Is.True);
                yield return Frames(2);
                Assert.That(played, Is.EqualTo(new[] { VfxEvent.EchoAppear }));
                AssertCyan(vfx.Sparks.LastColor);
                var deadline = Time.time + 8f;
                while (playback.ActiveActor != null && Time.time < deadline) yield return null;
                yield return Frames(2);
                Assert.That(played, Is.EqualTo(new[] { VfxEvent.EchoAppear, VfxEvent.EchoDissolve }),
                    "The destroyed clone is noticed and dissolves.");
                Assert.That(vfx.Sparks.Alive, Is.GreaterThan(50));
                yield return Frames(70);
                Assert.That(vfx.Sparks.Alive, Is.Zero);
            }
            Assert.That(SystemCount(), Is.EqualTo(systems), "Pooled: nothing new is created while playing.");

            // A respawn removes the clone silently.
            Assert.That(playback.TryStart(), Is.True);
            yield return Frames(10);
            played.Clear();
            manager.RespawnNow();
            yield return Frames(5);
            Assert.That(played, Is.Empty);
        }

        [UnityTest]
        public IEnumerator RewindFlowsBackwardsOnlyWhileHeldAndFreezesWithThePause()
        {
            var platform = Object.FindFirstObjectByType<RewindMovingPlatform>();
            var forward = (platform.transform.parent.Find("Platform End").position
                - platform.transform.parent.Find("Platform Start").position).normalized;
            yield return Frames(200);
            Assert.That(vfx.RewindFlow.Emitted, Is.Zero, "Normal motion has no effect.");
            Assert.That(platform.BeginRewind(), Is.True);
            yield return Frames(30);
            Assert.That(vfx.FlowingMechanism, Is.SameAs(platform));
            Assert.That(vfx.RewindFlow.Alive, Is.GreaterThan(15));
            Assert.That(Vector3.Dot(vfx.RewindFlow.LastVelocity.normalized, forward), Is.LessThan(-0.95f),
                "Against the normal direction of travel.");
            AssertNotCyan(vfx.RewindFlow.LastColor);

            Time.timeScale = 0f;
            yield return Frames(2);
            var alive = vfx.RewindFlow.Alive;
            var emitted = vfx.RewindFlow.Emitted;
            yield return Frames(60);
            Assert.That(vfx.RewindFlow.Alive, Is.EqualTo(alive));
            Assert.That(vfx.RewindFlow.Emitted, Is.EqualTo(emitted));
            Time.timeScale = 1f;

            platform.EndRewind();
            yield return Frames(45);
            Assert.That(vfx.FlowingMechanism, Is.Null);
            Assert.That(vfx.RewindFlow.Alive, Is.Zero);
        }

        [UnityTest]
        public IEnumerator CheckpointBurstsOnceAndKeepsASoftGlowThatMovesToTheNextOne()
        {
            var first = GameObject.Find("Checkpoint End Room 1").GetComponent<CheckpointTrigger>();
            var second = GameObject.Find("Checkpoint End Room 2").GetComponent<CheckpointTrigger>();
            Assert.That(vfx.CheckpointGlow.Emitted, Is.Zero);
            manager.ActivateCheckpoint(first);
            yield return Frames(3);
            Assert.That(played, Is.EqualTo(new[] { VfxEvent.Checkpoint }));
            Assert.That(vfx.Rings.Alive, Is.EqualTo(2));
            AssertNotCyan(vfx.Sparks.LastColor);
            yield return Frames(300);
            Assert.That(played.Count, Is.EqualTo(1));
            Assert.That(vfx.Rings.Alive, Is.Zero, "The burst is short.");
            Assert.That(vfx.CheckpointGlow.Alive, Is.InRange(10, 90), "The glow stays.");
            AssertNotCyan(vfx.CheckpointGlow.LastColor);
            manager.ActivateCheckpoint(second);
            yield return Frames(3);
            Assert.That(vfx.GlowingCheckpoint, Is.SameAs(second));
            Assert.That(played.Count, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator GoalBurstsAndDustNeedsAHighEnoughFall()
        {
            // A small drop: nothing.
            player.Respawn(new Vector3(0f, 0.5f, 24f), Quaternion.identity);
            yield return Frames(60);
            Assert.That(vfx.DustPuffs.Emitted, Is.Zero);
            // A fall: one puff. The respawn teleport itself is not counted.
            player.Respawn(new Vector3(0f, 2.6f, 24f), Quaternion.identity);
            yield return Frames(70);
            Assert.That(played.Count(e => e == VfxEvent.LandingDust), Is.EqualTo(1));
            AssertNotCyan(vfx.DustPuffs.LastColor);
            yield return Frames(40);
            Assert.That(vfx.DustPuffs.Alive, Is.Zero);

            played.Clear();
            Object.FindFirstObjectByType<LevelStats>().Complete();
            yield return Frames(3);
            Assert.That(played, Is.EqualTo(new[] { VfxEvent.Goal }));
            Assert.That(vfx.Sparks.Alive, Is.GreaterThan(150));
            AssertNotCyan(vfx.Sparks.LastColor);
            yield return Frames(100);
            Assert.That(vfx.Sparks.Alive, Is.Zero);
        }

        [Test]
        public void OnlyTheEchoPaletteIsCyan()
        {
            AssertCyan(VfxPalette.EchoCyan);
            foreach (var colour in new[] { VfxPalette.Lilac, VfxPalette.Violet, VfxPalette.Orange, VfxPalette.Gold, VfxPalette.Dust })
                AssertNotCyan(colour);
        }

        private static float Hue(Color colour)
        {
            Color.RGBToHSV(colour, out var hue, out _, out _);
            return hue * 360f;
        }

        private static void AssertCyan(Color colour) => Assert.That(Hue(colour), Is.InRange(170f, 200f), colour.ToString());
        private static void AssertNotCyan(Color colour) => Assert.That(Hue(colour), Is.Not.InRange(150f, 245f), colour.ToString());
        private static int SystemCount() => Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(p => p.GetComponentInParent<GameVfx>() != null);

        private static IEnumerator Frames(int count)
        {
            for (var i = 0; i < count; i++) yield return null;
        }
    }
}

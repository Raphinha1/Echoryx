using System.Collections;
using System.Linq;
using System.Reflection;
using Echoryx.Flow;
using Echoryx.Game;
using Echoryx.Rewind;
using Echoryx.Rewind.Platforms;
using NUnit.Framework;
using TMPro;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Echoryx.Audio.PlayTests
{
    public sealed class PolishPlayChecks
    {
        private const string Menu = "Assets/_Echoryx/Scenes/MainMenu.unity";
        private const string Level = "Assets/_Echoryx/Scenes/Level_01_Tutorial.unity";

        [SetUp]
        public void SetUp() => GameProgress.Storage = new MemoryProgressStorage();

        [TearDown]
        public void TearDown()
        {
            GameProgress.Storage = null;
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator MenuMovesUsesTheFontsAndFadesIntoTheLevelAndBack()
        {
#if UNITY_EDITOR
            yield return Load(Menu);
            var scenery = SceneManager.GetActiveScene().GetRootGameObjects().Single(r => r.name == MenuMotion.SceneryRootName).transform;
            var positions = scenery.Cast<Transform>().Select(t => t.localPosition).ToArray();
            var rotations = scenery.Cast<Transform>().Select(t => t.localRotation).ToArray();
            var camera = Camera.main.transform.position;
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(scenery.Cast<Transform>().Select((t, i) => Quaternion.Angle(t.localRotation, rotations[i])).All(a => a > 0.2f), Is.True, "Everything turns.");
            // One object may be at the crest of its sine, where its height barely changes.
            Assert.That(scenery.Cast<Transform>().Select((t, i) => Vector3.Distance(t.localPosition, positions[i])).Count(d => d > 0.01f),
                Is.GreaterThanOrEqualTo(positions.Length - 1), "They float.");
            Assert.That(Camera.main.transform.position, Is.Not.EqualTo(camera));

            var menu = Object.FindFirstObjectByType<MainMenuController>();
            var title = Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Single(t => t.name == "Game Title");
            Assert.That(title.font.name, Is.EqualTo("TitleFont"));
            Assert.That(menu.PlayButton.GetComponentInChildren<TMP_Text>().font.name, Is.EqualTo("BodyFont"));
            Assert.That(menu.PlayButton.GetComponent<ButtonHoverScale>(), Is.Not.Null);
            Assert.That(menu.PlayButton.transform.localScale.x, Is.GreaterThan(1.02f), "The focused button grows.");

            var audio = AudioManager.Ensure();
            Assert.That(audio.CurrentMusic, Is.EqualTo(SoundId.MenuMusic));
            menu.Play();
            Assert.That(SceneTransition.IsLeaving, Is.True);
            Assert.That(audio.CurrentMusic, Is.Null, "The menu music starts fading out at once.");
            yield return null;
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(Menu), "No hard cut: it darkens first.");
            yield return WaitFor(Level);
            Assert.That(audio.CurrentMusic, Is.EqualTo(SoundId.LevelMusic));
            Assert.That(SceneTransition.Darkness, Is.GreaterThan(0.5f), "The level appears from black.");
            Assert.That(audio.ActiveMusicSource.volume, Is.LessThan(0.2f), "Level music fades in.");
            yield return new WaitForSecondsRealtime(3f);
            Assert.That(SceneTransition.Darkness, Is.EqualTo(0f));
            Assert.That(audio.ActiveMusicSource.volume, Is.EqualTo(audio.Library.levelMusic.volume).Within(0.01f));

            var flow = Object.FindFirstObjectByType<LevelFlow>();
            flow.Pause();
            flow.GoToMenu();
            Assert.That(audio.CurrentMusic, Is.Null);
            yield return WaitFor(Menu);
            Assert.That(audio.CurrentMusic, Is.EqualTo(SoundId.MenuMusic));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            yield return new WaitForSecondsRealtime(1f);
#else
            Assert.Ignore("Loads authored scenes in the Editor.");
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator RewindingPlaysTheAudibleLoop()
        {
#if UNITY_EDITOR
            yield return Load(Level);
            var audio = AudioManager.Ensure();
            var player = Object.FindFirstObjectByType<CheckpointManager>().Player;
            var platform = Object.FindFirstObjectByType<RewindMovingPlatform>();
            Assert.That(audio.Library.rewind.Pick().name, Is.EqualTo("rewind-audible"));
            yield return new WaitForSeconds(3f); // the platform finishes its trip
            Assert.That(platform.BeginRewind(), Is.True);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(platform.Mode, Is.EqualTo(RewindMode.Rewinding));
            Assert.That(audio.IsLoopActive(SoundId.Rewind), Is.True);
            Assert.That(audio.LoopGain(SoundId.Rewind), Is.EqualTo(1f));
            var source = audio.GetComponents<AudioSource>().Single(s => s.clip != null && s.clip.name == "rewind-audible");
            Assert.That(source.loop && source.volume > 0.5f, Is.True);
            Assert.That(source.outputAudioMixerGroup, Is.SameAs(audio.Library.sfxGroup));
            platform.EndRewind();
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(audio.IsLoopActive(SoundId.Rewind), Is.False);
            Assert.That(audio.LoopGain(SoundId.Rewind), Is.EqualTo(0f));
#else
            Assert.Ignore("Loads authored scenes in the Editor.");
            yield break;
#endif
        }

#if UNITY_EDITOR
        private static IEnumerator Load(string path)
        {
            EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
            for (var i = 0; i < 4; i++) yield return null;
        }

        private static IEnumerator WaitFor(string path)
        {
            var deadline = Time.realtimeSinceStartup + 10f;
            while (SceneManager.GetActiveScene().path != path && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(path));
            yield return null;
        }
#endif
    }
}

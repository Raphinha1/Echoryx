using System;
using System.Collections;
using System.Collections.Generic;
using Echoryx.Flow;
using Echoryx.Level;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Echoryx.Audio.PlayTests
{
    public sealed class AudioPlayChecks
    {
        private AudioLibrary empty;
        private AudioLibrary shipped;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (AudioManager.Instance != null && shipped != null) AudioManager.Instance.Library = shipped;
            if (empty != null) UnityEngine.Object.Destroy(empty);
            GameProgress.Storage = null;
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator AnEmptyLibraryNeverFails()
        {
            var audio = AudioManager.Ensure();
            shipped = audio.Library;
            empty = ScriptableObject.CreateInstance<AudioLibrary>();
            empty.jump.clips = new AudioClip[] { null }; // an unassigned slot in the list
            empty.footsteps.clips = null;
            foreach (var library in new[] { empty, null })
            {
                audio.Library = library;
                foreach (SoundId id in Enum.GetValues(typeof(SoundId)))
                {
                    Assert.That(library == null || library.Get(id).Pick() == null, Is.True, id.ToString());
                    audio.Play(id);
                    audio.PlayMusic(id);
                }
                audio.SetRewindLoop(true);
                audio.SetRewindLoop(false);
                audio.Paused = true;
                yield return null;
                audio.Paused = false;
                yield return null;
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ScenesChooseMusicAndGameEventsRequestSounds()
        {
#if UNITY_EDITOR
            GameProgress.Storage = new MemoryProgressStorage();
            var audio = AudioManager.Ensure();
            var requested = new List<SoundId>();
            Action<SoundId> record = requested.Add;
            audio.Requested += record;
            try
            {
                yield return Load("Assets/_Echoryx/Scenes/MainMenu.unity");
                Assert.That(audio.CurrentMusic, Is.EqualTo(SoundId.MenuMusic));
                UnityEngine.Object.FindFirstObjectByType<MainMenuController>().SelectButton.onClick.Invoke();
                Assert.That(requested, Does.Contain(SoundId.UiClick));

                yield return Load("Assets/_Echoryx/Scenes/Level_01_Tutorial.unity");
                Assert.That(audio.CurrentMusic, Is.EqualTo(SoundId.LevelMusic));
                Assert.That(UnityEngine.Object.FindObjectsByType<AudioManager>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
                var flow = UnityEngine.Object.FindFirstObjectByType<LevelFlow>();
                flow.Pause();
                yield return null;
                yield return null;
                Assert.That(audio.Paused, Is.True);
                flow.Resume();
                for (var i = 0; i < 5; i++) yield return null;
                Assert.That(audio.Paused, Is.False);
                UnityEngine.Object.FindFirstObjectByType<LevelStats>().Complete();
                Assert.That(requested, Does.Contain(SoundId.Goal));
            }
            finally { audio.Requested -= record; }
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
#endif
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using Echoryx.Echo;
using Echoryx.Flow;
using Echoryx.Game;
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
                audio.SetLoop(SoundId.Rewind, true);
                audio.SetLoop(SoundId.Rewind, false);
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

        [UnityTest]
        public IEnumerator EchoLoopsStartAndStopWithTheEcho()
        {
#if UNITY_EDITOR
            GameProgress.Storage = new MemoryProgressStorage();
            var audio = AudioManager.Ensure();
            shipped = audio.Library;
            empty = ScriptableObject.CreateInstance<AudioLibrary>();
            var clip = AudioClip.Create("tone", 44100, 1, 44100, false);
            empty.echoRecord.clips = new[] { clip };
            empty.echoPlay.clips = new[] { clip };
            var requested = new List<SoundId>();
            Action<SoundId> record = requested.Add;
            try
            {
                yield return Load("Assets/_Echoryx/Scenes/Level_01_Tutorial.unity");
                audio.Library = empty;
                audio.Requested += record;
                var manager = UnityEngine.Object.FindFirstObjectByType<CheckpointManager>();
                var recorder = manager.Player.GetComponent<EchoRecorder>();
                var playback = manager.Player.GetComponent<EchoPlayback>();
                var flow = UnityEngine.Object.FindFirstObjectByType<LevelFlow>();

                // Recording: starts, holds through a pause, resumes, stops when the recording ends.
                recorder.Toggle();
                yield return Settle();
                AssertLoops(audio, true, false);
                Assert.That(audio.LoopGain(SoundId.EchoRecord), Is.EqualTo(1f));
                flow.Pause();
                yield return Settle();
                Assert.That(audio.IsLoopActive(SoundId.EchoRecord) && audio.IsLoopHeld(SoundId.EchoRecord), Is.True);
                Assert.That(audio.LoopGain(SoundId.EchoRecord), Is.EqualTo(0f));
                flow.Resume();
                yield return Settle();
                Assert.That(audio.IsLoopHeld(SoundId.EchoRecord), Is.False);
                Assert.That(audio.LoopGain(SoundId.EchoRecord), Is.EqualTo(1f));
                recorder.Toggle();
                yield return Settle();
                AssertLoops(audio, false, false);
                Assert.That(audio.LoopGain(SoundId.EchoRecord), Is.EqualTo(0f));
                Assert.That(requested, Has.No.Member(SoundId.EchoEnd));

                // Playback until it finishes by itself: the end sound plays once.
                Assert.That(playback.TryStart(), Is.True);
                yield return null;
                AssertLoops(audio, false, true);
                var deadline = Time.time + 8f; // longer than the five-second Echo limit
                while (recorder.State == EchoState.Playing && Time.time < deadline) yield return null;
                yield return Settle();
                AssertLoops(audio, false, false);
                Assert.That(requested.FindAll(id => id == SoundId.EchoEnd).Count, Is.EqualTo(1));

                // Cancel while recording.
                recorder.Toggle();
                yield return Settle();
                AssertLoops(audio, true, false);
                recorder.Cancel();
                yield return Settle();
                AssertLoops(audio, false, false);

                // Respawn while recording and while playing: both stop, without an end sound.
                recorder.Toggle();
                yield return Settle();
                manager.RespawnNow();
                yield return Settle();
                AssertLoops(audio, false, false);
                recorder.Toggle();
                yield return Settle();
                recorder.Toggle();
                yield return null;
                Assert.That(playback.TryStart(), Is.True);
                yield return null;
                AssertLoops(audio, false, true);
                requested.Clear();
                manager.RespawnNow();
                yield return Settle();
                AssertLoops(audio, false, false);
                Assert.That(requested, Has.No.Member(SoundId.EchoEnd));
            }
            finally
            {
                audio.Requested -= record;
                UnityEngine.Object.Destroy(clip);
            }
#else
            Assert.Ignore("Loads authored scenes in the Editor.");
            yield break;
#endif
        }

        // Longer than the loop fade, in the unscaled time the fade uses.
        private static IEnumerator Settle()
        {
            yield return null;
            yield return new WaitForSecondsRealtime(AudioManager.LoopFadeSeconds * 3f);
            yield return null;
        }

        private static void AssertLoops(AudioManager audio, bool recording, bool playing)
        {
            Assert.That(audio.IsLoopActive(SoundId.EchoRecord), Is.EqualTo(recording), "recording loop");
            Assert.That(audio.IsLoopActive(SoundId.EchoPlay), Is.EqualTo(playing), "playback loop");
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

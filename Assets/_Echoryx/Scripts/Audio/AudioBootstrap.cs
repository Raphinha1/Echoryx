using Echoryx.Flow;
using Echoryx.Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoryx.Audio
{
    /// <summary>Adds audio at run time: music per scene, gameplay sounds and button sounds. No scene wiring.</summary>
    internal static class AudioBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            UiEvents.ButtonCreated -= UiButtonSound.Attach;
            UiEvents.ButtonCreated += UiButtonSound.Attach;
            SceneTransition.Started -= OnTransitionStarted;
            SceneTransition.Started += OnTransitionStarted;
        }

        // Lower the current music while the screen darkens, unless the destination plays the same track.
        private static void OnTransitionStarted(string scenePath)
        {
            var audio = AudioManager.Ensure();
            var next = MusicFor(scenePath);
            if (next.HasValue && next != audio.CurrentMusic) audio.FadeOutMusic();
        }

        private static SoundId? MusicFor(string scenePath)
        {
            var catalog = LevelCatalog.Current;
            if (catalog == null) return null;
            if (scenePath == catalog.MainMenuScenePath) return SoundId.MenuMusic;
            return catalog.IndexOfScene(scenePath) >= 0 ? SoundId.LevelMusic : (SoundId?)null;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var audio = AudioManager.Ensure();
            audio.Paused = false;
            audio.StopLoops();
            var track = MusicFor(scene.path);
            if (track.HasValue) audio.PlayMusic(track.Value);

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<CheckpointManager>(true) == null) continue;
                var go = new GameObject("Game Audio Events");
                SceneManager.MoveGameObjectToScene(go, scene);
                go.AddComponent<GameAudioEvents>();
                break;
            }
        }
    }
}

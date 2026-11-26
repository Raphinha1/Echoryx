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
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var audio = AudioManager.Ensure();
            audio.Paused = false;
            audio.SetRewindLoop(false);
            var catalog = LevelCatalog.Current;
            if (catalog != null && scene.path == catalog.MainMenuScenePath) audio.PlayMusic(SoundId.MenuMusic);
            else if (catalog != null && catalog.IndexOfScene(scene.path) >= 0) audio.PlayMusic(SoundId.LevelMusic);

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

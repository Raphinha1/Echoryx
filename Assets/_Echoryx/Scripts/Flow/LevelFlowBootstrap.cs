using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoryx.Flow
{
    /// <summary>
    /// Adds the LevelFlow to any loaded scene that the catalog lists, so a new level needs no scene wiring
    /// and rebuilding a level scene cannot lose its menus. Also works when playing a level from the Editor.
    /// </summary>
    internal static class LevelFlowBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var catalog = LevelCatalog.Current;
            if (catalog == null) return;
            var index = catalog.IndexOfScene(scene.path);
            if (index >= 0) LevelFlow.Install(scene, catalog, index);
        }
    }
}

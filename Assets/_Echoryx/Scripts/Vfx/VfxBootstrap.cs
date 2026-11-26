using Echoryx.Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoryx.Vfx
{
    /// <summary>Adds the effects at run time to every scene with a checkpoint manager. No scene wiring.</summary>
    internal static class VfxBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<CheckpointManager>(true) == null) continue;
                var vfx = GameVfx.Create(null);
                if (vfx != null) SceneManager.MoveGameObjectToScene(vfx.gameObject, scene);
                break;
            }
        }
    }
}

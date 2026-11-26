using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoryx.Flow
{
    /// <summary>Single place that loads scenes: always leaves time running.</summary>
    public static class SceneNavigator
    {
        public static bool IsInBuild(string scenePath)
        {
            if (!string.IsNullOrEmpty(scenePath) && SceneUtility.GetBuildIndexByScenePath(scenePath) >= 0) return true;
            Debug.LogError("Scene is not in Build Settings: '" + scenePath + "'. Run Echoryx/Flow/2 Sync Build Settings From Catalog.");
            return false;
        }

        public static bool Load(string scenePath)
        {
            if (!IsInBuild(scenePath)) return false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(scenePath, LoadSceneMode.Single);
            return true;
        }
    }
}

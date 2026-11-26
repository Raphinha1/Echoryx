using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Echoryx.Flow
{
    /// <summary>Persistent black fade around a scene change. Uses unscaled time, so it also runs from a pause.</summary>
    public sealed class SceneTransition : MonoBehaviour
    {
        public const float FadeSeconds = 0.6f;

        private static SceneTransition instance;
        private CanvasGroup group;
        private string pending;
        private int holdFrames;

        /// <summary>Raised with the destination path as soon as a fade starts (e.g. to fade the music).</summary>
        public static event Action<string> Started;

        public static bool IsLeaving => instance != null && instance.pending != null;
        public static float Darkness => instance != null ? instance.group.alpha : 0f;

        /// <summary>Fades to black, loads the scene and fades back. False if busy or the scene is not in the build.</summary>
        public static bool To(string scenePath)
        {
            if (IsLeaving || !SceneNavigator.IsInBuild(scenePath)) return false;
            Ensure().pending = scenePath;
            instance.group.blocksRaycasts = true;
            Started?.Invoke(scenePath);
            return true;
        }

        private static SceneTransition Ensure()
        {
            if (instance != null) return instance;
            var go = new GameObject("Scene Transition");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SceneTransition>();
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            go.AddComponent<GraphicRaycaster>();
            instance.group = go.AddComponent<CanvasGroup>();
            instance.group.alpha = 0f;
            instance.group.blocksRaycasts = false;
            var shade = UiKit.Stretched(go.transform, "Black");
            UiKit.Fill(shade, Color.black);
            return instance;
        }

        private void Update()
        {
            // The frame after a load is long; clamping keeps the fade from jumping.
            var step = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f) / FadeSeconds;
            if (pending != null)
            {
                group.alpha = Mathf.MoveTowards(group.alpha, 1f, step);
                if (group.alpha < 1f) return;
                var target = pending;
                pending = null;
                holdFrames = 2;
                SceneNavigator.Load(target);
                return;
            }
            if (holdFrames > 0)
            {
                holdFrames--;
                return;
            }
            group.blocksRaycasts = false; // the new scene is usable while it fades in
            group.alpha = Mathf.MoveTowards(group.alpha, 0f, step);
        }
    }
}

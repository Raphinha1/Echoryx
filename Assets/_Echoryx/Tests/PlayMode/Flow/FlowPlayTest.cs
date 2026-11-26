using System.Collections;
using NUnit.Framework;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Echoryx.Flow.PlayTests
{
    /// <summary>Virtual devices, deterministic frames and throw-away progress storage for flow checks.</summary>
    public abstract class FlowPlayTest
    {
        protected const string MenuScene = "Assets/_Echoryx/Scenes/MainMenu.unity";
        protected const string LevelScene = "Assets/_Echoryx/Scenes/Level_01_Tutorial.unity";
        protected Keyboard keyboard;
        protected Mouse mouse;
        protected MemoryProgressStorage storage;
        private InputSettings originalSettings;
        private InputSettings testSettings;
        private HideFlags originalFlags;

        [UnitySetUp]
        public IEnumerator SetUpDevices()
        {
            storage = new MemoryProgressStorage();
            GameProgress.Storage = storage; // never the real PlayerPrefs
            Time.timeScale = 1f;
            Time.captureDeltaTime = 1f / 60f;
            originalSettings = InputSystem.settings;
            originalFlags = originalSettings.hideFlags;
            originalSettings.hideFlags = HideFlags.None;
            testSettings = Object.Instantiate(originalSettings);
            testSettings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = testSettings;
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDownDevices()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            if (originalSettings != null)
            {
                InputSystem.settings = originalSettings;
                originalSettings.hideFlags = originalFlags;
            }
            if (testSettings != null) Object.Destroy(testSettings);
            LevelCatalog.Override = null;
            MainMenuController.QuitHandler = null;
            GameProgress.Storage = null;
            Time.timeScale = 1f;
            Time.captureDeltaTime = 0f;
            yield return null;
        }

        protected static IEnumerator Load(string scene)
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(scene, new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("These checks load authored scenes in Unity Editor.");
#endif
            yield return Frames(3);
        }

        protected static IEnumerator WaitForScene(string scene)
        {
            var deadline = Time.realtimeSinceStartup + 10f; // menu changes fade to black first
            while (SceneManager.GetActiveScene().path != scene && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(scene));
            yield return Frames(3);
        }

        protected static IEnumerator Frames(int count)
        {
            for (var i = 0; i < count; i++) yield return null;
        }

        protected void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

        protected IEnumerator Pulse(Key key)
        {
            Keys(key);
            yield return Frames(2);
            Keys();
            yield return Frames(2);
        }
    }
}

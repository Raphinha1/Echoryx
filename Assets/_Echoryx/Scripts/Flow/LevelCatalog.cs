using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoryx.Flow
{
    /// <summary>One playable level. The id is what progress is saved under: never change it once shipped.</summary>
    [Serializable]
    public sealed class LevelEntry
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [Tooltip("Project path of the scene, e.g. Assets/_Echoryx/Scenes/Level_02.unity")]
        [SerializeField] private string scenePath;

        public string Id => id;
        public string DisplayName => displayName;
        public string ScenePath => scenePath;

        public LevelEntry(string id, string displayName, string scenePath)
        {
            this.id = id;
            this.displayName = displayName;
            this.scenePath = scenePath;
        }
    }

    /// <summary>
    /// Ordered list of levels plus the data the game flow needs. Adding a level means adding an entry here
    /// (and its scene to Build Settings); no code changes.
    /// </summary>
    [CreateAssetMenu(fileName = ResourceName, menuName = "Echoryx/Level Catalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        public const string ResourceName = "LevelCatalog";

        [SerializeField] private string mainMenuScenePath = "Assets/_Echoryx/Scenes/MainMenu.unity";
        [Tooltip("Asset holding the System map (Pause, RestartLevel).")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private LevelEntry[] levels = Array.Empty<LevelEntry>();

        /// <summary>Lets tests run the flow against their own list of levels.</summary>
        public static LevelCatalog Override { get; set; }

        public static LevelCatalog Current => Override != null ? Override : Resources.Load<LevelCatalog>(ResourceName);

        public string MainMenuScenePath => mainMenuScenePath;
        public InputActionAsset InputActions => inputActions;
        public IReadOnlyList<LevelEntry> Levels => levels;
        public int Count => levels.Length;

        public void Configure(string menuScenePath, InputActionAsset actions, params LevelEntry[] entries)
        {
            mainMenuScenePath = menuScenePath;
            inputActions = actions;
            levels = entries ?? Array.Empty<LevelEntry>();
        }

        public int IndexOfScene(string scenePath)
        {
            for (var i = 0; i < levels.Length; i++)
                if (levels[i] != null && levels[i].ScenePath == scenePath) return i;
            return -1;
        }

        /// <summary>The level after this one, or null when it is the last (or the index is unknown).</summary>
        public LevelEntry NextAfter(int index) =>
            index >= 0 && index + 1 < levels.Length ? levels[index + 1] : null;

        /// <summary>Authoring problems; empty when the list is usable.</summary>
        public List<string> Validate()
        {
            var problems = new List<string>();
            if (levels.Length == 0) problems.Add("The catalog has no levels.");
            var ids = new HashSet<string>();
            var scenes = new HashSet<string>();
            for (var i = 0; i < levels.Length; i++)
            {
                var level = levels[i];
                if (level == null) { problems.Add("Level " + i + " is empty."); continue; }
                if (string.IsNullOrWhiteSpace(level.Id)) problems.Add("Level " + i + " has no id.");
                else if (!ids.Add(level.Id)) problems.Add("Duplicated level id: " + level.Id);
                if (string.IsNullOrWhiteSpace(level.DisplayName)) problems.Add("Level " + i + " has no display name.");
                if (string.IsNullOrWhiteSpace(level.ScenePath)) problems.Add("Level " + i + " has no scene path.");
                else if (!scenes.Add(level.ScenePath)) problems.Add("Duplicated level scene: " + level.ScenePath);
            }
            return problems;
        }
    }
}

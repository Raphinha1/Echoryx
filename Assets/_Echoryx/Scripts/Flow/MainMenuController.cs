using System;
using System.Collections.Generic;
using Echoryx.Level;
using UnityEngine;
using UnityEngine.UI;

namespace Echoryx.Flow
{
    /// <summary>Main menu: Play, level selection (locked levels disabled), options, credits and Quit.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        /// <summary>Grid of the level selector: 3 columns x 4 rows fit on screen.</summary>
        public const int MaximumVisibleLevels = 12;

        /// <summary>Replaces the real quit (used by tests).</summary>
        public static Action QuitHandler { get; set; }

        private readonly List<Button> levelButtons = new List<Button>();
        private LevelCatalog catalog;
        private GameProgress progress;
        private GameObject canvasRoot;
        private GameObject mainPanel;
        private GameObject selectPanel;
        private bool leaving;

        public Button PlayButton { get; private set; }
        public Button SelectButton { get; private set; }
        public Button QuitButton { get; private set; }
        public Button BackButton { get; private set; }
        public Button OptionsButton { get; private set; }
        public Button CreditsButton { get; private set; }
        public OptionsPanel Options { get; private set; }
        public CreditsPanel Credits { get; private set; }
        public IReadOnlyList<Button> LevelButtons => levelButtons;
        public bool IsSelectingLevel => selectPanel != null && selectPanel.activeSelf;

        private void Start()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            UiKit.EnsureEventSystem(gameObject.scene);
            var current = LevelCatalog.Current;
            if (current == null)
            {
                Debug.LogError("No LevelCatalog found in Resources.", this);
                return;
            }
            Show(current, GameProgress.Load());
            gameObject.AddComponent<MenuMotion>();
        }

        /// <summary>(Re)builds the menu for a list of levels and a saved progress.</summary>
        public void Show(LevelCatalog levels, GameProgress saved)
        {
            catalog = levels;
            progress = saved;
            if (canvasRoot != null)
            {
                canvasRoot.SetActive(false);
                Destroy(canvasRoot);
            }
            Build();
            CloseLevelSelect();
        }

        public void Play()
        {
            if (catalog.Count > 0) LoadLevel(progress.ContinueIndex(catalog));
        }

        public void LoadLevel(int index)
        {
            if (leaving || !progress.IsUnlocked(catalog, index)) return;
            leaving = SceneTransition.To(catalog.Levels[index].ScenePath);
        }

        public void OpenLevelSelect()
        {
            mainPanel.SetActive(false);
            selectPanel.SetActive(true);
            UiKit.Select(levelButtons.Count > 0 ? levelButtons[progress.ContinueIndex(catalog) % levelButtons.Count] : BackButton);
        }

        public void CloseLevelSelect()
        {
            selectPanel.SetActive(false);
            mainPanel.SetActive(true);
            UiKit.Select(PlayButton);
        }

        public void Quit()
        {
            if (QuitHandler != null) { QuitHandler(); return; }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Build()
        {
            var canvas = UiKit.CreateCanvas(transform, "Menu Canvas", 0);
            canvasRoot = canvas.gameObject;
            var center = new Vector2(0.5f, 0.5f);
            var title = UiKit.Label(canvas.transform, "Game Title", "ECHORYX", 150f, UiKit.Lilac, center,
                new Vector2(0f, 400f), new Vector2(1400f, 190f));
            title.characterSpacing = 12f;
            UiFonts.ApplyTitle(title);
            title.gameObject.AddComponent<TitleEffect>();

            mainPanel = UiKit.Stretched(canvas.transform, "Main Panel").gameObject;
            UiKit.Label(mainPanel.transform, "Subtitle", "Usa el pasado para abrirte camino", 38f, UiKit.Orange, center,
                new Vector2(0f, 275f), new Vector2(1200f, 60f));
            var size = new Vector2(520f, 90f);
            PlayButton = UiKit.Button(mainPanel.transform, "Play", "Jugar", center, new Vector2(0f, 150f), size);
            SelectButton = UiKit.Button(mainPanel.transform, "Select Level", "Seleccionar nivel", center, new Vector2(0f, 40f), size);
            OptionsButton = UiKit.Button(mainPanel.transform, "Options Button", "Opciones", center, new Vector2(0f, -70f), size);
            CreditsButton = UiKit.Button(mainPanel.transform, "Credits Button", "Créditos", center, new Vector2(0f, -180f), size);
            QuitButton = UiKit.Button(mainPanel.transform, "Quit", "Salir", center, new Vector2(0f, -290f), size);
            PlayButton.onClick.AddListener(Play);
            SelectButton.onClick.AddListener(OpenLevelSelect);
            QuitButton.onClick.AddListener(Quit);

            selectPanel = UiKit.Stretched(canvas.transform, "Level Select Panel").gameObject;
            // Title 305..495, heading 240..300, cards from 225 down, Back at -480: clear gaps in reference units.
            UiKit.Label(selectPanel.transform, "Heading", "Seleccionar nivel", 48f, UiKit.Orange, center,
                new Vector2(0f, 270f), new Vector2(1200f, 60f));
            levelButtons.Clear();
            if (catalog.Count > MaximumVisibleLevels)
                Debug.LogWarning("The level selector shows only the first " + MaximumVisibleLevels + " levels.", this);
            var shown = Mathf.Min(catalog.Count, MaximumVisibleLevels);
            var columns = Mathf.Min(3, shown);
            for (var i = 0; i < shown; i++)
            {
                var index = i;
                var position = new Vector2((i % 3 - (columns - 1) * 0.5f) * 490f, 150f - i / 3 * 165f);
                var button = UiKit.Button(selectPanel.transform, "Level " + (i + 1), Describe(i), center, position,
                    new Vector2(460f, 150f), 34f);
                button.interactable = progress.IsUnlocked(catalog, i);
                if (!button.interactable) button.GetComponentInChildren<TMPro.TMP_Text>().color = UiKit.LockedText;
                button.onClick.AddListener(() => LoadLevel(index));
                levelButtons.Add(button);
            }
            BackButton = UiKit.Button(selectPanel.transform, "Back", "Volver", center, new Vector2(0f, -480f), new Vector2(360f, 70f), 34f);
            BackButton.onClick.AddListener(CloseLevelSelect);

            var heading = title.gameObject;
            Options = OptionsPanel.Create(canvas.transform, () => ReturnFromScreen(heading, OptionsButton), RefreshAfterReset);
            Credits = CreditsPanel.Create(canvas.transform, () => ReturnFromScreen(heading, CreditsButton));
            OptionsButton.onClick.AddListener(() => { mainPanel.SetActive(false); Options.Open(); });
            CreditsButton.onClick.AddListener(() => { mainPanel.SetActive(false); heading.SetActive(false); Credits.Open(); });
        }

        private void ReturnFromScreen(GameObject heading, Button focus)
        {
            heading.SetActive(true);
            mainPanel.SetActive(true);
            UiKit.Select(focus);
        }

        // The level buttons show saved progress, so they are relabelled in place (the options stay open).
        private void RefreshAfterReset()
        {
            progress = GameProgress.Load();
            for (var i = 0; i < levelButtons.Count; i++)
            {
                var unlocked = progress.IsUnlocked(catalog, i);
                levelButtons[i].interactable = unlocked;
                var label = levelButtons[i].GetComponentInChildren<TMPro.TMP_Text>();
                label.text = Describe(i);
                label.color = unlocked ? UiKit.Ink : UiKit.LockedText;
            }
        }

        private string Describe(int index)
        {
            var level = catalog.Levels[index];
            var record = progress.Find(level.Id);
            var detail = record != null
                ? "Mejor tiempo " + LevelStatsTracker.FormatTime(record.bestSeconds)
                    + "\nIntentos " + record.bestAttempts + " · Rewinds " + record.bestRewinds
                : progress.IsUnlocked(catalog, index) ? "Sin completar" : "Bloqueado";
            return (index + 1) + ". " + level.DisplayName + "\n<size=68%>" + detail + "</size>";
        }
    }
}

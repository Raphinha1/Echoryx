using Echoryx.Echo;
using Echoryx.Level;
using Echoryx.Player;
using Echoryx.Rewind;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Echoryx.Flow
{
    public enum LevelFlowState { Playing, Paused, Resuming, Completed }

    /// <summary>
    /// Pause menu, full level restart and the end-of-level step (save progress; Next / Retry / Menu buttons
    /// on the existing completion panel). Added at run time to every scene listed in the LevelCatalog.
    ///
    /// Pausing freezes the game with timeScale 0 and also stops what timeScale does not: gameplay input is
    /// suspended, the camera orbit and Rewind aiming are disabled and a held Rewind is released exactly as
    /// if the button had been let go. The pause frame still has a non-zero deltaTime, so it is simulated in full; the motor is
    /// disabled (keeping its state) only once time stops, and enabled again on the first frame where time
    /// advances. Any other frame loses one frame of platform transport per pause.
    /// Keys still held after resuming must be pressed again (same rule as a respawn).
    /// Runs before every gameplay input consumer so nothing leaks through on the pause frame.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class LevelFlow : MonoBehaviour
    {
        private const string PauseActionName = "System/Pause";
        private const string RestartActionName = "System/RestartLevel";
        private const string HelpActionName = "System/ToggleHelp";
        public const string AllLevelsCompletedText = "Has completado todos los niveles";

        private LevelCatalog catalog;
        private int levelIndex;
        private PlayerInputReader input;
        private PlayerMotor motor;
        private PlayerCameraOrbit orbit;
        private RewindInteractor rewind;
        private LevelStats stats;
        private InputAction pauseAction;
        private InputAction restartAction;
        private InputAction helpAction;
        private GameObject pauseMenu;
        private GameObject pausePanel;
        private bool motorWasEnabled;
        private bool orbitWasEnabled;
        private bool rewindWasEnabled;
        private bool leaving;

        public LevelFlowState State { get; private set; }
        public bool IsPauseMenuVisible => pauseMenu != null && pauseMenu.activeSelf;
        public Button ResumeButton { get; private set; }
        public Button RetryButton { get; private set; }
        public Button MenuButton { get; private set; }
        // Buttons added to the completion panel (null when the level has no such panel).
        public Button NextLevelButton { get; private set; }
        public Button CompleteRetryButton { get; private set; }
        public Button CompleteMenuButton { get; private set; }
        public TMP_Text AllCompletedLabel { get; private set; }
        public Button OptionsButton { get; private set; }
        public OptionsPanel Options { get; private set; }
        /// <summary>Null when the scene has no Echo recorder.</summary>
        public EchoHud Hud { get; private set; }

        public static LevelFlow Install(Scene scene, LevelCatalog catalog, int levelIndex)
        {
            var go = new GameObject("Level Flow");
            go.SetActive(false); // Wire everything before Awake/OnEnable run.
            SceneManager.MoveGameObjectToScene(go, scene);
            var flow = go.AddComponent<LevelFlow>();
            flow.catalog = catalog;
            flow.levelIndex = levelIndex;
            flow.input = Find<PlayerInputReader>(scene);
            flow.motor = Find<PlayerMotor>(scene);
            flow.orbit = Find<PlayerCameraOrbit>(scene);
            flow.rewind = Find<RewindInteractor>(scene);
            flow.stats = Find<LevelStats>(scene);
            UiKit.EnsureEventSystem(scene);
            flow.BuildPauseMenu();
            var recorder = Find<EchoRecorder>(scene);
            if (recorder != null) flow.Hud = EchoHud.Create(go.transform, recorder);
            flow.BuildCompletionButtons(Find<LevelCompletePanel>(scene));
            go.SetActive(true);
            return flow;
        }

        private static T Find<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }

        private void OnEnable()
        {
            var actions = catalog != null ? catalog.InputActions : null;
            pauseAction = actions != null ? actions.FindAction(PauseActionName) : null;
            restartAction = actions != null ? actions.FindAction(RestartActionName) : null;
            if (pauseAction == null || restartAction == null)
                Debug.LogError("The level catalog needs input actions with " + PauseActionName + " and " + RestartActionName + ".", this);
            helpAction = actions != null ? actions.FindAction(HelpActionName) : null;
            pauseAction?.Enable();
            restartAction?.Enable();
            helpAction?.Enable();
            if (stats != null) stats.Completed += OnLevelCompleted;
        }

        private void OnDisable()
        {
            if (stats != null) stats.Completed -= OnLevelCompleted;
            pauseAction?.Disable();
            restartAction?.Disable();
            helpAction?.Disable();
            // Never leave the game frozen if this object goes away while paused.
            if (State == LevelFlowState.Paused) Time.timeScale = 1f;
        }

        private void Update()
        {
            if (leaving) return;
            // Both follow Time.deltaTime, which only changes on the frame after timeScale does.
            if (State == LevelFlowState.Paused && Time.deltaTime <= 0f && motor != null) motor.enabled = false;
            else if (State == LevelFlowState.Resuming && Time.deltaTime > 0f) FinishResume();

            if (restartAction != null && restartAction.WasPressedThisFrame())
            {
                Retry();
                return;
            }

            if (State == LevelFlowState.Playing && Hud != null && helpAction != null && helpAction.WasPressedThisFrame())
                Hud.ToggleControls();

            if (pauseAction == null || !pauseAction.WasPressedThisFrame()) return;
            if (State != LevelFlowState.Paused) Pause();
            else if (Options.IsOpen) Options.Cancel(); // Esc backs out of the options first
            else Resume();
        }

        private void LateUpdate()
        {
            // Re-enable the motor after the resume frame's Update phase. A behaviour that an earlier script
            // enables during Update misses that frame's Update; doing it on the first advancing frame would
            // make the motor skip it while the platform has already moved.
            if (State == LevelFlowState.Resuming) RestoreMotor();
        }

        private void RestoreMotor()
        {
            if (motor != null) motor.enabled = motorWasEnabled;
        }

        public bool Pause()
        {
            if (leaving || State == LevelFlowState.Paused || (stats != null && stats.IsCompleted)) return false;
            if (State == LevelFlowState.Playing)
            {
                motorWasEnabled = motor != null && motor.enabled;
                orbitWasEnabled = orbit != null && orbit.enabled;
                rewindWasEnabled = rewind != null && rewind.enabled;
            }
            State = LevelFlowState.Paused;
            Time.timeScale = 0f;
            SuspendInteraction();
            pauseMenu.SetActive(true);
            pausePanel.SetActive(true);
            Hud?.SetVisible(false);
            UiKit.Select(ResumeButton);
            return true;
        }

        public bool Resume()
        {
            if (leaving || State != LevelFlowState.Paused) return false;
            Options.Hide();
            pauseMenu.SetActive(false);
            Hud?.SetVisible(true);
            UiKit.Select(null);
            Time.timeScale = 1f;
            State = LevelFlowState.Resuming;
            return true;
        }

        private void FinishResume()
        {
            if (input != null)
            {
                input.ResetInputState(); // held keys (or the button that pressed Resume) must be released first
                input.Suspended = false;
            }
            RestoreMotor();
            if (orbit != null) orbit.enabled = orbitWasEnabled;
            if (rewind != null) rewind.enabled = rewindWasEnabled;
            State = LevelFlowState.Playing;
        }

        /// <summary>Stops gameplay input, the camera and Rewind aiming, and frees the cursor.</summary>
        private void SuspendInteraction()
        {
            if (input != null) input.Suspended = true;
            if (rewind != null)
            {
                rewind.ReleaseForRespawn(); // ends a held Rewind the same way letting go does
                rewind.enabled = false;     // and stops its IMGUI reticle, which would draw over the menus
            }
            if (orbit != null) orbit.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnLevelCompleted()
        {
            var level = catalog.Levels[levelIndex];
            GameProgress.Load().RecordCompletion(level.Id, stats.Attempts, stats.RewindUses, stats.ElapsedSeconds);
            if (State == LevelFlowState.Resuming) FinishResume();
            State = LevelFlowState.Completed;
            SuspendInteraction();
            Hud?.SetVisible(false);
            // The completion panel subscribed first, so it is already visible and its buttons can take focus.
            UiKit.Select(NextLevelButton != null && NextLevelButton.gameObject.activeSelf ? NextLevelButton : CompleteRetryButton);
        }

        public void GoToNextLevel()
        {
            var next = catalog.NextAfter(levelIndex);
            if (State == LevelFlowState.Completed && next != null) FadeTo(next.ScenePath);
        }

        /// <summary>Restarts the whole level from scratch (R only returns to the last checkpoint).</summary>
        public void Retry() => Leave(gameObject.scene.path);

        public void GoToMenu() => FadeTo(catalog.MainMenuScenePath);

        private void FadeTo(string scenePath)
        {
            if (!leaving) leaving = SceneTransition.To(scenePath);
        }

        private void Leave(string scenePath)
        {
            if (!leaving) leaving = SceneNavigator.Load(scenePath);
        }

        /// <summary>Extends the level's existing completion panel; they appear and hide together with it.</summary>
        private void BuildCompletionButtons(LevelCompletePanel complete)
        {
            if (complete == null || complete.Root == null) return;
            var panel = (RectTransform)complete.Root.transform;
            var canvas = complete.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();

            // Taller to fit the buttons, nudged up so it stays clear of the hint strip.
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, 700f);
            panel.anchoredPosition = new Vector2(0f, 25f);
            var background = panel.GetComponent<Image>();
            if (background != null) background.color = UiKit.PanelColor;
            foreach (var text in panel.GetComponentsInChildren<TMP_Text>(true)) UiFonts.ApplyBody(text);
            var title = panel.Find("Title");
            if (title != null && title.TryGetComponent<TMP_Text>(out var heading))
            {
                heading.color = UiKit.Orange;
                heading.font = UiFonts.Title;
            }

            var hasNext = catalog.NextAfter(levelIndex) != null;
            var bottom = new Vector2(0.5f, 0f);
            var size = new Vector2(300f, 80f);
            NextLevelButton = UiKit.Button(panel, "Next Level", "Siguiente nivel", bottom, new Vector2(-320f, 36f), size, 34f);
            CompleteRetryButton = UiKit.Button(panel, "Retry", "Reintentar", bottom, new Vector2(hasNext ? 0f : -170f, 36f), size, 34f);
            CompleteMenuButton = UiKit.Button(panel, "Menu", "Menú", bottom, new Vector2(hasNext ? 320f : 170f, 36f), size, 34f);
            AllCompletedLabel = UiKit.Label(panel, "All Completed", AllLevelsCompletedText, 36f, UiKit.Orange,
                bottom, new Vector2(0f, 128f), new Vector2(900f, 44f));
            NextLevelButton.gameObject.SetActive(hasNext);
            AllCompletedLabel.gameObject.SetActive(!hasNext);
            NextLevelButton.onClick.AddListener(GoToNextLevel);
            CompleteRetryButton.onClick.AddListener(Retry);
            CompleteMenuButton.onClick.AddListener(GoToMenu);
        }

        private void OpenOptions()
        {
            pausePanel.SetActive(false);
            Options.Open();
        }

        private void BuildPauseMenu()
        {
            var canvas = UiKit.CreateCanvas(transform, "Pause Canvas", 100);
            var backdrop = UiKit.Stretched(canvas.transform, "Pause Menu");
            UiKit.Fill(backdrop, UiKit.Backdrop);
            pauseMenu = backdrop.gameObject;
            var center = new Vector2(0.5f, 0.5f);
            var panel = UiKit.Place(UiKit.Node(backdrop, "Panel"), center, Vector2.zero, new Vector2(760f, 760f));
            pausePanel = panel.gameObject;
            UiKit.Fill(panel, UiKit.PanelColor);
            UiFonts.ApplyTitle(UiKit.Label(panel, "Title", "Pausa", 80f, UiKit.Orange, center, new Vector2(0f, 290f), new Vector2(700f, 100f)));
            var size = new Vector2(520f, 90f);
            ResumeButton = UiKit.Button(panel, "Resume", "Reanudar", center, new Vector2(0f, 160f), size);
            RetryButton = UiKit.Button(panel, "Retry", "Reintentar nivel", center, new Vector2(0f, 50f), size);
            OptionsButton = UiKit.Button(panel, "Options Button", "Opciones", center, new Vector2(0f, -60f), size);
            MenuButton = UiKit.Button(panel, "Main Menu", "Menú principal", center, new Vector2(0f, -170f), size);
            UiKit.Label(panel, "Controls", "Esc: reanudar\nR: volver al checkpoint · Retroceso: reiniciar nivel", 28f,
                UiKit.Lilac, center, new Vector2(0f, -300f), new Vector2(720f, 90f));
            ResumeButton.onClick.AddListener(() => Resume());
            RetryButton.onClick.AddListener(Retry);
            MenuButton.onClick.AddListener(GoToMenu);
            Options = OptionsPanel.Create(backdrop, () =>
            {
                pausePanel.SetActive(true);
                UiKit.Select(OptionsButton);
            });
            OptionsButton.onClick.AddListener(OpenOptions);
            pauseMenu.SetActive(false);
        }
    }
}

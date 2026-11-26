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
    public enum LevelFlowState { Playing, Paused, Resuming }

    /// <summary>
    /// Pause menu and full level restart. Added at run time to every scene listed in the LevelCatalog.
    ///
    /// Pausing freezes the game with timeScale 0 and also stops what timeScale does not: gameplay input is
    /// suspended, the camera orbit is disabled and a held Rewind is released exactly as if the button had
    /// been let go. The pause frame still has a non-zero deltaTime, so it is simulated in full; the motor is
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

        private LevelCatalog catalog;
        private int levelIndex;
        private PlayerInputReader input;
        private PlayerMotor motor;
        private PlayerCameraOrbit orbit;
        private RewindInteractor rewind;
        private LevelStats stats;
        private InputAction pauseAction;
        private InputAction restartAction;
        private GameObject pauseMenu;
        private bool motorWasEnabled;
        private bool orbitWasEnabled;
        private bool leaving;

        public LevelFlowState State { get; private set; }
        public bool IsPauseMenuVisible => pauseMenu != null && pauseMenu.activeSelf;
        public Button ResumeButton { get; private set; }
        public Button RetryButton { get; private set; }
        public Button MenuButton { get; private set; }

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
            pauseAction?.Enable();
            restartAction?.Enable();
        }

        private void OnDisable()
        {
            pauseAction?.Disable();
            restartAction?.Disable();
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

            if (pauseAction == null || !pauseAction.WasPressedThisFrame()) return;
            if (State == LevelFlowState.Paused) Resume();
            else Pause();
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
            }
            State = LevelFlowState.Paused;
            Time.timeScale = 0f;
            SuspendInteraction();
            pauseMenu.SetActive(true);
            UiKit.Select(ResumeButton);
            return true;
        }

        public bool Resume()
        {
            if (leaving || State != LevelFlowState.Paused) return false;
            pauseMenu.SetActive(false);
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
            State = LevelFlowState.Playing;
        }

        /// <summary>Stops gameplay input and the camera, lets go of Rewind and frees the cursor.</summary>
        private void SuspendInteraction()
        {
            if (input != null) input.Suspended = true;
            if (rewind != null) rewind.ReleaseForRespawn();
            if (orbit != null) orbit.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>Restarts the whole level from scratch (R only returns to the last checkpoint).</summary>
        public void Retry() => Leave(gameObject.scene.path);

        public void GoToMenu() => Leave(catalog.MainMenuScenePath);

        private void Leave(string scenePath)
        {
            if (!leaving) leaving = SceneNavigator.Load(scenePath);
        }

        private void BuildPauseMenu()
        {
            var canvas = UiKit.CreateCanvas(transform, "Pause Canvas", 100);
            var backdrop = UiKit.Stretched(canvas.transform, "Pause Menu");
            UiKit.Fill(backdrop, UiKit.Backdrop);
            pauseMenu = backdrop.gameObject;
            var center = new Vector2(0.5f, 0.5f);
            var panel = UiKit.Place(UiKit.Node(backdrop, "Panel"), center, Vector2.zero, new Vector2(760f, 640f));
            UiKit.Fill(panel, UiKit.PanelColor);
            UiKit.Label(panel, "Title", "Pausa", 80f, UiKit.Orange, center, new Vector2(0f, 225f), new Vector2(700f, 100f));
            var size = new Vector2(520f, 90f);
            ResumeButton = UiKit.Button(panel, "Resume", "Reanudar", center, new Vector2(0f, 85f), size);
            RetryButton = UiKit.Button(panel, "Retry", "Reintentar nivel", center, new Vector2(0f, -30f), size);
            MenuButton = UiKit.Button(panel, "Main Menu", "Menú principal", center, new Vector2(0f, -145f), size);
            UiKit.Label(panel, "Controls", "Esc: reanudar\nR: volver al checkpoint · Retroceso: reiniciar nivel", 28f,
                UiKit.Lilac, center, new Vector2(0f, -255f), new Vector2(720f, 90f));
            ResumeButton.onClick.AddListener(() => Resume());
            RetryButton.onClick.AddListener(Retry);
            MenuButton.onClick.AddListener(GoToMenu);
            pauseMenu.SetActive(false);
        }
    }
}

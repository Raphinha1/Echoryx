using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Echoryx.Flow
{
    /// <summary>Options screen shared by the main menu and the pause menu, including "reset progress".</summary>
    public sealed class OptionsPanel : MonoBehaviour
    {
        private GameObject settings;
        private GameObject confirmation;
        private TMP_Text musicValue;
        private TMP_Text sfxValue;
        private TMP_Text resultLabel;
        private Action closed;
        private Action progressReset;

        public Slider MusicSlider { get; private set; }
        public Slider SfxSlider { get; private set; }
        public Button FullScreenButton { get; private set; }
        public Button PreviousResolutionButton { get; private set; }
        public Button NextResolutionButton { get; private set; }
        public Button ResetProgressButton { get; private set; }
        public Button ConfirmResetButton { get; private set; }
        public Button CancelResetButton { get; private set; }
        public Button BackButton { get; private set; }
        public TMP_Text ResolutionLabel { get; private set; }
        public bool IsOpen => gameObject.activeSelf;
        public bool IsConfirming => confirmation.activeSelf;

        /// <summary>Builds the screen hidden. onClosed runs after Back; onProgressReset after a confirmed reset.</summary>
        public static OptionsPanel Create(Transform canvas, Action onClosed, Action onProgressReset = null)
        {
            var root = UiKit.Stretched(canvas, "Options");
            var panel = root.gameObject.AddComponent<OptionsPanel>();
            panel.closed = onClosed;
            panel.progressReset = onProgressReset;
            panel.Build(root);
            root.gameObject.SetActive(false);
            return panel;
        }

        public void Open()
        {
            gameObject.SetActive(true);
            MusicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
            SfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
            resultLabel.text = string.Empty;
            confirmation.SetActive(false);
            settings.SetActive(true);
            Refresh();
            UiKit.Select(MusicSlider);
        }

        public void Close()
        {
            if (!IsOpen) return;
            Hide();
            closed?.Invoke();
        }

        /// <summary>Closes without returning to the previous screen (e.g. when the game resumes).</summary>
        public void Hide()
        {
            if (!IsOpen) return;
            GameSettings.Save();
            gameObject.SetActive(false);
        }

        /// <summary>One step back: from the confirmation to the options, or out of the options.</summary>
        public void Cancel()
        {
            if (IsConfirming) CancelReset();
            else Close();
        }

        private void AskReset()
        {
            settings.SetActive(false);
            confirmation.SetActive(true);
            UiKit.Select(CancelResetButton); // the safe choice has the focus
        }

        private void CancelReset()
        {
            confirmation.SetActive(false);
            settings.SetActive(true);
            UiKit.Select(ResetProgressButton);
        }

        private void ConfirmReset()
        {
            GameProgress.Reset();
            CancelReset();
            resultLabel.text = "Progreso borrado.";
            progressReset?.Invoke();
        }

        private void ToggleFullScreen()
        {
            GameSettings.SetDisplay(GameSettings.Resolution, !GameSettings.FullScreen);
            Refresh();
        }

        private void StepResolution(int step)
        {
            var sizes = GameSettings.Display.Sizes;
            if (sizes.Count == 0) return;
            var current = GameSettings.Resolution;
            var index = 0;
            for (var i = 0; i < sizes.Count; i++)
                if (sizes[i].Equals(current)) index = i;
            index = Mathf.Clamp(index + step, 0, sizes.Count - 1);
            GameSettings.SetDisplay(sizes[index], GameSettings.FullScreen);
            Refresh();
        }

        private void Refresh()
        {
            musicValue.text = Mathf.RoundToInt(MusicSlider.value * 100f) + " %";
            sfxValue.text = Mathf.RoundToInt(SfxSlider.value * 100f) + " %";
            FullScreenButton.GetComponentInChildren<TMP_Text>().text = "Pantalla completa: " + (GameSettings.FullScreen ? "Sí" : "No");
            ResolutionLabel.text = GameSettings.Resolution.ToString();
        }

        private void Build(RectTransform root)
        {
            UiKit.Fill(root, UiKit.Backdrop);
            var center = new Vector2(0.5f, 0.5f);
            var box = UiKit.Place(UiKit.Node(root, "Box"), center, Vector2.zero, new Vector2(1000f, 860f));
            UiKit.Fill(box, UiKit.PanelColor);

            settings = UiKit.Stretched(box, "Settings").gameObject;
            var parent = settings.transform;
            UiFonts.ApplyTitle(UiKit.Label(parent, "Title", "Opciones", 70f, UiKit.Orange, center, new Vector2(0f, 345f), new Vector2(900f, 90f)));
            MusicSlider = VolumeRow(parent, "Música", 220f, out musicValue);
            SfxSlider = VolumeRow(parent, "SFX", 120f, out sfxValue);
            MusicSlider.onValueChanged.AddListener(v => { GameSettings.MusicVolume = v; Refresh(); });
            SfxSlider.onValueChanged.AddListener(v => { GameSettings.SfxVolume = v; Refresh(); });

            FullScreenButton = UiKit.Button(parent, "Full Screen", "Pantalla completa", center, new Vector2(0f, 5f), new Vector2(620f, 80f), 34f);
            FullScreenButton.onClick.AddListener(ToggleFullScreen);
            UiKit.Label(parent, "Resolution Title", "Resolución", 34f, UiKit.Lilac, center, new Vector2(0f, -75f), new Vector2(600f, 44f));
            PreviousResolutionButton = UiKit.Button(parent, "Previous Resolution", "<", center, new Vector2(-270f, -140f), new Vector2(80f, 70f), 40f);
            NextResolutionButton = UiKit.Button(parent, "Next Resolution", ">", center, new Vector2(270f, -140f), new Vector2(80f, 70f), 40f);
            ResolutionLabel = UiKit.Label(parent, "Resolution", string.Empty, 38f, Color.white, center, new Vector2(0f, -140f), new Vector2(420f, 60f));
            PreviousResolutionButton.onClick.AddListener(() => StepResolution(-1));
            NextResolutionButton.onClick.AddListener(() => StepResolution(1));

            ResetProgressButton = UiKit.Button(parent, "Reset Progress", "Reiniciar progreso", center, new Vector2(0f, -245f), new Vector2(520f, 76f), 34f);
            ResetProgressButton.onClick.AddListener(AskReset);
            resultLabel = UiKit.Label(parent, "Result", string.Empty, 28f, UiKit.Orange, center, new Vector2(0f, -305f), new Vector2(800f, 36f));
            BackButton = UiKit.Button(parent, "Back", "Volver", center, new Vector2(0f, -370f), new Vector2(360f, 76f), 34f);
            BackButton.onClick.AddListener(Close);

            confirmation = UiKit.Stretched(box, "Confirmation").gameObject;
            UiFonts.ApplyTitle(UiKit.Label(confirmation.transform, "Question", "¿Seguro?", 70f, UiKit.Orange, center, new Vector2(0f, 150f), new Vector2(900f, 100f)));
            UiKit.Label(confirmation.transform, "Warning", "Se borrarán los niveles completados y las mejores marcas. No se puede deshacer.",
                34f, Color.white, center, new Vector2(0f, 40f), new Vector2(820f, 100f));
            ConfirmResetButton = UiKit.Button(confirmation.transform, "Confirm", "Sí, borrar", center, new Vector2(-220f, -110f), new Vector2(380f, 84f), 36f);
            CancelResetButton = UiKit.Button(confirmation.transform, "Cancel", "Cancelar", center, new Vector2(220f, -110f), new Vector2(380f, 84f), 36f);
            ConfirmResetButton.onClick.AddListener(ConfirmReset);
            CancelResetButton.onClick.AddListener(CancelReset);
            confirmation.SetActive(false);
        }

        private static Slider VolumeRow(Transform parent, string title, float y, out TMP_Text value)
        {
            var center = new Vector2(0.5f, 0.5f);
            UiKit.Label(parent, title + " Title", title, 38f, UiKit.Lilac, center, new Vector2(-340f, y), new Vector2(220f, 56f));
            var slider = UiKit.Slider(parent, title + " Slider", center, new Vector2(20f, y), new Vector2(480f, 56f), 1f);
            value = UiKit.Label(parent, title + " Value", string.Empty, 34f, Color.white, center, new Vector2(350f, y), new Vector2(160f, 56f));
            return slider;
        }
    }
}

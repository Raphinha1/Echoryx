using Echoryx.Echo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Echoryx.Flow
{
    /// <summary>
    /// Echo status, recording bar and a controls reminder. Only reads the recorder's public state.
    /// The recording is timed with scaled time, so a pause does not move the bar.
    /// </summary>
    [DefaultExecutionOrder(300)]
    public sealed class EchoHud : MonoBehaviour
    {
        public const string ReadyToRecordText = "Listo para grabar";
        public const string ReadyToPlayText = "Listo para reproducir";
        public const string RecordingText = "Grabando";
        public const string PlayingText = "Echo activo";
        public const string ControlsText = "Q grabar · E reproducir · Clic derecho rebobinar · H ocultar";

        private static readonly Color Cyan = new Color(0.35f, 0.9f, 1f);
        private static readonly Color Red = new Color(1f, 0.38f, 0.38f);

        private EchoRecorder recorder;
        private GameObject canvas;
        private GameObject bar;
        private RectTransform fill;
        private GameObject controls;
        private EchoState previous;
        private double recordingStarted;

        public TMP_Text StateLabel { get; private set; }
        public bool IsVisible => canvas != null && canvas.activeSelf;
        public bool IsBarVisible => bar.activeSelf;
        public bool AreControlsVisible => controls.activeSelf;
        /// <summary>Fraction of the five-second recording used, 0 to 1.</summary>
        public float Progress { get; private set; }

        public static EchoHud Create(Transform parent, EchoRecorder recorder)
        {
            var hud = parent.gameObject.AddComponent<EchoHud>();
            hud.recorder = recorder;
            hud.Build(parent);
            return hud;
        }

        public void SetVisible(bool visible) => canvas.SetActive(visible);

        public void ToggleControls()
        {
            GameSettings.ShowControls = !GameSettings.ShowControls;
            controls.SetActive(GameSettings.ShowControls);
        }

        private void LateUpdate()
        {
            if (recorder == null || canvas == null) return;
            var state = recorder.State;
            if (state == EchoState.Recording && previous != EchoState.Recording) recordingStarted = Time.timeAsDouble;
            previous = state;

            var recording = state == EchoState.Recording;
            Progress = recording ? Mathf.Clamp01((float)((Time.timeAsDouble - recordingStarted) / EchoCapture.HardMaximumSeconds)) : 0f;
            bar.SetActive(recording);
            fill.anchorMax = new Vector2(Progress, 1f);
            switch (state)
            {
                case EchoState.Recording: Show(RecordingText, Red); break;
                case EchoState.Playing: Show(PlayingText, Cyan); break;
                case EchoState.Ready: Show(ReadyToPlayText, UiKit.Orange); break;
                default: Show(ReadyToRecordText, UiKit.Lilac); break;
            }
        }

        private void Show(string text, Color color)
        {
            StateLabel.text = text;
            StateLabel.color = color;
        }

        private void Build(Transform parent)
        {
            var root = UiKit.CreateCanvas(parent, "Echo HUD Canvas", 10);
            canvas = root.gameObject;
            root.GetComponent<GraphicRaycaster>().enabled = false; // purely informative

            var topLeft = new Vector2(0f, 1f);
            var panel = UiKit.Place(UiKit.Node(root.transform, "Echo Status"), topLeft, new Vector2(32f, -32f), new Vector2(420f, 92f));
            UiKit.Fill(panel, new Color(0.05f, 0.035f, 0.11f, 0.6f)).raycastTarget = false;
            StateLabel = UiKit.Label(panel, "State", ReadyToRecordText, 32f, UiKit.Lilac, topLeft, new Vector2(20f, -10f), new Vector2(380f, 46f));
            StateLabel.alignment = TextAlignmentOptions.Left;

            var track = UiKit.Place(UiKit.Node(panel, "Recording Bar"), topLeft, new Vector2(20f, -64f), new Vector2(380f, 14f));
            UiKit.Fill(track, UiKit.Locked).raycastTarget = false;
            fill = UiKit.Stretched(track, "Fill");
            UiKit.Fill(fill, Red).raycastTarget = false;
            bar = track.gameObject;
            bar.SetActive(false);

            var topRight = new Vector2(1f, 1f);
            var help = UiKit.Place(UiKit.Node(root.transform, "Controls Reminder"), topRight, new Vector2(-32f, -32f), new Vector2(800f, 44f));
            UiKit.Fill(help, new Color(0.05f, 0.035f, 0.11f, 0.4f)).raycastTarget = false;
            var label = UiKit.Label(help, "Text", ControlsText, 22f, new Color(1f, 1f, 1f, 0.75f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780f, 40f));
            UiFonts.ApplyBody(label);
            controls = help.gameObject;
            controls.SetActive(GameSettings.ShowControls);
        }
    }
}

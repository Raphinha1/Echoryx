using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Echoryx.Flow
{
    /// <summary>Scrollable credits: mouse wheel or drag, up/down arrows, or a gamepad stick.</summary>
    public sealed class CreditsPanel : MonoBehaviour
    {
        private const float PixelsPerSecond = 900f;
        private Action closed;

        public ScrollRect Scroll { get; private set; }
        public Button BackButton { get; private set; }
        public bool IsOpen => gameObject.activeSelf;

        public static CreditsPanel Create(Transform canvas, Action onClosed)
        {
            var root = UiKit.Stretched(canvas, "Credits");
            var panel = root.gameObject.AddComponent<CreditsPanel>();
            panel.closed = onClosed;
            panel.Build(root);
            root.gameObject.SetActive(false);
            return panel;
        }

        public void Open()
        {
            gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            Scroll.verticalNormalizedPosition = 1f;
            UiKit.Select(BackButton);
        }

        public void Close()
        {
            gameObject.SetActive(false);
            closed?.Invoke();
        }

        private void Update()
        {
            var direction = 0f;
            var keyboard = Keyboard.current;
            if (keyboard != null) direction += (keyboard.upArrowKey.isPressed ? 1f : 0f) - (keyboard.downArrowKey.isPressed ? 1f : 0f);
            var pad = Gamepad.current;
            if (pad != null && Mathf.Abs(pad.leftStick.y.ReadValue()) > 0.2f) direction += pad.leftStick.y.ReadValue();
            var hidden = Scroll.content.rect.height - Scroll.viewport.rect.height;
            if (direction != 0f && hidden > 0f)
                Scroll.verticalNormalizedPosition = Mathf.Clamp01(Scroll.verticalNormalizedPosition
                    + direction * PixelsPerSecond * Time.unscaledDeltaTime / hidden);
        }

        private void Build(RectTransform root)
        {
            UiKit.Fill(root, UiKit.Backdrop);
            var center = new Vector2(0.5f, 0.5f);
            var box = UiKit.Place(UiKit.Node(root, "Box"), center, Vector2.zero, new Vector2(1500f, 1000f));
            UiKit.Fill(box, UiKit.PanelColor);
            UiFonts.ApplyTitle(UiKit.Label(box, "Title", "Créditos", 70f, UiKit.Orange, center, new Vector2(0f, 425f), new Vector2(1000f, 90f)));

            var viewport = UiKit.Place(UiKit.Node(box, "Viewport"), center, new Vector2(0f, -15f), new Vector2(1400f, 760f));
            UiKit.Fill(viewport, new Color(0f, 0f, 0f, 0.2f)); // also receives the wheel and drag
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = UiKit.Node(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(36, 36, 20, 20);
            layout.spacing = 36f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            foreach (var text in CreditsText.Blocks())
            {
                var label = UiKit.Node(content, "Section").gameObject.AddComponent<TextMeshProUGUI>();
                label.font = UiFonts.Body;
                label.fontSize = 28f;
                label.color = Color.white;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.raycastTarget = false;
                label.text = text;
            }
            Scroll = box.gameObject.AddComponent<ScrollRect>();
            Scroll.viewport = viewport;
            Scroll.content = content;
            Scroll.horizontal = false;
            Scroll.movementType = ScrollRect.MovementType.Clamped;
            Scroll.scrollSensitivity = 60f;

            BackButton = UiKit.Button(box, "Back", "Volver", center, new Vector2(0f, -445f), new Vector2(360f, 76f), 34f);
            // Up/down scroll the text, so they must not move the focus away.
            var navigation = BackButton.navigation;
            navigation.mode = Navigation.Mode.None;
            BackButton.navigation = navigation;
            BackButton.onClick.AddListener(Close);
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Echoryx.Flow
{
    /// <summary>Shared look of the flow menus (lilac and orange, TextMeshPro), built in code.</summary>
    internal static class UiKit
    {
        public static readonly Color Backdrop = new Color(0.05f, 0.035f, 0.11f, 0.72f);
        public static readonly Color PanelColor = new Color(0.10f, 0.075f, 0.20f, 0.94f);
        public static readonly Color Lilac = new Color(0.76f, 0.66f, 1f);
        public static readonly Color Orange = new Color(1f, 0.62f, 0.22f);
        public static readonly Color Ink = new Color(0.08f, 0.05f, 0.16f);
        public static readonly Color Locked = new Color(0.30f, 0.26f, 0.42f, 0.85f);
        public static readonly Color LockedText = new Color(0.72f, 0.68f, 0.84f);

        public static Canvas CreateCanvas(Transform parent, string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>A rect anchored to one point of its parent, with the pivot on that same point.</summary>
        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretched(Transform parent, string name)
        {
            var rect = Node(parent, name);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static Image Fill(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static TextMeshProUGUI Label(Transform parent, string name, string text, float fontSize, Color color,
            Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = Place(Node(parent, name), anchor, position, size);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }

        public static Button Button(Transform parent, string name, string text, Vector2 anchor, Vector2 position,
            Vector2 size, float fontSize = 40f)
        {
            var rect = Place(Node(parent, name), anchor, position, size);
            var image = Fill(rect, Color.white);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Lilac;
            // Hover and keyboard/gamepad focus share the orange accent.
            colors.highlightedColor = Orange;
            colors.selectedColor = Orange;
            colors.pressedColor = new Color(0.86f, 0.48f, 0.12f);
            colors.disabledColor = Locked;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            var label = Label(rect, "Text", text, fontSize, Ink, new Vector2(0.5f, 0.5f), Vector2.zero, size - new Vector2(24f, 12f));
            label.fontStyle = FontStyles.Bold;
            return button;
        }

        /// <summary>Gives the scene an EventSystem driven by the Input System, if it has none.</summary>
        public static void EnsureEventSystem(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponentInChildren<EventSystem>(true) != null) return;
            var go = new GameObject("EventSystem");
            go.SetActive(false);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            go.SetActive(true);
        }

        public static void Select(Selectable target)
        {
            var events = EventSystem.current;
            if (events != null) events.SetSelectedGameObject(target != null ? target.gameObject : null);
        }
    }
}

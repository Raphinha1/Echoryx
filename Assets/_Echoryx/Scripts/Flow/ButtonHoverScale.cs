using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Echoryx.Flow
{
    /// <summary>Grows a button smoothly while the pointer is over it or it has focus. Works while paused.</summary>
    public sealed class ButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public const float HoverScale = 1.06f;
        private const float Rate = 14f;

        private Button button;
        private bool hovered;
        private bool selected;

        private void Awake() => button = GetComponent<Button>();

        public void OnPointerEnter(PointerEventData eventData) => hovered = true;
        public void OnPointerExit(PointerEventData eventData) => hovered = false;
        public void OnSelect(BaseEventData eventData) => selected = true;
        public void OnDeselect(BaseEventData eventData) => selected = false;

        private void Update()
        {
            var grow = (hovered || selected) && button != null && button.IsInteractable();
            var target = grow ? HoverScale : 1f;
            var step = 1f - Mathf.Exp(-Rate * Time.unscaledDeltaTime);
            transform.localScale = Vector3.one * Mathf.Lerp(transform.localScale.x, target, step);
        }

        private void OnDisable()
        {
            hovered = selected = false;
            transform.localScale = Vector3.one;
        }
    }
}

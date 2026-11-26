using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Echoryx.Audio
{
    /// <summary>Click and hover sounds of one menu button.</summary>
    public sealed class UiButtonSound : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        private Button button;

        public static void Attach(Button target)
        {
            var sound = target.gameObject.AddComponent<UiButtonSound>();
            sound.button = target;
            target.onClick.AddListener(() => AudioManager.Ensure().Play(SoundId.UiClick));
        }

        public void OnPointerEnter(PointerEventData eventData) => Hover();

        public void OnSelect(BaseEventData eventData)
        {
            // Only keyboard/gamepad navigation; menus focusing a button by themselves stay silent.
            if (eventData is AxisEventData) Hover();
        }

        private void Hover()
        {
            if (button != null && button.IsInteractable()) AudioManager.Ensure().Play(SoundId.UiHover);
        }
    }
}

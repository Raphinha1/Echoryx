using TMPro;
using UnityEngine;

namespace Echoryx.Level
{
    /// <summary>Single on-screen hint label; the most recently entered hint owns it.</summary>
    public sealed class TutorialHintDisplay : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text label;
        private TutorialHint owner;

        public string CurrentText => panel != null && panel.activeSelf && label != null ? label.text : string.Empty;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        public void Show(TutorialHint source, string message)
        {
            owner = source;
            label.text = message;
            panel.SetActive(true);
        }

        public void Hide(TutorialHint source)
        {
            if (owner != source) return;
            owner = null;
            panel.SetActive(false);
        }
    }
}

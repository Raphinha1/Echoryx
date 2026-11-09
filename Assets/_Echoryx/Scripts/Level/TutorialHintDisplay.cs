using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Echoryx.Level
{
    /// <summary>
    /// Single on-screen hint label. The most recently entered hint is shown; when it is left while an
    /// older overlapping hint is still active, that one is shown again instead of leaving the label empty.
    /// </summary>
    public sealed class TutorialHintDisplay : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text label;
        private readonly List<KeyValuePair<TutorialHint, string>> active = new List<KeyValuePair<TutorialHint, string>>();

        public string CurrentText => panel != null && panel.activeSelf && label != null ? label.text : string.Empty;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        public void Show(TutorialHint source, string message)
        {
            Remove(source);
            active.Add(new KeyValuePair<TutorialHint, string>(source, message));
            Refresh();
        }

        public void Hide(TutorialHint source)
        {
            Remove(source);
            Refresh();
        }

        private void Remove(TutorialHint source)
        {
            for (var i = active.Count - 1; i >= 0; i--)
                if (active[i].Key == source) active.RemoveAt(i);
        }

        private void Refresh()
        {
            if (active.Count == 0)
            {
                panel.SetActive(false);
                return;
            }
            label.text = active[active.Count - 1].Value;
            panel.SetActive(true);
        }
    }
}

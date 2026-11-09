using TMPro;
using UnityEngine;

namespace Echoryx.Level
{
    /// <summary>Shows the end-of-level summary when LevelStats completes.</summary>
    public sealed class LevelCompletePanel : MonoBehaviour
    {
        [SerializeField] private LevelStats stats;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text attemptsLabel;
        [SerializeField] private TMP_Text rewindsLabel;
        [SerializeField] private TMP_Text timeLabel;

        public bool IsVisible => panel != null && panel.activeSelf;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable()
        {
            if (stats != null) stats.Completed += Show;
        }

        private void OnDisable()
        {
            if (stats != null) stats.Completed -= Show;
        }

        private void Show()
        {
            attemptsLabel.text = "Intentos (muertes y reinicios): " + stats.Attempts;
            rewindsLabel.text = "Usos de Rewind: " + stats.RewindUses;
            timeLabel.text = "Tiempo total: " + LevelStatsTracker.FormatTime(stats.ElapsedSeconds);
            panel.SetActive(true);
        }
    }
}

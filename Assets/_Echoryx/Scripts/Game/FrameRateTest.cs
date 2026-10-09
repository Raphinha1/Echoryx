
using UnityEngine;

public class FrameRateTest : MonoBehaviour
{
    [Range(30, 120)]
    public int targetFps = 60;

    private int previousVSync;
    private int previousTarget;
    private float smoothedDelta;

    private void OnEnable()
    {
        previousVSync = QualitySettings.vSyncCount;
        previousTarget = Application.targetFrameRate;

        QualitySettings.vSyncCount = 0;
    }

    private void Update()
    {
        Application.targetFrameRate = targetFps;

        smoothedDelta = Mathf.Lerp(
            smoothedDelta,
            Time.unscaledDeltaTime,
            0.05f
        );
    }

    private void OnGUI()
    {
        float actualFps = smoothedDelta > 0
            ? 1f / smoothedDelta
            : 0f;

        GUI.Label(
            new Rect(15, 15, 320, 30),
            $"FPS objetivo: {targetFps} | Reales: {actualFps:F0}"
        );
    }

    private void OnDisable()
    {
        QualitySettings.vSyncCount = previousVSync;
        Application.targetFrameRate = previousTarget;
    }
}
  

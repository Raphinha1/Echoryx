using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Echoryx.Flow
{
    public interface ISettingsStorage
    {
        bool Has(string key);
        float GetFloat(string key, float fallback);
        void SetFloat(string key, float value);
        void Save();
    }

    public sealed class PlayerPrefsSettingsStorage : ISettingsStorage
    {
        public bool Has(string key) => PlayerPrefs.HasKey(key);
        public float GetFloat(string key, float fallback) => PlayerPrefs.GetFloat(key, fallback);
        public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);
        public void Save() => PlayerPrefs.Save();
    }

    /// <summary>Volatile settings, so tests never touch the player's preferences.</summary>
    public sealed class MemorySettingsStorage : ISettingsStorage
    {
        private readonly Dictionary<string, float> values = new Dictionary<string, float>();
        public bool Has(string key) => values.ContainsKey(key);
        public float GetFloat(string key, float fallback) => values.TryGetValue(key, out var value) ? value : fallback;
        public void SetFloat(string key, float value) => values[key] = value;
        public void Save() { }
    }

    public readonly struct DisplaySize : IEquatable<DisplaySize>
    {
        public readonly int Width;
        public readonly int Height;
        public DisplaySize(int width, int height) { Width = width; Height = height; }
        public bool Equals(DisplaySize other) => Width == other.Width && Height == other.Height;
        public override bool Equals(object obj) => obj is DisplaySize other && Equals(other);
        public override int GetHashCode() => Width * 10000 + Height;
        public override string ToString() => Width + " × " + Height;
    }

    public interface IDisplayDevice
    {
        IReadOnlyList<DisplaySize> Sizes { get; }
        DisplaySize Current { get; }
        bool FullScreen { get; }
        void Apply(DisplaySize size, bool fullScreen);
    }

    public sealed class ScreenDisplayDevice : IDisplayDevice
    {
        public IReadOnlyList<DisplaySize> Sizes
        {
            get
            {
                var sizes = Screen.resolutions.Select(r => new DisplaySize(r.width, r.height)).Distinct().ToList();
                if (!sizes.Contains(Current)) sizes.Add(Current);
                return sizes.OrderBy(s => s.Width).ThenBy(s => s.Height).ToList();
            }
        }

        public DisplaySize Current => new DisplaySize(Screen.width, Screen.height);
        public bool FullScreen => Screen.fullScreen;
        public void Apply(DisplaySize size, bool fullScreen) =>
            Screen.SetResolution(size.Width, size.Height, fullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }

    /// <summary>Player preferences: volumes, display and whether the controls reminder is shown.</summary>
    public static class GameSettings
    {
        private const string MusicKey = "Echoryx.MusicVolume";
        private const string SfxKey = "Echoryx.SfxVolume";
        private const string FullScreenKey = "Echoryx.FullScreen";
        private const string WidthKey = "Echoryx.ScreenWidth";
        private const string HeightKey = "Echoryx.ScreenHeight";
        private const string HelpKey = "Echoryx.ShowControls";

        private static ISettingsStorage storage;
        private static IDisplayDevice display;

        /// <summary>Set to null to return to PlayerPrefs.</summary>
        public static ISettingsStorage Storage
        {
            get => storage ?? (storage = new PlayerPrefsSettingsStorage());
            set => storage = value;
        }

        /// <summary>Set to null to return to the real screen.</summary>
        public static IDisplayDevice Display
        {
            get => display ?? (display = new ScreenDisplayDevice());
            set => display = value;
        }

        public static event Action VolumesChanged;

        /// <summary>Linear slider position, 0 to 1.</summary>
        public static float MusicVolume
        {
            get => Mathf.Clamp01(Storage.GetFloat(MusicKey, 1f));
            set { Storage.SetFloat(MusicKey, Mathf.Clamp01(value)); VolumesChanged?.Invoke(); }
        }

        public static float SfxVolume
        {
            get => Mathf.Clamp01(Storage.GetFloat(SfxKey, 1f));
            set { Storage.SetFloat(SfxKey, Mathf.Clamp01(value)); VolumesChanged?.Invoke(); }
        }

        /// <summary>Mixer attenuation for a linear volume; -80 dB is the mixer's silence.</summary>
        public static float ToDecibels(float volume) => volume <= 0.0001f ? -80f : 20f * Mathf.Log10(volume);

        public static bool ShowControls
        {
            get => Storage.GetFloat(HelpKey, 1f) > 0.5f;
            set { Storage.SetFloat(HelpKey, value ? 1f : 0f); Storage.Save(); }
        }

        public static bool FullScreen => Storage.Has(FullScreenKey) ? Storage.GetFloat(FullScreenKey, 1f) > 0.5f : Display.FullScreen;

        public static DisplaySize Resolution => Storage.Has(WidthKey) && Storage.Has(HeightKey)
            ? new DisplaySize((int)Storage.GetFloat(WidthKey, 0f), (int)Storage.GetFloat(HeightKey, 0f))
            : Display.Current;

        public static void SetDisplay(DisplaySize size, bool fullScreen)
        {
            Storage.SetFloat(WidthKey, size.Width);
            Storage.SetFloat(HeightKey, size.Height);
            Storage.SetFloat(FullScreenKey, fullScreen ? 1f : 0f);
            Display.Apply(size, fullScreen);
            Storage.Save();
        }

        /// <summary>Re-applies a saved display choice at start-up; does nothing until the player has made one.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplySavedDisplay()
        {
            if (Application.isEditor || !Storage.Has(WidthKey)) return;
            Display.Apply(Resolution, FullScreen);
        }

        public static void Save() => Storage.Save();
    }
}

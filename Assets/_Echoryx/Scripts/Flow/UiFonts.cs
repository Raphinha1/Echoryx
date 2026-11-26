using TMPro;
using UnityEngine;

namespace Echoryx.Flow
{
    /// <summary>Menu fonts built by Echoryx/Fonts/Create TMP Fonts; falls back to the TMP default when absent.</summary>
    public static class UiFonts
    {
        public const string TitlePath = "Fonts/TitleFont";
        public const string BodyPath = "Fonts/BodyFont";
        public const string GlowPath = "Fonts/TitleGlow";

        public static TMP_FontAsset Title => Resources.Load<TMP_FontAsset>(TitlePath) ?? TMP_Settings.defaultFontAsset;
        public static TMP_FontAsset Body => Resources.Load<TMP_FontAsset>(BodyPath) ?? TMP_Settings.defaultFontAsset;
        public static Material TitleGlow => Resources.Load<Material>(GlowPath);

        /// <summary>Title font for a heading, shrinking it if the wider typeface would not fit.</summary>
        public static void ApplyTitle(TMP_Text label)
        {
            label.font = Title;
            FitToRect(label);
        }

        public static void ApplyBody(TMP_Text label)
        {
            label.font = Body;
            FitToRect(label);
        }

        private static void FitToRect(TMP_Text label)
        {
            label.fontSizeMax = label.fontSize;
            label.fontSizeMin = label.fontSize * 0.55f;
            label.enableAutoSizing = true;
        }
    }
}

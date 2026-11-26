using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Echoryx.Flow.Editor
{
    /// <summary>
    /// Copies docs/credits.md and the font licences into Resources so the credits screen works in a build.
    /// Runs from the menu and before every build.
    /// </summary>
    public sealed class CreditsSync : IPreprocessBuildWithReport
    {
        public const string Folder = "Assets/_Echoryx/Resources/Credits";

        public static readonly (string source, string copy)[] Files =
        {
            ("docs/credits.md", Folder + "/credits.md"),
            ("Assets/_Echoryx/Fonts/Orbitron-OFL.txt", Folder + "/Orbitron-OFL.txt"),
            ("Assets/_Echoryx/Fonts/Exo2-OFL.txt", Folder + "/Exo2-OFL.txt"),
        };

        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) => Sync();

        [MenuItem("Echoryx/Credits/Sync To Resources")]
        public static void Sync()
        {
            Directory.CreateDirectory(Folder);
            foreach (var (source, copy) in Files)
            {
                var text = File.ReadAllText(source);
                if (File.Exists(copy) && File.ReadAllText(copy) == text) continue;
                File.WriteAllText(copy, text);
            }
            AssetDatabase.Refresh();
            Debug.Log("CREDITS synced.");
        }
    }
}

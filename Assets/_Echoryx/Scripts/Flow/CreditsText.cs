using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Echoryx.Flow
{
    /// <summary>Loads the shipped copy of docs/credits.md and the font licences, as TextMeshPro rich text blocks.</summary>
    public static class CreditsText
    {
        public const string CreditsResource = "Credits/credits";
        public static readonly string[] LicenseResources = { "Credits/Orbitron-OFL", "Credits/Exo2-OFL" };

        /// <summary>One block per section: a single text mesh cannot hold the whole screen.</summary>
        public static List<string> Blocks()
        {
            var blocks = new List<string>();
            var credits = Resources.Load<TextAsset>(CreditsResource);
            if (credits != null) AddSections(blocks, credits.text);
            else blocks.Add("No se encontraron los créditos.");
            foreach (var name in LicenseResources)
            {
                var license = Resources.Load<TextAsset>(name);
                if (license == null) continue;
                var heading = "<size=130%><color=#FF9E38>Licencia de " + name.Substring(name.IndexOf('/') + 1).Replace("-OFL", string.Empty) + "</color></size>\n";
                // Licence text is shown verbatim.
                blocks.Add(heading + "<noparse>" + license.text.Replace("\r", string.Empty).Trim() + "</noparse>");
            }
            return blocks;
        }

        private static void AddSections(List<string> blocks, string markdown)
        {
            var block = new List<string>();
            var tableHeader = true;
            foreach (var raw in markdown.Replace("\r", string.Empty).Split('\n'))
            {
                var line = raw.TrimEnd();
                if (line.StartsWith("## ") && block.Count > 0)
                {
                    blocks.Add(string.Join("\n", block).Trim());
                    block.Clear();
                }
                if (line.StartsWith("|"))
                {
                    if (Regex.IsMatch(line, "^[|:\\- ]+$")) continue; // table separator
                    var cells = line.Trim('|').Split('|');
                    for (var i = 0; i < cells.Length; i++) cells[i] = Inline(cells[i].Trim());
                    block.Add(tableHeader ? "<b>" + string.Join(" · ", cells) + "</b>" : "• " + string.Join(" · ", cells));
                    tableHeader = false;
                    continue;
                }
                tableHeader = true;
                var heading = Regex.Match(line, "^(#+) (.*)$");
                if (heading.Success)
                {
                    var size = heading.Groups[1].Length == 1 ? 160 : heading.Groups[1].Length == 2 ? 135 : 115;
                    block.Add("<size=" + size + "%><color=#FF9E38>" + Inline(heading.Groups[2].Value) + "</color></size>");
                }
                else if (line.StartsWith("- ")) block.Add("• " + Inline(line.Substring(2)));
                else block.Add(Inline(line));
            }
            if (block.Count > 0) blocks.Add(string.Join("\n", block).Trim());
        }

        private static string Inline(string text)
        {
            text = Regex.Replace(text, "<(https?://[^>]+)>", "$1");
            text = Regex.Replace(text, "\\[((?:[^\\[\\]]|\\[[^\\]]*\\])+)\\]\\((https?://[^)]+)\\)", "$1 ($2)");
            text = Regex.Replace(text, "\\[((?:[^\\[\\]]|\\[[^\\]]*\\])+)\\]\\([^)]+\\)", "$1"); // repo-relative links: text only
            text = Regex.Replace(text, "\\*\\*([^*]+)\\*\\*", "<b>$1</b>");
            text = Regex.Replace(text, "(?<![\\w*])\\*([^*]+)\\*(?![\\w*])", "<i>$1</i>");
            return text.Replace("`", string.Empty);
        }
    }
}

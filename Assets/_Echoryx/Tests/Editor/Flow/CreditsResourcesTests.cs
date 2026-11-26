using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Echoryx.Flow.Tests
{
    public sealed class CreditsResourcesTests
    {
        [TestCase("docs/credits.md", "Credits/credits")]
        [TestCase("Assets/_Echoryx/Fonts/Orbitron-OFL.txt", "Credits/Orbitron-OFL")]
        [TestCase("Assets/_Echoryx/Fonts/Exo2-OFL.txt", "Credits/Exo2-OFL")]
        public void TheShippedCopyMatchesItsSource(string source, string resource)
        {
            var copy = Resources.Load<TextAsset>(resource);
            Assert.That(copy, Is.Not.Null, resource);
            Assert.That(copy.text, Is.EqualTo(File.ReadAllText(source)), "Run Echoryx/Credits/Sync To Resources.");
        }

        [Test]
        public void CreditsRenderWithoutMarkdownAndIncludeTheFontLicences()
        {
            var text = string.Join("\n", CreditsText.Blocks());
            Assert.That(text, Does.Contain("MintoDog").And.Contain("Orbitron").And.Contain("Exo 2"));
            Assert.That(text, Does.Contain("Licencia de Orbitron").And.Contain("SIL OPEN FONT LICENSE Version 1.1"));
            var rendered = text.Substring(0, text.IndexOf("Licencia de Orbitron", System.StringComparison.Ordinal));
            Assert.That(rendered, Does.Not.Contain("**").And.Not.Contain("](").And.Not.Contain("|---").And.Not.Contain("\n## "));
        }
    }
}

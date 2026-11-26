using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace Echoryx.Flow.Tests
{
    /// <summary>The authored catalog, build order and menu scene that the game ships with.</summary>
    public sealed class ShippedFlowAssetsTests
    {
        private static LevelCatalog Catalog => LevelCatalog.Current;

        [Test]
        public void TheCatalogInResourcesIsValidAndStartsWithTheTutorial()
        {
            Assert.That(Catalog, Is.Not.Null);
            Assert.That(Catalog.Validate(), Is.Empty);
            Assert.That(Catalog.Levels[0].Id, Is.EqualTo("level-01-tutorial"), "Saved progress depends on this id.");
            Assert.That(Catalog.Levels[0].ScenePath, Is.EqualTo("Assets/_Echoryx/Scenes/Level_01_Tutorial.unity"));
            Assert.That(Catalog.InputActions, Is.Not.Null);
        }

        [Test]
        public void MenuIsTheFirstBuildSceneAndEveryCatalogLevelIsInTheBuild()
        {
            var build = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
            Assert.That(build[0], Is.EqualTo(Catalog.MainMenuScenePath));
            foreach (var level in Catalog.Levels)
            {
                Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(level.ScenePath), Is.Not.Null, level.ScenePath);
                Assert.That(build, Does.Contain(level.ScenePath), level.ScenePath);
            }
        }
    }
}

using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Echoryx.Flow.Tests
{
    public sealed class LevelProgressTests
    {
        private LevelCatalog catalog;
        private MemoryProgressStorage storage;

        [SetUp]
        public void SetUp()
        {
            catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            catalog.Configure("Menu.unity", null,
                new LevelEntry("a", "A", "A.unity"), new LevelEntry("b", "B", "B.unity"), new LevelEntry("c", "C", "C.unity"));
            storage = new MemoryProgressStorage();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(catalog);

        [Test]
        public void CatalogKeepsOrderAndKnowsTheNextLevel()
        {
            Assert.That(catalog.Count, Is.EqualTo(3));
            Assert.That(catalog.IndexOfScene("B.unity"), Is.EqualTo(1));
            Assert.That(catalog.IndexOfScene("Other.unity"), Is.EqualTo(-1));
            Assert.That(catalog.NextAfter(0).Id, Is.EqualTo("b"));
            Assert.That(catalog.NextAfter(2), Is.Null, "The last level has no next.");
            Assert.That(catalog.NextAfter(-1), Is.Null);
            Assert.That(catalog.Validate(), Is.Empty);
        }

        [Test]
        public void ValidationReportsAuthoringMistakes()
        {
            catalog.Configure("Menu.unity", null,
                new LevelEntry("a", "A", "A.unity"), new LevelEntry("a", "", "A.unity"), new LevelEntry("", "C", ""));
            var problems = string.Join("\n", catalog.Validate());
            Assert.That(problems, Does.Contain("Duplicated level id"));
            Assert.That(problems, Does.Contain("Duplicated level scene"));
            Assert.That(problems, Does.Contain("no display name"));
            Assert.That(problems, Does.Contain("no id"));
            Assert.That(problems, Does.Contain("no scene path"));
            catalog.Configure("Menu.unity", null);
            Assert.That(catalog.Validate(), Is.Not.Empty);
        }

        [Test]
        public void OnlyTheFirstLevelStartsUnlockedAndEachCompletionOpensTheNext()
        {
            var progress = new GameProgress(storage);
            Assert.That(Unlocked(progress), Is.EqualTo(new[] { true, false, false }));
            Assert.That(progress.ContinueIndex(catalog), Is.EqualTo(0));
            progress.RecordCompletion("a", 3, 2, 60d);
            Assert.That(Unlocked(progress), Is.EqualTo(new[] { true, true, false }));
            Assert.That(progress.ContinueIndex(catalog), Is.EqualTo(1));
            progress.RecordCompletion("b", 0, 1, 30d);
            progress.RecordCompletion("c", 0, 1, 30d);
            Assert.That(Unlocked(progress), Is.EqualTo(new[] { true, true, true }));
            Assert.That(progress.ContinueIndex(catalog), Is.EqualTo(0), "Everything done: Play starts over.");
            Assert.That(progress.IsUnlocked(catalog, 3), Is.False);
            Assert.That(progress.IsUnlocked(catalog, -1), Is.False);
        }

        [Test]
        public void ACompletedLevelStaysOpenWhenALevelIsInsertedBeforeIt()
        {
            var progress = new GameProgress(storage);
            progress.RecordCompletion("b", 1, 1, 10d);
            catalog.Configure("Menu.unity", null, new LevelEntry("a", "A", "A.unity"),
                new LevelEntry("new", "New", "New.unity"), new LevelEntry("b", "B", "B.unity"));
            Assert.That(progress.IsUnlocked(catalog, 2), Is.True);
        }

        [Test]
        public void BestStatisticsAreKeptIndependentlyAndSurviveReloading()
        {
            new GameProgress(storage).RecordCompletion("a", 3, 5, 90d);
            new GameProgress(storage).RecordCompletion("a", 7, 2, 120d);
            new GameProgress(storage).RecordCompletion("a", 4, 4, 45.5d);
            var record = new GameProgress(storage).Find("a");
            Assert.That(record.bestAttempts, Is.EqualTo(3));
            Assert.That(record.bestRewinds, Is.EqualTo(2));
            Assert.That(record.bestSeconds, Is.EqualTo(45.5d));
            Assert.That(new GameProgress(storage).IsCompleted("b"), Is.False);
        }

        [Test]
        public void UnreadableOrEmptySavesStartCleanAndCanBeOverwritten()
        {
            storage.Json = "not json {";
            LogAssert.Expect(LogType.Warning, "Saved progress is unreadable; starting with empty progress.");
            var progress = new GameProgress(storage);
            Assert.That(progress.IsCompleted("a"), Is.False);
            progress.RecordCompletion("a", 0, 0, 5d);
            Assert.That(new GameProgress(storage).IsCompleted("a"), Is.True);
            storage.Json = "{}";
            Assert.That(new GameProgress(storage).IsCompleted("a"), Is.False);
        }

        private bool[] Unlocked(GameProgress progress) =>
            new[] { progress.IsUnlocked(catalog, 0), progress.IsUnlocked(catalog, 1), progress.IsUnlocked(catalog, 2) };
    }
}

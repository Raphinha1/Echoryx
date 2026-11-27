using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Echoryx.Flow.PlayTests
{
    public sealed class MainMenuPlayChecks : FlowPlayTest
    {
        [UnityTest]
        public IEnumerator MenuStartsOnPlayAndEnterLoadsTheFirstLevel()
        {
            yield return Load(MenuScene);
            var menu = Object.FindFirstObjectByType<MainMenuController>();
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(menu.IsSelectingLevel, Is.False);
            Assert.That(Text(menu.PlayButton), Is.EqualTo("Jugar"));
            Assert.That(Text(menu.SelectButton), Is.EqualTo("Seleccionar nivel"));
            Assert.That(Text(menu.QuitButton), Is.EqualTo("Salir"));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(menu.PlayButton.gameObject));
            // Real submit through the EventSystem and Input System, not a direct method call.
            yield return Pulse(Key.Enter);
            yield return WaitForScene(LevelScene);
        }

        [UnityTest]
        public IEnumerator LevelSelectorDisablesLockedLevelsAndShowsBestStatistics()
        {
            yield return Load(MenuScene);
            var menu = Object.FindFirstObjectByType<MainMenuController>();
            var catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            catalog.Configure(MenuScene, null, new LevelEntry("a", "Uno", LevelScene),
                new LevelEntry("b", "Dos", "b.unity"), new LevelEntry("c", "Tres", "c.unity"));
            var progress = new GameProgress(storage);

            menu.Show(catalog, progress);
            menu.SelectButton.onClick.Invoke();
            yield return null;
            Assert.That(menu.IsSelectingLevel, Is.True);
            Assert.That(menu.LevelButtons.Select(b => b.interactable), Is.EqualTo(new[] { true, false, false }));
            Assert.That(Text(menu.LevelButtons[1]), Does.Contain("Bloqueado"));
            menu.LoadLevel(2);
            yield return Frames(3);
            Assert.That(menu != null && menu.IsSelectingLevel, Is.True, "A locked level cannot be loaded.");

            progress.RecordCompletion("a", 2, 1, 65d);
            menu.Show(catalog, progress);
            menu.SelectButton.onClick.Invoke();
            yield return null;
            Assert.That(menu.LevelButtons.Select(b => b.interactable), Is.EqualTo(new[] { true, true, false }));
            Assert.That(Text(menu.LevelButtons[0]), Does.Contain("1:05").And.Contain("Intentos 2").And.Contain("Rewinds 1"));
            Assert.That(Text(menu.LevelButtons[1]), Does.Contain("Sin completar"));
            menu.BackButton.onClick.Invoke();
            Assert.That(menu.IsSelectingLevel, Is.False);
            Object.Destroy(catalog);
        }

        [UnityTest]
        public IEnumerator SelectingAnUnlockedLevelLoadsItAndQuitAsksToExit()
        {
            yield return Load(MenuScene);
            var menu = Object.FindFirstObjectByType<MainMenuController>();
            var quits = 0;
            MainMenuController.QuitHandler = () => quits++;
            menu.QuitButton.onClick.Invoke();
            Assert.That(quits, Is.EqualTo(1));
            menu.SelectButton.onClick.Invoke();
            Assert.That(menu.LevelButtons.Count, Is.EqualTo(LevelCatalog.Current.Count));
            menu.LevelButtons[0].onClick.Invoke();
            yield return WaitForScene(LevelScene);
        }

        [UnityTest]
        public IEnumerator LevelSelectorLeavesClearSpaceBetweenTitleHeadingCardsAndBack()
        {
            yield return Load(MenuScene);
            var menu = Object.FindFirstObjectByType<MainMenuController>();
            // Worst case: the full grid of 12 levels.
            var catalog = ScriptableObject.CreateInstance<LevelCatalog>();
            var entries = new LevelEntry[MainMenuController.MaximumVisibleLevels];
            for (var i = 0; i < entries.Length; i++) entries[i] = new LevelEntry("l" + i, "Nivel " + i, "l" + i + ".unity");
            catalog.Configure(MenuScene, null, entries);
            menu.Show(catalog, new GameProgress(storage));
            menu.SelectButton.onClick.Invoke();
            yield return null;
            Canvas.ForceUpdateCanvases();
            var canvas = menu.PlayButton.GetComponentInParent<Canvas>().transform;
            var boxes = new System.Collections.Generic.List<(string name, Rect rect)>();
            foreach (var name in new[] { "Game Title", "Heading" })
                boxes.Add((name, Box(canvas, canvas.GetComponentsInChildren<TMP_Text>().First(t => t.name == name).rectTransform)));
            foreach (var button in menu.LevelButtons) boxes.Add((button.name, Box(canvas, (RectTransform)button.transform)));
            boxes.Add(("Back", Box(canvas, (RectTransform)menu.BackButton.transform)));
            for (var i = 0; i < boxes.Count; i++)
            {
                // 16:9, 16:10 and 4:3 all give a canvas at least 1920 x 1080 reference units tall (match 0.5).
                Assert.That(boxes[i].rect.yMin >= -540f && boxes[i].rect.yMax <= 540f && boxes[i].rect.xMin >= -831f && boxes[i].rect.xMax <= 831f,
                    Is.True, boxes[i].name + " fits a 4:3 screen");
                for (var j = i + 1; j < boxes.Count; j++)
                    Assert.That(boxes[i].rect.Overlaps(boxes[j].rect), Is.False, boxes[i].name + " overlaps " + boxes[j].name);
            }
            Object.Destroy(catalog);
        }

        // In canvas units, independent of the actual screen resolution.
        private static Rect Box(Transform canvas, RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var min = canvas.InverseTransformPoint(corners[0]);
            var max = canvas.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static string Text(Component button) => button.GetComponentInChildren<TMP_Text>().text;
    }
}

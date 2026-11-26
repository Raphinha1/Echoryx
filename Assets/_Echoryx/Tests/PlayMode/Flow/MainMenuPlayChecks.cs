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

        private static string Text(Component button) => button.GetComponentInChildren<TMP_Text>().text;
    }
}

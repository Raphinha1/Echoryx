using System.Collections;
using System.Collections.Generic;
using Echoryx.Game;
using Echoryx.Level;
using Echoryx.Player;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Echoryx.Flow.PlayTests
{
    /// <summary>
    /// The end-of-level step. These complete the level through LevelStats; the route test in
    /// Echoryx.Level.PlayTests covers reaching the real goal.
    /// </summary>
    public sealed class CompletionPlayChecks : FlowPlayTest
    {
        private const string SecondScene = "Assets/_Echoryx/Scenes/Level_02_Sincronia.unity";
        private LevelCatalog fake;

        [UnityTearDown]
        public IEnumerator DestroyCatalog()
        {
            if (fake != null) Object.Destroy(fake);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LastLevelSavesProgressAndSaysAllLevelsAreCompleted()
        {
            UseOnlyTheTutorial(); // the tutorial as the last level of its catalog
            yield return Load(LevelScene);
            var flow = Object.FindFirstObjectByType<LevelFlow>();
            var stats = Object.FindFirstObjectByType<LevelStats>();
            var panel = Object.FindFirstObjectByType<LevelCompletePanel>();
            Assert.That(flow.CompleteRetryButton.gameObject.activeInHierarchy, Is.False, "Hidden until the level ends.");
            yield return Pulse(Key.R);
            Assert.That(new GameProgress(storage).IsCompleted("level-01-tutorial"), Is.False);

            stats.Complete();
            yield return Frames(2);
            Assert.That(panel.IsVisible, Is.True);
            Assert.That(flow.State, Is.EqualTo(LevelFlowState.Completed));
            Assert.That(new GameProgress(storage).Find("level-01-tutorial").bestAttempts, Is.EqualTo(1));
            Assert.That(flow.NextLevelButton.gameObject.activeSelf, Is.False);
            Assert.That(flow.AllCompletedLabel.gameObject.activeInHierarchy, Is.True);
            Assert.That(flow.AllCompletedLabel.text, Is.EqualTo("Has completado todos los niveles"));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(flow.CompleteRetryButton.gameObject));
            Assert.That(panel.GetComponentInParent<Canvas>().GetComponent<GraphicRaycaster>(), Is.Not.Null, "Clickable HUD.");
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
            AssertNoOverlapInside((RectTransform)panel.Root.transform);

            // Gameplay stops listening and Esc no longer opens the pause menu.
            var player = Object.FindFirstObjectByType<CheckpointManager>().Player;
            var position = player.transform.position;
            Keys(Key.W);
            yield return Frames(40);
            Keys();
            Assert.That(Vector3.Distance(player.transform.position, position), Is.LessThan(0.05f));
            yield return Pulse(Key.Escape);
            Assert.That(flow.IsPauseMenuVisible, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator WithAFollowingLevelNextIsFocusedAndLoadsIt()
        {
            UseTwoLevels();
            yield return Load(LevelScene);
            var flow = Object.FindFirstObjectByType<LevelFlow>();
            Object.FindFirstObjectByType<LevelStats>().Complete();
            yield return Frames(2);
            Assert.That(flow.NextLevelButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(flow.AllCompletedLabel.gameObject.activeSelf, Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(flow.NextLevelButton.gameObject));
            AssertNoOverlapInside((RectTransform)Object.FindFirstObjectByType<LevelCompletePanel>().Root.transform);
            var progress = new GameProgress(storage);
            Assert.That(progress.IsUnlocked(fake, 1), Is.True, "Completing a level opens the next.");

            yield return Pulse(Key.Enter);
            yield return WaitForScene(SecondScene);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            // The second catalog entry also receives the flow, without any scene wiring.
            var second = Object.FindFirstObjectByType<LevelFlow>();
            Assert.That(second, Is.Not.Null);
            yield return Pulse(Key.Escape);
            Assert.That(second.State, Is.EqualTo(LevelFlowState.Paused));
        }

        [UnityTest]
        public IEnumerator RetryKeepsTheBestResultAndMenuShowsIt()
        {
            yield return Load(LevelScene);
            Object.FindFirstObjectByType<LevelStats>().Complete();
            yield return Frames(2);
            var first = new GameProgress(storage).Find("level-01-tutorial");
            var flow = Object.FindFirstObjectByType<LevelFlow>();
            flow.CompleteRetryButton.onClick.Invoke();
            yield return Frames(5);
            Assert.That(flow == null, Is.True);

            var stats = Object.FindFirstObjectByType<LevelStats>();
            Assert.That(stats.IsCompleted, Is.False);
            yield return Pulse(Key.R);
            yield return Frames(120); // a slower run with one more attempt
            stats.Complete();
            yield return Frames(2);
            var best = new GameProgress(storage).Find("level-01-tutorial");
            Assert.That(best.bestAttempts, Is.EqualTo(0));
            Assert.That(best.bestSeconds, Is.EqualTo(first.bestSeconds));

            Object.FindFirstObjectByType<LevelFlow>().CompleteMenuButton.onClick.Invoke();
            yield return WaitForScene(MenuScene);
            var menu = Object.FindFirstObjectByType<MainMenuController>();
            menu.SelectButton.onClick.Invoke();
            Assert.That(menu.LevelButtons[0].GetComponentInChildren<TMP_Text>().text, Does.Contain("Mejor tiempo").And.Contain("Intentos 0"));
        }

        [UnityTest]
        public IEnumerator BackspaceRestartsFromTheCompletionPanel()
        {
            yield return Load(LevelScene);
            var stats = Object.FindFirstObjectByType<LevelStats>();
            stats.Complete();
            yield return Frames(2);
            yield return Pulse(Key.Backspace);
            yield return Frames(3);
            Assert.That(stats == null, Is.True);
            Assert.That(Object.FindFirstObjectByType<LevelStats>().IsCompleted, Is.False);
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(LevelScene));
        }

        private void UseOnlyTheTutorial()
        {
            fake = ScriptableObject.CreateInstance<LevelCatalog>();
            fake.Configure(MenuScene, LevelCatalog.Current.InputActions, new LevelEntry("level-01-tutorial", "Uno", LevelScene));
            LevelCatalog.Override = fake;
        }

        private void UseTwoLevels()
        {
            var real = LevelCatalog.Current;
            fake = ScriptableObject.CreateInstance<LevelCatalog>();
            fake.Configure(MenuScene, real.InputActions,
                new LevelEntry("level-01-tutorial", "Uno", LevelScene), new LevelEntry("test-second", "Dos", SecondScene));
            LevelCatalog.Override = fake;
        }

        /// <summary>Layout sanity in reference units: visible texts and buttons stay inside and do not collide.</summary>
        private static void AssertNoOverlapInside(RectTransform panel)
        {
            Canvas.ForceUpdateCanvases();
            var boxes = new List<KeyValuePair<string, Rect>>();
            foreach (RectTransform child in panel)
            {
                if (!child.gameObject.activeSelf) continue;
                var corners = new Vector3[4];
                child.GetWorldCorners(corners);
                var min = (Vector2)panel.InverseTransformPoint(corners[0]);
                var max = (Vector2)panel.InverseTransformPoint(corners[2]);
                var box = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                Assert.That(box.yMin >= panel.rect.yMin - 0.5f && box.yMax <= panel.rect.yMax + 0.5f
                    && box.xMin >= panel.rect.xMin - 0.5f && box.xMax <= panel.rect.xMax + 0.5f, Is.True, child.name + " leaves the panel");
                foreach (var other in boxes) Assert.That(box.Overlaps(other.Value), Is.False, child.name + " overlaps " + other.Key);
                boxes.Add(new KeyValuePair<string, Rect>(child.name, box));
            }
        }
    }
}

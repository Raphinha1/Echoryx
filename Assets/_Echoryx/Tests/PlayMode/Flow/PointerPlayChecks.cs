using System.Collections;
using Echoryx.Level;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Echoryx.Flow.PlayTests
{
    /// <summary>Clicks the menus with a virtual mouse, through the real raycasters and EventSystem.</summary>
    public sealed class PointerPlayChecks : FlowPlayTest
    {
        [UnityTest]
        public IEnumerator MouseClicksWorkOnThePauseAndCompletionMenus()
        {
            yield return Load(LevelScene);
            var flow = Object.FindFirstObjectByType<LevelFlow>();
            yield return Pulse(Key.Escape);
            yield return Click(flow.ResumeButton);
            Assert.That(flow.State, Is.EqualTo(LevelFlowState.Playing), "Resume clicked.");
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            Object.FindFirstObjectByType<LevelStats>().Complete();
            yield return Frames(3);
            // This button lives on the level's own HUD canvas, which had no raycaster before the flow.
            yield return Click(flow.CompleteMenuButton);
            yield return WaitForScene(MenuScene);

            var menu = Object.FindFirstObjectByType<MainMenuController>();
            yield return Click(menu.SelectButton);
            Assert.That(menu.IsSelectingLevel, Is.True);
            yield return Click(menu.LevelButtons[0]);
            yield return WaitForScene(LevelScene);
        }

        private IEnumerator Click(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return Frames(2);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
            yield return Frames(2);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return Frames(3);
        }
    }
}

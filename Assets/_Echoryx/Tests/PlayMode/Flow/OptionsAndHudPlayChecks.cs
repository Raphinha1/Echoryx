using System.Collections;
using System.Collections.Generic;
using Echoryx.Echo;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Echoryx.Flow.PlayTests
{
    public sealed class OptionsAndHudPlayChecks : FlowPlayTest
    {
        private sealed class FakeDisplay : IDisplayDevice
        {
            public IReadOnlyList<DisplaySize> Sizes { get; } = new[] { new DisplaySize(1280, 720), new DisplaySize(1920, 1080) };
            public DisplaySize Current { get; private set; } = new DisplaySize(1920, 1080);
            public bool FullScreen { get; private set; } = true;
            public void Apply(DisplaySize size, bool fullScreen) { Current = size; FullScreen = fullScreen; }
        }

        [UnityTest]
        public IEnumerator MenuOptionsSaveVolumesAndDisplayAndResetProgressOnlyAfterConfirming()
        {
            var display = new FakeDisplay();
            GameSettings.Display = display;
            new GameProgress(storage).RecordCompletion("level-01-tutorial", 1, 1, 30d);
            yield return Load(MenuScene);
            var menu = Object.FindFirstObjectByType<MainMenuController>();
            menu.OptionsButton.onClick.Invoke();
            var options = menu.Options;
            Assert.That(options.IsOpen, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(options.MusicSlider.gameObject));

            options.MusicSlider.value = 0.5f;
            options.SfxSlider.value = 0f;
            yield return null;
            Assert.That(GameSettings.MusicVolume, Is.EqualTo(0.5f));
            Assert.That(GameSettings.SfxVolume, Is.EqualTo(0f));

            options.FullScreenButton.onClick.Invoke();
            options.PreviousResolutionButton.onClick.Invoke();
            Assert.That(display.FullScreen, Is.False);
            Assert.That(display.Current, Is.EqualTo(new DisplaySize(1280, 720)));
            Assert.That(options.ResolutionLabel.text, Is.EqualTo("1280 × 720"));
            options.PreviousResolutionButton.onClick.Invoke();
            Assert.That(display.Current.Width, Is.EqualTo(1280), "Stops at the smallest.");

            options.ResetProgressButton.onClick.Invoke();
            Assert.That(options.IsConfirming, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.SameAs(options.CancelResetButton.gameObject));
            options.CancelResetButton.onClick.Invoke();
            Assert.That(new GameProgress(storage).IsCompleted("level-01-tutorial"), Is.True, "Cancel keeps it.");
            options.ResetProgressButton.onClick.Invoke();
            options.ConfirmResetButton.onClick.Invoke();
            Assert.That(storage.Json, Is.Empty);
            Assert.That(menu.LevelButtons[0].GetComponentInChildren<TMP_Text>().text, Does.Contain("Sin completar"));

            options.BackButton.onClick.Invoke();
            Assert.That(options.IsOpen, Is.False);
            options.Open();
            Assert.That(options.MusicSlider.value, Is.EqualTo(0.5f), "Saved values come back.");
        }

        [UnityTest]
        public IEnumerator CreditsScrollAndGoBack()
        {
            yield return Load(MenuScene);
            var menu = Object.FindFirstObjectByType<MainMenuController>();
            menu.CreditsButton.onClick.Invoke();
            yield return Frames(3);
            var credits = menu.Credits;
            Assert.That(credits.IsOpen, Is.True);
            Assert.That(credits.Scroll.content.rect.height, Is.GreaterThan(credits.Scroll.viewport.rect.height * 2f));
            Assert.That(credits.Scroll.verticalNormalizedPosition, Is.EqualTo(1f).Within(0.001f), "Starts at the top.");
            Keys(Key.DownArrow);
            yield return new WaitForSecondsRealtime(0.5f); // scrolling runs on unscaled time
            Keys();
            Assert.That(credits.Scroll.verticalNormalizedPosition, Is.LessThan(0.99f));
            foreach (var label in credits.Scroll.content.GetComponentsInChildren<TMP_Text>())
            {
                label.ForceMeshUpdate();
                Assert.That(label.isTextTruncated, Is.False);
                Assert.That(label.textInfo.characterCount, Is.InRange(1, 12000), "Each block fits in one mesh.");
            }
            credits.BackButton.onClick.Invoke();
            Assert.That(credits.IsOpen, Is.False);
            Assert.That(menu.PlayButton.gameObject.activeInHierarchy, Is.True);
        }

        [UnityTest]
        public IEnumerator PauseOptionsAreLeftWithEscBeforeResuming()
        {
            yield return Load(LevelScene);
            var flow = Object.FindFirstObjectByType<LevelFlow>();
            yield return Pulse(Key.Escape);
            flow.OptionsButton.onClick.Invoke();
            Assert.That(flow.Options.IsOpen, Is.True);
            flow.Options.ResetProgressButton.onClick.Invoke();
            yield return Pulse(Key.Escape);
            Assert.That(flow.Options.IsOpen && !flow.Options.IsConfirming, Is.True, "First Esc leaves the confirmation.");
            yield return Pulse(Key.Escape);
            Assert.That(flow.Options.IsOpen, Is.False);
            Assert.That(flow.State, Is.EqualTo(LevelFlowState.Paused));
            Assert.That(flow.ResumeButton.gameObject.activeInHierarchy, Is.True);
            flow.OptionsButton.onClick.Invoke();
            flow.Resume();
            yield return Frames(5);
            Assert.That(flow.Options.IsOpen, Is.False, "Resuming closes the options.");
            Assert.That(flow.State, Is.EqualTo(LevelFlowState.Playing));
        }

        [UnityTest]
        public IEnumerator EchoHudFollowsTheRecorderAndHidesTheControlsWithH()
        {
            yield return Load(LevelScene);
            var flow = Object.FindFirstObjectByType<LevelFlow>();
            var hud = flow.Hud;
            var recorder = Object.FindFirstObjectByType<EchoRecorder>();
            Assert.That(hud.StateLabel.text, Is.EqualTo(EchoHud.ReadyToRecordText));
            Assert.That(hud.IsBarVisible, Is.False);
            Assert.That(hud.AreControlsVisible, Is.True);

            yield return Pulse(Key.Q);
            yield return Frames(150); // 2.5 s at the fixed 60 fps of these checks
            Assert.That(hud.StateLabel.text, Is.EqualTo(EchoHud.RecordingText));
            Assert.That(hud.IsBarVisible, Is.True);
            Assert.That(hud.Progress, Is.InRange(0.45f, 0.6f));
            var progress = hud.Progress;
            yield return Pulse(Key.Escape);
            Assert.That(hud.IsVisible, Is.False, "Hidden behind the pause menu.");
            yield return Frames(120);
            Assert.That(hud.Progress, Is.EqualTo(progress).Within(0.02f), "Paused time does not fill the bar.");
            yield return Pulse(Key.Escape);
            yield return Frames(200);
            Assert.That(recorder.State, Is.EqualTo(EchoState.Ready), "The five-second limit.");
            Assert.That(hud.StateLabel.text, Is.EqualTo(EchoHud.ReadyToPlayText));
            Assert.That(hud.IsBarVisible, Is.False);
            yield return Pulse(Key.E);
            Assert.That(hud.StateLabel.text, Is.EqualTo(EchoHud.PlayingText));

            yield return Pulse(Key.H);
            Assert.That(hud.AreControlsVisible, Is.False);
            Assert.That(GameSettings.ShowControls, Is.False, "Remembered.");
            yield return Pulse(Key.H);
            Assert.That(hud.AreControlsVisible, Is.True);
        }
    }
}

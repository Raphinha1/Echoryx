using NUnit.Framework;
using UnityEngine;

namespace Echoryx.Echo.Tests
{
    public sealed class EchoCaptureTests
    {
        [Test]
        public void StartStopAndReplaceRespectStates()
        {
            var capture = new EchoCapture();
            Assert.That(capture.State, Is.EqualTo(EchoState.Available));
            Assert.That(capture.Begin(10d, Vector3.zero, Quaternion.identity), Is.True);
            Assert.That(capture.State, Is.EqualTo(EchoState.Recording));
            Assert.That(capture.Begin(10.1d, Vector3.one, Quaternion.identity), Is.False);
            Assert.That(capture.Stop(10.2d, Vector3.one, Quaternion.identity), Is.True);
            Assert.That(capture.State, Is.EqualTo(EchoState.Ready));
            Assert.That(capture.Stop(10.3d, Vector3.one, Quaternion.identity), Is.False);

            var previous = capture.ReadyRecording;
            Assert.That(capture.Begin(20d, Vector3.zero, Quaternion.identity), Is.True);
            Assert.That(capture.ReadyRecording, Is.Null);
            capture.Stop(20.1d, Vector3.right, Quaternion.identity);
            Assert.That(previous.Duration, Is.EqualTo(0.2d).Within(1e-6));
            Assert.That(previous[previous.SampleCount - 1].Position, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void RecordingAutomaticallyEndsAtFiveSeconds()
        {
            var capture = new EchoCapture(5f, 20);
            capture.Begin(0d, Vector3.zero, Quaternion.identity);
            Assert.That(capture.Advance(5.2d, new Vector3(5.2f, 0f, 0f), Quaternion.identity), Is.True);
            Assert.That(capture.State, Is.EqualTo(EchoState.Ready));
            Assert.That(capture.ReadyRecording.Duration, Is.EqualTo(5d));
            Assert.That(capture.ReadyRecording.SampleCount, Is.EqualTo(101));
            Assert.That(capture.ReadyRecording[100].Time, Is.EqualTo(5d).Within(1e-8));
            Assert.That(capture.ReadyRecording[100].Position.x, Is.EqualTo(5f).Within(1e-4));
        }

        [Test]
        public void IrregularFramesProduceOrderedScheduledSamplesAndFinalPose()
        {
            var capture = new EchoCapture(5f, 10);
            capture.Begin(0d, Vector3.zero, Quaternion.identity);
            capture.Advance(0.03d, new Vector3(0.3f, 0f, 0f), Quaternion.identity);
            capture.Advance(0.11d, new Vector3(1.1f, 0f, 0f), Quaternion.identity);
            capture.Stop(0.26d, new Vector3(2.6f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));

            var result = capture.ReadyRecording;
            Assert.That(result.SampleCount, Is.EqualTo(4));
            Assert.That(result[0].Time, Is.EqualTo(0d));
            Assert.That(result[1].Time, Is.EqualTo(0.1d).Within(1e-8));
            Assert.That(result[2].Time, Is.EqualTo(0.2d).Within(1e-8));
            Assert.That(result[3].Time, Is.EqualTo(0.26d).Within(1e-8));
            for (var i = 1; i < result.SampleCount; i++)
                Assert.That(result[i].Time, Is.GreaterThan(result[i - 1].Time));
            Assert.That(result[1].Position.x, Is.EqualTo(1f).Within(1e-4));
            Assert.That(result[2].Position.x, Is.EqualTo(2f).Within(1e-4));
            Assert.That(result[3].Position.x, Is.EqualTo(2.6f).Within(1e-4));
            Assert.That(Quaternion.Angle(result[3].Rotation, Quaternion.Euler(0f, 90f, 0f)),
                Is.LessThan(0.01f));
        }

        [Test]
        public void CancelClearsBothActiveAndCompletedRecordings()
        {
            var capture = new EchoCapture();
            capture.Begin(0d, Vector3.zero, Quaternion.identity);
            Assert.That(capture.Cancel(), Is.True);
            Assert.That(capture.State, Is.EqualTo(EchoState.Available));
            Assert.That(capture.SampleCount, Is.Zero);
            Assert.That(capture.ReadyRecording, Is.Null);

            capture.Begin(1d, Vector3.zero, Quaternion.identity);
            capture.Stop(1.1d, Vector3.one, Quaternion.identity);
            Assert.That(capture.Cancel(), Is.True);
            Assert.That(capture.State, Is.EqualTo(EchoState.Available));
            Assert.That(capture.ReadyRecording, Is.Null);
            Assert.That(capture.Cancel(), Is.False);
        }

        [Test]
        public void PlaybackIsExclusiveAndCompletionClearsTheRecording()
        {
            var capture = new EchoCapture();
            Assert.That(capture.TryBeginPlayback(), Is.False);
            capture.Begin(0d, Vector3.zero, Quaternion.identity);
            Assert.That(capture.TryBeginPlayback(), Is.False);
            capture.Stop(0.2d, Vector3.right, Quaternion.identity);

            Assert.That(capture.TryBeginPlayback(), Is.True);
            Assert.That(capture.State, Is.EqualTo(EchoState.Playing));
            Assert.That(capture.TryBeginPlayback(), Is.False);
            Assert.That(capture.Begin(1d, Vector3.zero, Quaternion.identity), Is.False);
            Assert.That(capture.CompletePlayback(), Is.True);
            Assert.That(capture.State, Is.EqualTo(EchoState.Available));
            Assert.That(capture.ReadyRecording, Is.Null);
            Assert.That(capture.SampleCount, Is.Zero);
            Assert.That(capture.CompletePlayback(), Is.False);
        }

        [Test]
        public void CancelDuringPlaybackAllowsANewRecording()
        {
            var capture = new EchoCapture();
            capture.Begin(0d, Vector3.zero, Quaternion.identity);
            capture.Stop(0.1d, Vector3.right, Quaternion.identity);
            capture.TryBeginPlayback();
            Assert.That(capture.Cancel(), Is.True);
            Assert.That(capture.State, Is.EqualTo(EchoState.Available));
            Assert.That(capture.ReadyRecording, Is.Null);
            Assert.That(capture.Begin(1d, Vector3.up, Quaternion.identity), Is.True);
        }

        [Test]
        public void InterpolationUsesElapsedTimeAndHandlesOneSample()
        {
            var capture = new EchoCapture(5f, 10);
            capture.Begin(0d, new Vector3(2f, 0f, 0f), Quaternion.identity);
            capture.Stop(0.2d, new Vector3(6f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));
            var recording = capture.ReadyRecording;

            Assert.That(recording.TryEvaluate(0.05d, out var first), Is.True);
            Assert.That(first.Position.x, Is.EqualTo(3f).Within(1e-4));
            Assert.That(Quaternion.Angle(first.Rotation, Quaternion.Euler(0f, 22.5f, 0f)),
                Is.LessThan(0.1f));
            Assert.That(recording.TryEvaluate(0.15d, out var second), Is.True);
            Assert.That(second.Position.x, Is.EqualTo(5f).Within(1e-4));
            Assert.That(recording.TryEvaluate(-1d, out var before), Is.True);
            Assert.That(before.Position.x, Is.EqualTo(2f));
            Assert.That(recording.TryEvaluate(10d, out var after), Is.True);
            Assert.That(after.Position.x, Is.EqualTo(6f));

            capture.Begin(1d, Vector3.up, Quaternion.identity);
            capture.Stop(1d, Vector3.up, Quaternion.identity);
            Assert.That(capture.ReadyRecording.SampleCount, Is.EqualTo(1));
            Assert.That(capture.ReadyRecording.TryEvaluate(10d, out var single), Is.True);
            Assert.That(single.Position, Is.EqualTo(Vector3.up));
            Assert.That(capture.TryBeginPlayback(), Is.True);
            Assert.That(capture.CompletePlayback(), Is.True);
        }
    }
}

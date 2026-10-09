using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Echoryx.Game.VisualLabs;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Echoryx.Game.PlayTests
{
    public sealed class SuperSoldierLocomotionPlayChecks
    {
        private LocomotionAnimationPreview preview;
        private Animator animator;
        private Vector3 origin;
        private Quaternion rotation;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Echoryx/Scenes/SuperSoldierLocomotionLab.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("The isolated locomotion lab is loaded through Unity Editor.");
#endif
            Time.captureDeltaTime = 1f / 60f;
            yield return null;
            preview = GameObject.Find("SuperSoldier Locomotion Preview").GetComponent<LocomotionAnimationPreview>();
            preview.SetPreviewParameters(0f, true, 0f);
            animator = preview.PreviewAnimator;
            origin = animator.transform.position;
            rotation = animator.transform.rotation;
            Assert.That(animator.avatar.isValid && animator.avatar.isHuman, Is.True);
            Assert.That(animator.applyRootMotion, Is.False);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.captureDeltaTime = 0f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator SixClipsRetargetWithMovingBonesAndNoRootOrAccumulatedBodyDrift()
        {
            var clips = animator.runtimeAnimatorController.animationClips.Distinct().ToArray();
            Assert.That(clips.Length, Is.EqualTo(6));
            preview.enabled = false;
            animator.runtimeAnimatorController = null;
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            var leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            var bones = animator.GetComponentsInChildren<SkinnedMeshRenderer>()
                .SelectMany(r => r.bones).Where(b => b != null).Distinct().ToArray();
            var results = new List<PoseReport>();
            foreach (var name in new[] { "IdleLoop", "Walk", "Run", "Jump", "Fall", "Land" })
            {
                var clip = clips.Single(c => c.name == name);
                Assert.That(clip.humanMotion, Is.True, name);
                var graph = PlayableGraph.Create("S05-B3 retarget check " + name);
                try
                {
                    graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    var playable = AnimationClipPlayable.Create(graph, clip);
                    playable.SetApplyFootIK(name == "IdleLoop" || name == "Walk" || name == "Run" || name == "Land");
                    AnimationPlayableOutput.Create(graph, "Target SuperSoldier Avatar", animator).SetSourcePlayable(playable);
                    graph.Play();
                    graph.Evaluate(0f);
                    var startBones = bones.Select(b => b.localRotation).ToArray();
                    var initialHip = hips.position;
                    var report = new PoseReport { clip = name, seconds = clip.length,
                        minFootY = float.PositiveInfinity, maxFootY = float.NegativeInfinity };
                    var count = Mathf.CeilToInt(clip.length * (clip.isLooping ? 3f : 1f) * 60f);
                    for (var frame = 0; frame < count; frame++)
                    {
                        graph.Evaluate(1f / 60f);
                        var drift = hips.position - initialHip;
                        drift.y = 0f;
                        report.maxHipHorizontalDrift = Mathf.Max(report.maxHipHorizontalDrift, drift.magnitude);
                        report.minFootY = Mathf.Min(report.minFootY, leftFoot.position.y, rightFoot.position.y);
                        report.maxFootY = Mathf.Max(report.maxFootY, leftFoot.position.y, rightFoot.position.y);
                        report.maxRootDrift = Mathf.Max(report.maxRootDrift, Vector3.Distance(origin, animator.transform.position));
                        for (var bone = 0; bone < bones.Length; bone++)
                        {
                            AssertFinite(bones[bone].position, name);
                            report.maxBonePoseChangeDegrees = Mathf.Max(report.maxBonePoseChangeDegrees,
                                Quaternion.Angle(startBones[bone], bones[bone].localRotation));
                        }
                        Assert.That(Quaternion.Angle(rotation, animator.transform.rotation), Is.LessThan(0.001f), name + " root heading");
                        if (frame == Mathf.FloorToInt(clip.length * 0.4f * 60f))
                        {
                            MeasureSkin(report);
                            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null) Capture(name);
                        }
                        yield return null;
                    }
                    Assert.That(report.maxRootDrift, Is.LessThan(0.0001f), name + " root translation");
                    Assert.That(report.maxHipHorizontalDrift, Is.LessThan(0.35f), name + " must not keep the source travel inside its hips");
                    Assert.That(report.maxBonePoseChangeDegrees, Is.GreaterThan(0.01f), name + " must animate the target skeleton");
                    results.Add(report);
                }
                finally { graph.Destroy(); }
            }
            Directory.CreateDirectory("docs");
            File.WriteAllText("docs/supersoldier-locomotion-play-report.json",
                JsonUtility.ToJson(new PlayReport { unity = Application.unityVersion, samplesPerSecond = 60, poses = results.ToArray() }, true) + "\n");
        }

        [UnityTest]
        public IEnumerator BlendAndAirTransitionsThenAutomaticDemoVisitAllPhases()
        {
            yield return Frames(20);
            AssertState("Locomotion");
            AssertClip("IdleLoop");
            preview.SetPreviewParameters(preview.WalkPreviewSpeed, true, 0f);
            yield return Frames(60);
            AssertClip("Walk");
            preview.SetPreviewParameters((preview.WalkPreviewSpeed + preview.RunPreviewSpeed) * 0.5f, true, 0f);
            yield return Frames(20);
            var mix = animator.GetCurrentAnimatorClipInfo(0);
            Assert.That(mix.Any(c => c.clip.name == "Walk" && c.weight > 0.1f), Is.True);
            Assert.That(mix.Any(c => c.clip.name == "Run" && c.weight > 0.1f), Is.True);
            preview.SetPreviewParameters(preview.RunPreviewSpeed, true, 0f);
            yield return Frames(60);
            AssertClip("Run");
            preview.SetPreviewParameters(preview.WalkPreviewSpeed, true, 0f);
            yield return Frames(60);
            AssertClip("Walk");
            preview.SetPreviewParameters(0f, true, 0f);
            yield return Frames(60);
            AssertClip("IdleLoop");
            preview.SetPreviewParameters(0f, false, 4f);
            yield return Frames(20);
            AssertState("Jump");
            preview.SetPreviewParameters(0f, false, -4f);
            yield return Frames(20);
            AssertState("Fall");
            yield return Frames(120);
            AssertState("Fall");
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.GreaterThan(1f), "Fall loops while airborne.");
            preview.SetPreviewParameters(0f, true, 0f);
            yield return Frames(15);
            AssertState("Land");
            var land = animator.runtimeAnimatorController.animationClips.Single(c => c.name == "Land");
            yield return Frames(Mathf.CeilToInt((land.length + 0.3f) * 60f));
            AssertState("Locomotion");
            AssertClip("IdleLoop");
            preview.SetPreviewParameters(0f, false, -1f);
            yield return Frames(20);
            AssertState("Fall");
            preview.SetPreviewParameters(0f, true, 0f);
            yield return Frames(Mathf.CeilToInt((land.length + 0.5f) * 60f));
            preview.SetAutomaticDemo(true);
            var phases = new HashSet<string>();
            var states = new HashSet<string>();
            for (var frame = 0; frame < 16 * 60; frame++)
            {
                phases.Add(preview.DemoPhase);
                foreach (var state in new[] { "Locomotion", "Jump", "Fall", "Land" })
                    if (animator.GetCurrentAnimatorStateInfo(0).IsName(state)) states.Add(state);
                Assert.That(Vector3.Distance(origin, animator.transform.position), Is.LessThan(0.0001f));
                yield return null;
            }
            Assert.That(phases, Is.EquivalentTo(new[] { "Idle", "Walk", "Run", "Jump", "Fall", "Land" }));
            Assert.That(states, Is.EquivalentTo(new[] { "Locomotion", "Jump", "Fall", "Land" }));
        }

        [UnityTest]
        public IEnumerator AutomaticJumpShowsTheWholeClipThenTransitionsThroughFallAndLandOnce()
        {
            var jumpClip = animator.runtimeAnimatorController.animationClips.Single(c => c.name == "Jump");
            Assert.That(jumpClip.humanMotion, Is.True);
            Assert.That(preview.JumpPreviewSeconds, Is.EqualTo(jumpClip.length).Within(0.002f));

            preview.SetAutomaticDemo(true);
            var jumpPhaseFrames = 0;
            var jumpEntryFrame = -1;
            var fallEntryFrame = -1;
            var jumpEntries = 0;
            var fallEntries = 0;
            var landEntries = 0;
            var wasJump = false;
            var wasFall = false;
            var wasLand = false;
            var jumpNormalizedTimeAtExit = 0f;
            var maxJumpClipWeight = 0f;
            var leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var initialFootRotation = leftFoot.localRotation;
            var maxFootPoseChangeDegrees = 0f;
            for (var frame = 0; frame < 15 * 60; frame++)
            {
                if (preview.DemoPhase == "Jump") jumpPhaseFrames++;

                var state = animator.GetCurrentAnimatorStateInfo(0);
                var isJump = state.IsName("Jump");
                if (isJump && !wasJump) jumpEntries++;
                wasJump = isJump;
                if (state.IsName("Jump"))
                {
                    if (jumpEntryFrame < 0) jumpEntryFrame = frame;
                    jumpNormalizedTimeAtExit = state.normalizedTime;
                    maxJumpClipWeight = Mathf.Max(maxJumpClipWeight, animator.GetCurrentAnimatorClipInfo(0)
                        .Where(info => info.clip == jumpClip).Select(info => info.weight).DefaultIfEmpty(0f).Max());
                    maxFootPoseChangeDegrees = Mathf.Max(maxFootPoseChangeDegrees,
                        Quaternion.Angle(initialFootRotation, leftFoot.localRotation));
                }
                var isFall = state.IsName("Fall");
                if (isFall && !wasFall)
                {
                    fallEntries++;
                    if (jumpEntryFrame >= 0 && fallEntryFrame < 0) fallEntryFrame = frame;
                }
                wasFall = isFall;

                var isLand = state.IsName("Land");
                if (isLand && !wasLand) landEntries++;
                wasLand = isLand;

                if (fallEntryFrame >= 0 && landEntries > 0 && state.IsName("Locomotion")) break;
                yield return null;
            }

            var phaseSeconds = jumpPhaseFrames / 60f;
            var stateSeconds = (fallEntryFrame - jumpEntryFrame) / 60f;
            TestContext.WriteLine($"Jump clip={jumpClip.length:F4}s; automatic phase={phaseSeconds:F4}s; Animator Jump state={stateSeconds:F4}s; normalized at exit={jumpNormalizedTimeAtExit:F3}; max Jump clip weight={maxJumpClipWeight:F3}; max left-foot pose change={maxFootPoseChangeDegrees:F1}deg; entries Jump/Fall/Land={jumpEntries}/{fallEntries}/{landEntries}.");
            Assert.That(phaseSeconds, Is.EqualTo(jumpClip.length).Within(2f / 60f));
            Assert.That(jumpEntryFrame, Is.GreaterThanOrEqualTo(0));
            Assert.That(fallEntryFrame, Is.GreaterThan(jumpEntryFrame));
            Assert.That(stateSeconds, Is.EqualTo(jumpClip.length).Within(3f / 60f));
            Assert.That(jumpNormalizedTimeAtExit, Is.GreaterThanOrEqualTo(1f), "Jump reaches its final pose before Fall.");
            Assert.That(maxJumpClipWeight, Is.GreaterThan(0.99f), "The Animator fully plays the Jump clip before leaving the state.");
            Assert.That(maxFootPoseChangeDegrees, Is.GreaterThan(1f), "The Jump animation visibly changes the retargeted pose.");
            Assert.That(jumpEntries, Is.EqualTo(1), "The automatic sequence must not repeat Jump.");
            Assert.That(fallEntries, Is.EqualTo(1), "The automatic sequence must enter Fall once.");
            Assert.That(landEntries, Is.EqualTo(1), "Automatic presentation has a single landing.");
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"), Is.True);
        }

        [UnityTest]
        public IEnumerator IndividualJumpRatesAdvanceAccordingToClipTime()
        {
#if UNITY_EDITOR
            var clip = animator.runtimeAnimatorController.animationClips.Single(c => c.name == "Jump");
            var inspector = new UnityEditor.SerializedObject(preview);
            foreach (var rate in new[] { 1f, 0.5f, 0.25f })
            {
                preview.SetIndividualClip(LocomotionAnimationPreview.IndividualClip.Jump);
                inspector.Update();
                inspector.FindProperty("playbackRate").floatValue = rate;
                inspector.FindProperty("pauseClip").boolValue = false;
                inspector.ApplyModifiedPropertiesWithoutUndo();
                yield return Frames(2);
                inspector.Update();
                var start = inspector.FindProperty("normalizedClipTime").floatValue;
                yield return Frames(12);
                inspector.Update();
                var actualAdvance = inspector.FindProperty("normalizedClipTime").floatValue - start;
                var expectedAdvance = 0.2f * rate / clip.length;
                TestContext.WriteLine($"Jump individual rate={rate:F2}x; normalized advance over 0.2s={actualAdvance:F3}; clip duration={clip.length / rate:F3}s.");
                Assert.That(actualAdvance, Is.EqualTo(expectedAdvance).Within(0.035f), $"Jump preview rate {rate:F2}x");
                Assert.That(preview.IsIndividualPreview, Is.True);
                Assert.That(animator.runtimeAnimatorController, Is.Null);
            }
#else
            Assert.Ignore("The playback-rate controls are Editor-only lab diagnostics.");
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator JumpingUpUsesAnInPlaceReferenceWithoutRootDrift()
        {
            var controller = animator.runtimeAnimatorController;
            var clip = controller.animationClips.Single(c => c.name == "Jump");
            preview.enabled = false;
            animator.runtimeAnimatorController = null;
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            var initialRootPosition = animator.transform.position;
            var graph = PlayableGraph.Create("Jumping Up pose inspection");
            try
            {
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var playable = AnimationClipPlayable.Create(graph, clip);
                AnimationPlayableOutput.Create(graph, "Jumping Up on SuperSoldier", animator).SetSourcePlayable(playable);
                graph.Play();

                var startHipsY = 0f;
                var peakHipsY = float.NegativeInfinity;
                var minHipsY = float.PositiveInfinity;
                var endHipsY = 0f;
                var samples = Mathf.CeilToInt(clip.length * 60f);
                for (var i = 0; i <= samples; i++)
                {
                    var time = Mathf.Min(i / 60f, clip.length - 0.0001f);
                    playable.SetTime(time);
                    graph.Evaluate(0f);
                    var y = hips.position.y;
                    if (i == 0) startHipsY = y;
                    peakHipsY = Mathf.Max(peakHipsY, y);
                    minHipsY = Mathf.Min(minHipsY, y);
                    endHipsY = y;
                    Assert.That(Vector3.Distance(initialRootPosition, animator.transform.position), Is.LessThan(0.0001f),
                        "The clip must not translate the Animator root.");
                    if (i % 10 == 0 || i == samples)
                        TestContext.WriteLine($"Jumping Up sample t={time:F3}s hipsY={y:F3}m");
                    yield return null;
                }
                TestContext.WriteLine($"Jumping Up in-place hips start={startHipsY:F3}m peak={peakHipsY:F3}m end={endHipsY:F3}m; extracted average Y speed={clip.averageSpeed.y:F3}m/s.");
                Assert.That(minHipsY, Is.InRange(0.53f, 0.59f), "Retain the authored grounded crouch, which full Y extraction lost.");
                Assert.That(peakHipsY, Is.LessThan(1.01f), "Do not bake the source's 0.61m ascent back into the pose.");
                Assert.That(endHipsY, Is.InRange(0.85f, 0.9f), "End height must remain compatible with Fall, not Feet or COM centering.");
                Assert.That(Mathf.Abs(clip.averageSpeed.y), Is.LessThan(0.001f), "The generated motion contains no vertical gameplay trajectory.");
            }
            finally
            {
                graph.Destroy();
                animator.runtimeAnimatorController = controller;
                animator.Rebind();
                animator.applyRootMotion = false;
                preview.enabled = true;
            }
        }

        [UnityTest]
        public IEnumerator RepeatedLandingsAreSingleAndInterruptibleAt30And120Hz()
        {
            foreach (var fps in new[] { 30, 120 })
            {
                Time.captureDeltaTime = 1f / fps;
                preview.SetPreviewParameters(0f, true, 0f);
                yield return Frames(fps);
                for (var attempt = 0; attempt < 3; attempt++)
                {
                    preview.SetPreviewParameters(0f, false, 3f);
                    yield return Frames(fps / 3);
                    AssertState("Jump");
                    preview.SetPreviewParameters(0f, false, -3f);
                    // Grounding during the Jump->Fall blend must interrupt it safely.
                    yield return Frames(1);
                    preview.SetPreviewParameters(0f, true, 0f);
                    var landEntries = 0;
                    var wasLand = false;
                    for (var frame = 0; frame < 3 * fps; frame++)
                    {
                        var isLand = animator.GetCurrentAnimatorStateInfo(0).IsName("Land");
                        if (isLand && !wasLand) landEntries++;
                        wasLand = isLand;
                        Assert.That(Vector3.Distance(origin, animator.transform.position), Is.LessThan(0.0001f));
                        yield return null;
                    }
                    Assert.That(landEntries, Is.EqualTo(1), $"Landing {attempt} at {fps}Hz must occur once, even while Grounded stays true.");
                    AssertState("Locomotion");
                }
                preview.SetPreviewParameters(0f, false, -3f);
                yield return Frames(fps / 3);
                preview.SetPreviewParameters(0f, true, 0f);
                yield return Frames(fps / 4);
                AssertState("Land");
                preview.SetPreviewParameters(0f, false, -3f);
                yield return Frames(fps / 3);
                AssertState("Fall");
            }
        }

        [UnityTest]
        public IEnumerator IndividualPreviewHasExclusiveControlAndRestoresController()
        {
            var controller = animator.runtimeAnimatorController;
            foreach (var clip in new[] { LocomotionAnimationPreview.IndividualClip.Walk, LocomotionAnimationPreview.IndividualClip.Run,
                LocomotionAnimationPreview.IndividualClip.Jump, LocomotionAnimationPreview.IndividualClip.Land })
            {
                preview.SetIndividualClip(clip);
                yield return Frames(30);
                Assert.That(preview.IsIndividualPreview, Is.True);
                Assert.That(animator.runtimeAnimatorController, Is.Null);
                Assert.That(preview.DemoPhase, Is.EqualTo(clip.ToString()));
                Assert.That(Vector3.Distance(origin, animator.transform.position), Is.LessThan(0.0001f));
            }
#if UNITY_EDITOR
            preview.SetIndividualClip(LocomotionAnimationPreview.IndividualClip.Jump);
            yield return Frames(2);
            var inspector = new UnityEditor.SerializedObject(preview);
            inspector.FindProperty("pauseClip").boolValue = true;
            inspector.FindProperty("normalizedClipTime").floatValue = 0.25f;
            inspector.ApplyModifiedPropertiesWithoutUndo();
            yield return Frames(2);
            var foot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var quarterPose = foot.localRotation;
            yield return Frames(30);
            Assert.That(Quaternion.Angle(quarterPose, foot.localRotation), Is.LessThan(0.01f), "Paused pose stays fixed.");
            inspector.Update();
            inspector.FindProperty("normalizedClipTime").floatValue = 0.75f;
            inspector.ApplyModifiedPropertiesWithoutUndo();
            yield return Frames(2);
            Assert.That(Quaternion.Angle(quarterPose, foot.localRotation), Is.GreaterThan(1f), "Inspector scrub changes the pose.");
#endif
            preview.SetPreviewParameters(preview.WalkPreviewSpeed, true, 0f);
            yield return Frames(60);
            Assert.That(preview.IsIndividualPreview, Is.False);
            Assert.That(animator.runtimeAnimatorController, Is.SameAs(controller));
            AssertClip("Walk");
            // The setter stores targets: it must not bypass Inspector/automatic smoothing.
            var previousSpeed = animator.GetFloat("Speed");
            preview.SetPreviewParameters(preview.RunPreviewSpeed, true, 0f);
            Assert.That(animator.GetFloat("Speed"), Is.EqualTo(previousSpeed));
            yield return Frames(1);
            Assert.That(animator.GetFloat("Speed"), Is.GreaterThan(previousSpeed).And.LessThan(preview.RunPreviewSpeed));
            yield return Frames(60);
            AssertClip("Run");
            preview.SetIndividualClip(LocomotionAnimationPreview.IndividualClip.Fall);
            yield return Frames(2);
            preview.enabled = false;
            Assert.That(preview.IsIndividualPreview, Is.False);
            Assert.That(animator.runtimeAnimatorController, Is.SameAs(controller));
            preview.enabled = true;
            preview.SetAutomaticDemo(true);
            yield return Frames(60);
            Assert.That(preview.DemoPhase, Is.EqualTo("Idle"));
            AssertClip("IdleLoop");
        }

        private void AssertState(string state) => Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName(state), Is.True, state);
        private void AssertClip(string name) => Assert.That(animator.GetCurrentAnimatorClipInfo(0).Any(c => c.clip.name == name && c.weight > 0.99f), Is.True, name);
        private static IEnumerator Frames(int count) { for (var i = 0; i < count; i++) yield return null; }
        private static void AssertFinite(Vector3 value, string clip)
        {
            Assert.That(float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z)
                || float.IsInfinity(value.x) || float.IsInfinity(value.y) || float.IsInfinity(value.z), Is.False, clip);
        }

        private static void Capture(string name)
        {
            var camera = Camera.main;
            var target = new RenderTexture(1600, 1000, 24);
            var pixels = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                target.Create();
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0);
                pixels.Apply();
                Directory.CreateDirectory("docs/images");
                File.WriteAllBytes("docs/images/s05-b3-" + name.ToLowerInvariant() + ".png", pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(pixels);
            }
        }

        private void MeasureSkin(PoseReport report)
        {
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            report.skinMinYAtCapture = float.PositiveInfinity;
            report.skinMaxYAtCapture = float.NegativeInfinity;
            try
            {
                foreach (var renderer in animator.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    // Compensate each imported renderer's scale before converting its local snapshot to world space.
                    renderer.BakeMesh(mesh, true);
                    mesh.GetVertices(vertices);
                    foreach (var vertex in vertices)
                    {
                        var y = renderer.transform.TransformPoint(vertex).y;
                        report.skinMinYAtCapture = Mathf.Min(report.skinMinYAtCapture, y);
                        report.skinMaxYAtCapture = Mathf.Max(report.skinMaxYAtCapture, y);
                    }
                }
            }
            finally { UnityEngine.Object.Destroy(mesh); }
        }

        [Serializable] private sealed class PlayReport { public string unity; public int samplesPerSecond; public PoseReport[] poses; }
        [Serializable] private sealed class PoseReport
        {
            public string clip;
            public float seconds, minFootY, maxFootY, maxHipHorizontalDrift, maxRootDrift, maxBonePoseChangeDegrees,
                skinMinYAtCapture, skinMaxYAtCapture;
        }
    }
}

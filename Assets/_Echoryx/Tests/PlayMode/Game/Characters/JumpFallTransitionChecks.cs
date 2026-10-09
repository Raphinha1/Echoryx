using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Echoryx.Game.VisualLabs;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Echoryx.Game.PlayTests
{
    public sealed class JumpFallTransitionChecks
    {
        private static readonly HumanBodyBones[] MeasuredBones = {
            HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest,
            HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg,
            HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg };

        [Serializable] public sealed class BonePose
        {
            public string bone;
            public Vector3 localPosition, worldPosition, rootRelativePosition;
            public Quaternion localRotation, worldRotation;
        }
        [Serializable] public sealed class Sample
        {
            public float time, jumpWeight, fallWeight, currentNormalizedTime, nextNormalizedTime, transitionNormalizedTime;
            public string phase, currentState, nextState;
            public bool mixing;
            public BonePose[] bones;
        }
        [Serializable] public sealed class Cycle
        {
            public float rate, blendStart, blendEnd, pelvisBeforeY, pelvisAfterY, blendHeightChange;
            public float maxPelvisStep, maxBoneStepDegrees, maxWorldBoneStepDegrees, rootDrift;
            public int jumpEntries, fallEntries, landEntries;
            public Sample[] samples;
        }
        [Serializable] public sealed class Report
        {
            public string unity;
            public Sample jumpStart, jumpEnd, fallStart, fallBestMatch;
            public float bestFallNormalizedTime, endpointHeightDifference;
            public float[] endpointBoneAngles, endpointWorldBoneAngles;
            public Cycle[] cycles;
        }

        [UnityTest]
        public IEnumerator ActualDemoBlendsJumpIntoFallWithoutHeightOrOrientationSnap()
        {
            var cycles = new List<Cycle>();
            var report = new Report { unity = Application.unityVersion };
            try
            {
                foreach (var rate in new[] { 1f, 0.5f })
                {
                    EditorSceneManager.LoadSceneInPlayMode("Assets/_Echoryx/Scenes/SuperSoldierLocomotionLab.unity",
                        new LoadSceneParameters(LoadSceneMode.Single));
                    Time.captureDeltaTime = 1f / 60f;
                    yield return null;
                    var preview = UnityEngine.Object.FindFirstObjectByType<LocomotionAnimationPreview>();
                    var animator = preview.PreviewAnimator;
                    AnimatorOverrideController comparison = null;
                    if (Environment.GetEnvironmentVariable("ECHORYX_JUMP_RAW_COMPARISON") == "1")
                    {
                        // Diagnostic only: compare the input clip in memory, without writing the approved controller/importer.
                        var input = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/_Echoryx/Art/Animations/SuperSoldier/MixamoReplacement/Jumping Up.fbx")
                            .OfType<AnimationClip>().Single(c => c.name == "Jump");
                        comparison = new AnimatorOverrideController(animator.runtimeAnimatorController);
                        comparison[animator.runtimeAnimatorController.animationClips.Single(c => c.name == "Jump")] = input;
                        animator.runtimeAnimatorController = comparison;
                        animator.Rebind();
                    }
                    var origin = animator.transform.position;
                    var heading = animator.transform.rotation;
                    if (cycles.Count == 0) MeasureIndividualClips(animator, report);
                    preview.SetPlaybackRate(rate);
                    preview.SetAutomaticDemo(true);
                    var cycle = new Cycle { rate = rate, blendStart = -1f, blendEnd = -1f };
                    var samples = new List<Sample>();
                    var previousState = "";
                    var clock = 0f;
                    var afterFall = 0;
                    var captureFrame = 0;
                    var captureRoot = Environment.GetEnvironmentVariable("ECHORYX_TRANSITION_CAPTURE");
                    for (var frame = 0; frame < 16 * 60 / rate; frame++)
                    {
                        yield return null;
                        clock += Time.deltaTime * rate;
                        var sample = Read(animator, clock);
                        sample.phase = preview.DemoPhase;
                        if (!string.IsNullOrEmpty(captureRoot) && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null && clock >= 7.8f && clock <= 11.1f)
                            CaptureFrame(Path.Combine(captureRoot, "rate" + Mathf.RoundToInt(rate * 100f)), captureFrame++, animator.transform.position);
                        if (sample.currentState != previousState)
                        {
                            if (sample.currentState == "Jump") cycle.jumpEntries++;
                            if (sample.currentState == "Fall") cycle.fallEntries++;
                            if (sample.currentState == "Land") cycle.landEntries++;
                        }
                        previousState = sample.currentState;
                        if (sample.currentState == "Jump" && sample.nextState == "Fall")
                        {
                            if (cycle.blendStart < 0f)
                            {
                                cycle.blendStart = clock;
                                cycle.pelvisBeforeY = samples.Last().bones[0].worldPosition.y;
                            }
                        }
                        if (cycle.blendStart >= 0f && cycle.blendEnd < 0f && sample.currentState == "Fall" && !sample.mixing)
                        {
                            cycle.blendEnd = clock;
                            cycle.pelvisAfterY = sample.bones[0].worldPosition.y;
                            cycle.blendHeightChange = Mathf.Abs(cycle.pelvisAfterY - cycle.pelvisBeforeY);
                        }
                        if (sample.currentState == "Jump" && sample.currentNormalizedTime > 0.75f || sample.currentState == "Fall" && afterFall++ < 20 / rate)
                        {
                            if (samples.Count > 0 && cycle.blendStart >= 0f && (cycle.blendEnd < 0f || clock <= cycle.blendEnd))
                            {
                                var prior = samples.Last();
                                cycle.maxPelvisStep = Mathf.Max(cycle.maxPelvisStep,
                                    Vector3.Distance(prior.bones[0].worldPosition, sample.bones[0].worldPosition));
                                for (var b = 0; b < MeasuredBones.Length; b++)
                                {
                                    cycle.maxBoneStepDegrees = Mathf.Max(cycle.maxBoneStepDegrees,
                                        Quaternion.Angle(prior.bones[b].localRotation, sample.bones[b].localRotation));
                                    cycle.maxWorldBoneStepDegrees = Mathf.Max(cycle.maxWorldBoneStepDegrees,
                                        Quaternion.Angle(prior.bones[b].worldRotation, sample.bones[b].worldRotation));
                                }
                            }
                            samples.Add(sample);
                        }
                        cycle.rootDrift = Mathf.Max(cycle.rootDrift, Vector3.Distance(origin, animator.transform.position));
                        Assert.That(Quaternion.Angle(heading, animator.transform.rotation), Is.LessThan(0.001f));
                        Assert.That(animator.applyRootMotion, Is.False);
                        if (cycle.landEntries > 0 && sample.currentState == "Locomotion") break;
                    }
                    cycle.samples = samples.ToArray();
                    cycles.Add(cycle);
                    if (comparison != null)
                    {
                        animator.runtimeAnimatorController = comparison.runtimeAnimatorController;
                        animator.Rebind();
                        UnityEngine.Object.Destroy(comparison);
                    }
                    TestContext.WriteLine($"Transition {rate}x: pelvis {cycle.pelvisBeforeY:F4}->{cycle.pelvisAfterY:F4}m, delta={cycle.blendHeightChange:F4}m; step={cycle.maxPelvisStep:F4}m; bone step={cycle.maxBoneStepDegrees:F2}deg; time={cycle.blendStart:F4}->{cycle.blendEnd:F4}s; entries={cycle.jumpEntries}/{cycle.fallEntries}/{cycle.landEntries}");
                }
                report.cycles = cycles.ToArray();
                File.WriteAllText("/tmp/echoryx-transition-measurements.json", JsonUtility.ToJson(report, true) + "\n");
                foreach (var cycle in cycles)
                {
                    Assert.That(cycle.jumpEntries, Is.EqualTo(1));
                    Assert.That(cycle.fallEntries, Is.EqualTo(1));
                    Assert.That(cycle.landEntries, Is.EqualTo(1));
                    Assert.That(cycle.blendStart, Is.GreaterThan(0f));
                    Assert.That(cycle.blendEnd - cycle.blendStart, Is.InRange(0.09f, 0.22f), "No long blend hiding the mismatch.");
                    Assert.That(cycle.samples.Any(s => s.jumpWeight > 0.1f && s.fallWeight > 0.1f), Is.True, "Both motions actually contribute during the blend.");
                    Assert.That(cycle.blendHeightChange, Is.LessThan(0.18f), "Vertical reference must not reset by the baked ascent height.");
                    Assert.That(cycle.maxPelvisStep / cycle.rate, Is.LessThan(0.04f), "Pelvis continuity at 60 pose samples per second.");
                    Assert.That(cycle.maxBoneStepDegrees / cycle.rate, Is.LessThan(2.25f), "Limit joint angular change to 135deg/s at 60Hz.");
                    Assert.That(cycle.maxWorldBoneStepDegrees / cycle.rate, Is.LessThan(3f), "Torso/leg world orientation must not snap.");
                    Assert.That(cycle.rootDrift, Is.LessThan(0.0001f));
                }
            }
            finally { Time.captureDeltaTime = 0f; Time.timeScale = 1f; }
        }

        private static Sample Read(Animator animator, float time)
        {
            var current = animator.GetCurrentAnimatorStateInfo(0);
            var next = animator.GetNextAnimatorStateInfo(0);
            var infos = animator.GetCurrentAnimatorClipInfo(0).Concat(animator.GetNextAnimatorClipInfo(0));
            return new Sample { time = time, currentState = Name(current), nextState = animator.IsInTransition(0) ? Name(next) : "",
                mixing = animator.IsInTransition(0), currentNormalizedTime = current.normalizedTime, nextNormalizedTime = next.normalizedTime,
                transitionNormalizedTime = animator.IsInTransition(0) ? animator.GetAnimatorTransitionInfo(0).normalizedTime : 0f,
                jumpWeight = infos.Where(c => c.clip.name == "Jump").Sum(c => c.weight),
                fallWeight = infos.Where(c => c.clip.name == "Fall").Sum(c => c.weight),
                bones = MeasuredBones.Select(b => {
                    var bone = animator.GetBoneTransform(b);
                    return new BonePose { bone = b.ToString(), localPosition = bone.localPosition, worldPosition = bone.position,
                        rootRelativePosition = animator.transform.InverseTransformPoint(bone.position), localRotation = bone.localRotation, worldRotation = bone.rotation };
                }).ToArray() };
        }

        private static string Name(AnimatorStateInfo state) => new[] { "Locomotion", "Jump", "Fall", "Land" }.FirstOrDefault(state.IsName) ?? "";

        private static void CaptureFrame(string folder, int frame, Vector3 origin)
        {
            var camera = Camera.main;
            var position = camera.transform.position;
            var rotation = camera.transform.rotation;
            var fov = camera.fieldOfView;
            var target = new RenderTexture(320, 420, 24);
            var tile = new Texture2D(320, 420, TextureFormat.RGB24, false);
            var sheet = new Texture2D(640, 420, TextureFormat.RGB24, false);
            var active = RenderTexture.active;
            try
            {
                target.Create();
                for (var side = 0; side < 2; side++)
                {
                    camera.transform.position = origin + (side == 0 ? new Vector3(0f, 1.4f, -4.5f) : new Vector3(-4.3f, 1.4f, -1.2f));
                    camera.transform.LookAt(origin + Vector3.up * 1.2f);
                    camera.fieldOfView = 40f;
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                    RenderTexture.active = target;
                    tile.ReadPixels(new Rect(0, 0, 320, 420), 0, 0);
                    tile.Apply();
                    sheet.SetPixels(side * 320, 0, 320, 420, tile.GetPixels());
                }
                sheet.Apply();
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder, frame.ToString("D4") + ".png"), sheet.EncodeToPNG());
            }
            finally
            {
                camera.transform.SetPositionAndRotation(position, rotation);
                camera.fieldOfView = fov;
                RenderTexture.active = active;
                target.Release();
                UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(tile); UnityEngine.Object.Destroy(sheet);
            }
        }

        private static void MeasureIndividualClips(Animator animator, Report report)
        {
            var controller = animator.runtimeAnimatorController;
            var jump = controller.animationClips.Single(c => c.name == "Jump");
            var fall = controller.animationClips.Single(c => c.name == "Fall");
            animator.runtimeAnimatorController = null;
            var graph = PlayableGraph.Create("Jump/Fall endpoint comparison");
            try
            {
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var output = AnimationPlayableOutput.Create(graph, "Individual endpoint", animator);
                graph.Play();
                var playable = AnimationClipPlayable.Create(graph, jump);
                output.SetSourcePlayable(playable);
                graph.Evaluate(0f);
                report.jumpStart = Read(animator, 0f);
                playable.SetTime(jump.length - 0.0001f);
                graph.Evaluate(0f);
                report.jumpEnd = Read(animator, jump.length);
                var drop = AnimationClipPlayable.Create(graph, fall);
                output.SetSourcePlayable(drop);
                graph.Evaluate(0f);
                report.fallStart = Read(animator, 0f);
                report.endpointHeightDifference = report.jumpEnd.bones[0].worldPosition.y - report.fallStart.bones[0].worldPosition.y;
                report.endpointBoneAngles = report.jumpEnd.bones.Zip(report.fallStart.bones, (a, b) => Quaternion.Angle(a.localRotation, b.localRotation)).ToArray();
                report.endpointWorldBoneAngles = report.jumpEnd.bones.Zip(report.fallStart.bones, (a, b) => Quaternion.Angle(a.worldRotation, b.worldRotation)).ToArray();
                var best = float.PositiveInfinity;
                for (var i = 0; i < 42; i++)
                {
                    drop.SetTime(i / 42f * fall.length);
                    graph.Evaluate(0f);
                    var sample = Read(animator, i / 42f * fall.length);
                    var difference = report.jumpEnd.bones.Zip(sample.bones, (a, b) => Quaternion.Angle(a.localRotation, b.localRotation)).Sum();
                    if (difference < best) { best = difference; report.bestFallNormalizedTime = i / 42f; report.fallBestMatch = sample; }
                }
                TestContext.WriteLine($"Individual endpoints: Jump Y={report.jumpEnd.bones[0].worldPosition.y:F4}, Fall Y={report.fallStart.bones[0].worldPosition.y:F4}; angles={string.Join(", ", report.endpointBoneAngles.Select(a => a.ToString("F2")))}; best Fall phase={report.bestFallNormalizedTime:F3}");
            }
            finally { graph.Destroy(); animator.runtimeAnimatorController = controller; animator.Rebind(); animator.applyRootMotion = false; }
        }
    }
}

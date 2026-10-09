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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Echoryx.Game.PlayTests
{
    public sealed class JumpPreparationChecks
    {
        [Serializable] private sealed class Frame
        {
            public float time, soleY;
            public Vector3 gameObject, visualRoot, pelvis, leftFoot, rightFoot;
        }
        [Serializable] private sealed class Cycle { public string mode; public Frame[] frames; }
        [Serializable] private sealed class Report { public string unity; public Cycle[] cycles; }

        [UnityTest]
        public IEnumerator PreparationKeepsSolesOnFloorInIndividualAndAutomaticPreview()
        {
            var cycles = new List<Cycle>();
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            try
            {
                foreach (var mode in new[] { "Individual", "Automatic" })
                {
                    EditorSceneManager.LoadSceneInPlayMode("Assets/_Echoryx/Scenes/SuperSoldierLocomotionLab.unity",
                        new LoadSceneParameters(LoadSceneMode.Single));
                    Time.captureDeltaTime = 1f / 60f;
                    yield return null;
                    var preview = UnityEngine.Object.FindFirstObjectByType<LocomotionAnimationPreview>();
                    var animator = preview.PreviewAnimator;
                    var origin = animator.transform.position;
                    var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
                    var left = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                    var right = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                    var renderers = animator.GetComponentsInChildren<SkinnedMeshRenderer>();
                    var frames = new List<Frame>();
                    if (mode == "Individual") preview.SetIndividualClip(LocomotionAnimationPreview.IndividualClip.Jump);
                    else preview.SetAutomaticDemo(true);
                    var clip = mode == "Individual" ? 0f : -1f;
                    for (var i = 0; i < 11 * 60; i++)
                    {
                        yield return null;
                        if (mode == "Automatic")
                        {
                            var state = animator.GetCurrentAnimatorStateInfo(0);
                            var next = animator.GetNextAnimatorStateInfo(0);
                            if (!state.IsName("Jump") && !(animator.IsInTransition(0) && next.IsName("Jump"))) continue;
                            clip = (state.IsName("Jump") ? state.normalizedTime : next.normalizedTime) * preview.JumpPreviewSeconds;
                        }
                        else clip += Time.deltaTime;
                        if (clip > preview.JumpPreviewSeconds + 0.001f) break;
                        var min = float.PositiveInfinity;
                        foreach (var skin in renderers)
                        {
                            skin.BakeMesh(mesh, true);
                            mesh.GetVertices(vertices);
                            foreach (var v in vertices) min = Mathf.Min(min, skin.transform.TransformPoint(v).y);
                        }
                        frames.Add(new Frame { time = clip, soleY = min, gameObject = animator.transform.position,
                            visualRoot = hips.parent.position, pelvis = hips.position, leftFoot = left.position, rightFoot = right.position });
                        Assert.That(Vector3.Distance(origin, animator.transform.position), Is.LessThan(0.0001f));
                        Assert.That(animator.applyRootMotion, Is.False);
                    }
                    cycles.Add(new Cycle { mode = mode, frames = frames.ToArray() });
                    foreach (var f in frames.Where(f => Mathf.Abs(f.time * 30f - Mathf.Round(f.time * 30f)) < 0.03f))
                        TestContext.WriteLine($"{mode} t={f.time:F3}, root={f.gameObject.y:F3}, visual={f.visualRoot.y:F3}, hips={f.pelvis.y:F3}, feet={f.leftFoot.y:F3}/{f.rightFoot.y:F3}, sole={f.soleY:F3}");
                }
                File.WriteAllText("/tmp/echoryx-jump-preparation.json", JsonUtility.ToJson(new Report { unity = Application.unityVersion, cycles = cycles.ToArray() }, true));
                foreach (var cycle in cycles)
                {
                    var preparation = cycle.frames.Where(f => f.time >= 0.05f && f.time <= 0.4f).ToArray();
                    Assert.That(preparation.Length, Is.GreaterThan(10));
                    Assert.That(preparation.Max(f => f.soleY), Is.LessThan(0.04f), cycle.mode + ": premature sole lift during grounded preparation.");
                    Assert.That(preparation.Min(f => f.soleY), Is.GreaterThan(-0.025f), cycle.mode + ": excessive sole penetration.");
                    // Automatic entry initially blends an approved airborne Run pose; compare against the isolated grounded reference once entry fades out.
                    var initial = cycles[0].frames[0];
                    var planted = preparation.Where(f => f.time >= 0.15f).ToArray();
                    Assert.That(planted.Max(f => Mathf.Abs(f.leftFoot.y - initial.leftFoot.y)), Is.LessThan(0.05f), cycle.mode + ": left ankle lost preparation contact.");
                    Assert.That(planted.Max(f => Mathf.Abs(f.rightFoot.y - initial.rightFoot.y)), Is.LessThan(0.05f), cycle.mode + ": right ankle lost preparation contact.");
                }
            }
            finally { UnityEngine.Object.Destroy(mesh); Time.captureDeltaTime = 0f; }
        }

        [UnityTest]
        public IEnumerator BodyCompressionFollowsExternalRootTravelWithoutWorldSpaceOffsets()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Echoryx/Scenes/SuperSoldierLocomotionLab.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var preview = UnityEngine.Object.FindFirstObjectByType<LocomotionAnimationPreview>();
            var animator = preview.PreviewAnimator;
            var controller = animator.runtimeAnimatorController;
            var clip = controller.animationClips.Single(c => c.name == "Jump");
            var origin = animator.transform.position;
            var bones = new[] { HumanBodyBones.Hips, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot }.Select(animator.GetBoneTransform).ToArray();
            preview.enabled = false;
            animator.runtimeAnimatorController = null;
            var graph = PlayableGraph.Create("Body compression relative to external motor root");
            try
            {
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var playable = AnimationClipPlayable.Create(graph, clip);
                AnimationPlayableOutput.Create(graph, "Retargeted in-place clip", animator).SetSourcePlayable(playable);
                graph.Play();
                for (var i = 0; i <= 50; i++)
                {
                    playable.SetTime(Mathf.Min(clip.length * i / 50f, clip.length - 0.0001f));
                    animator.transform.position = origin;
                    graph.Evaluate(0f);
                    var positions = bones.Select(b => b.position).ToArray();
                    animator.transform.position = origin + Vector3.up * 2f;
                    graph.Evaluate(0f);
                    for (var b = 0; b < bones.Length; b++)
                        Assert.That(Vector3.Distance(bones[b].position, positions[b] + Vector3.up * 2f), Is.LessThan(0.0002f), "No absolute floor pinning or permanent visual offset.");
                    Assert.That(animator.transform.position, Is.EqualTo(origin + Vector3.up * 2f));
                    Assert.That(animator.applyRootMotion, Is.False);
                }
            }
            finally
            {
                graph.Destroy(); animator.transform.position = origin;
                animator.runtimeAnimatorController = controller; animator.Rebind(); preview.enabled = true;
            }
        }
    }
}

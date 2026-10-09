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
    // Full-cycle diagnosis, deliberately bypassing the controller and parameter driver.
    public sealed class SuperSoldierLocomotionDiagnostics
    {
        [Serializable] private sealed class Frame
        {
            public float time, skinMinY, hipsY, bodyYaw;
            public Vector3 leftFoot, rightFoot;
        }
        [Serializable] private sealed class Cycle
        {
            public string clip;
            public float length, minSkinY, maxSkinY, maxRootDrift;
            public Frame[] frames;
        }
        [Serializable] private sealed class Report { public string[] renderers; public Cycle[] clips; }

        [UnityTest]
        public IEnumerator IndividualCyclesKeepSolesAboveFloor()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/_Echoryx/Scenes/SuperSoldierLocomotionLab.unity", new LoadSceneParameters(LoadSceneMode.Single));
            Time.captureDeltaTime = 1f / 60f;
            yield return null;
            var preview = UnityEngine.Object.FindFirstObjectByType<LocomotionAnimationPreview>();
            preview.enabled = false;
            var animator = preview.PreviewAnimator;
            var clips = animator.runtimeAnimatorController.animationClips.Distinct().ToArray();
            animator.runtimeAnimatorController = null;
            var origin = animator.transform.position;
            var renderers = animator.GetComponentsInChildren<SkinnedMeshRenderer>();
            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var cycles = new List<Cycle>();
            var poseHandler = new HumanPoseHandler(animator.avatar, animator.transform);
            var pose = new HumanPose();
            try
            {
                foreach (var name in new[] { "IdleLoop", "Walk", "Run", "Jump", "Fall", "Land" })
                {
                    var clip = clips.Single(c => c.name == name);
                    var graph = PlayableGraph.Create("Isolated full cycle " + name);
                    var frames = new List<Frame>();
                    var sheet = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null ? null : new Texture2D(1280, 1680, TextureFormat.RGB24, false);
                    try
                    {
                        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                        var playable = AnimationClipPlayable.Create(graph, clip);
                        playable.SetApplyFootIK(name == "IdleLoop" || name == "Walk" || name == "Run" || name == "Land");
                        AnimationPlayableOutput.Create(graph, "Individual clip", animator).SetSourcePlayable(playable);
                        graph.Play();
                        var total = Mathf.CeilToInt(clip.length * 60f);
                        var nextCapture = 0;
                        for (var i = 0; i <= total; i++)
                        {
                            var t = Mathf.Min(i / 60f, clip.length - 0.0001f);
                            playable.SetTime(t);
                            graph.Evaluate(0f);
                            var min = float.PositiveInfinity;
                            foreach (var skin in renderers)
                            {
                                skin.BakeMesh(mesh, true);
                                mesh.GetVertices(vertices);
                                foreach (var v in vertices) min = Mathf.Min(min, skin.transform.TransformPoint(v).y);
                            }
                            poseHandler.GetHumanPose(ref pose);
                            frames.Add(new Frame { time = t, skinMinY = min, hipsY = animator.GetBoneTransform(HumanBodyBones.Hips).position.y,
                                bodyYaw = Mathf.DeltaAngle(animator.transform.eulerAngles.y, pose.bodyRotation.eulerAngles.y),
                                leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot).position,
                                rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot).position });
                            if (sheet != null && nextCapture < 8 && i >= Mathf.RoundToInt(total * nextCapture / 8f))
                            {
                                CaptureTile(sheet, nextCapture, animator.transform.position, false);
                                CaptureTile(sheet, nextCapture + 8, animator.transform.position, true);
                                nextCapture++;
                            }
                            yield return null;
                        }
                        cycles.Add(new Cycle { clip = name, length = clip.length, frames = frames.ToArray(), minSkinY = frames.Min(f => f.skinMinY),
                            maxSkinY = frames.Max(f => f.skinMinY), maxRootDrift = Vector3.Distance(origin, animator.transform.position) });
                        if (sheet != null)
                        {
                            Directory.CreateDirectory("docs/images");
                            File.WriteAllBytes("docs/images/s05-b3-cycle-" + name.ToLowerInvariant() + ".png", sheet.EncodeToPNG());
                        }
                    }
                    finally { graph.Destroy(); if (sheet != null) UnityEngine.Object.Destroy(sheet); }
                }
                File.WriteAllText("docs/supersoldier-locomotion-cycle-report.json", JsonUtility.ToJson(new Report
                    { renderers = renderers.Select(r => r.name).ToArray(), clips = cycles.ToArray() }, true) + "\n");
                foreach (var cycle in cycles)
                {
                    Assert.That(cycle.maxRootDrift, Is.LessThan(0.0001f), cycle.clip);
                    Assert.That(cycle.frames.Max(f => Mathf.Abs(f.bodyYaw)), Is.LessThan(12f), cycle.clip + " heading continuity");
                    if (cycle.clip == "Walk" || cycle.clip == "Run" || cycle.clip == "Land")
                        Assert.That(cycle.minSkinY, Is.GreaterThan(-0.025f), cycle.clip + " excessive sole penetration over full cycle");
                }
            }
            finally { poseHandler.Dispose(); UnityEngine.Object.Destroy(mesh); Time.captureDeltaTime = 0f; }
        }

        private static void CaptureTile(Texture2D sheet, int tile, Vector3 origin, bool side)
        {
            var target = new RenderTexture(320, 420, 24);
            var pixels = new Texture2D(320, 420, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            var camera = Camera.main;
            var cameraPosition = camera.transform.position;
            var cameraRotation = camera.transform.rotation;
            var fieldOfView = camera.fieldOfView;
            try
            {
                target.Create();
                camera.transform.position = origin + (side ? new Vector3(-3.5f, 1.1f, -1.2f) : new Vector3(0f, 1.1f, -3.8f));
                camera.transform.LookAt(origin + Vector3.up * 0.95f);
                camera.fieldOfView = 40f;
                RenderPipeline.SubmitRenderRequest(Camera.main, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 320, 420), 0, 0);
                pixels.Apply();
                sheet.SetPixels((tile % 4) * 320, (3 - tile / 4) * 420, 320, 420, pixels.GetPixels());
                sheet.Apply();
            }
            finally
            {
                camera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
                camera.fieldOfView = fieldOfView;
                RenderTexture.active = previous;
                target.Release(); UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(pixels);
            }
        }
    }
}

using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Echoryx.Game.PlayTests
{
    /// <summary>Confirms the imported Humanoid Avatar and source clip evaluate in an isolated lab.</summary>
    public sealed class SuperSoldierAnimationPlayCheck
    {
        [UnityTest]
        public IEnumerator HumanoidIdleClipEvaluatesOnSkinnedMeshes()
        {
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(
                "Assets/_Echoryx/Scenes/SuperSoldierValidationLab.unity",
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("The isolated character lab is loaded through Unity Editor.");
#endif
            yield return null;
            var character = GameObject.Find("SuperSoldier Animated Preview");
            Assert.That(character, Is.Not.Null);
            var animator = character.GetComponent<Animator>();
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.avatar, Is.Not.Null);
            Assert.That(animator.avatar.isValid && animator.avatar.isHuman, Is.True);
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
            var renderers = character.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Assert.That(renderers.Length, Is.GreaterThan(0));
            Assert.That(animator.enabled, Is.True);

            // Allow Animator to enter and advance the default state, independent of render FPS.
            yield return new WaitForSeconds(0.25f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).length, Is.GreaterThan(1f));
            var start = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            var bones = renderers.SelectMany(r => r.bones).Where(b => b != null).Distinct().ToArray();
            var startRotations = bones.Select(b => b.localRotation).ToArray();
            yield return new WaitForSeconds(0.5f);
            var end = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            Assert.That(end, Is.GreaterThan(start), "The imported animation state must advance in Play Mode.");
            var largestPoseChange = bones.Select((bone, index) => Quaternion.Angle(startRotations[index], bone.localRotation)).DefaultIfEmpty(0f).Max();
            Assert.That(largestPoseChange, Is.GreaterThan(0.01f), "The animation must update at least one skinned bone pose.");
            Assert.That(renderers, Has.All.Property("enabled").True);
        }
    }
}

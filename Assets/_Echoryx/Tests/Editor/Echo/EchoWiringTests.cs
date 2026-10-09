using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoryx.Echo.Tests
{
    public sealed class EchoWiringTests
    {
        [Test]
        public void ExistingInputAssetAndPlayerPrefabAreConnected()
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");
            Assert.That(actions, Is.Not.Null);
            var action = actions.FindAction("Player/EchoRecord");
            Assert.That(action, Is.Not.Null);
            var hasQ = false;
            foreach (var binding in action.bindings)
                hasQ |= binding.path == "<Keyboard>/q";
            Assert.That(hasQ, Is.True);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Echoryx/Prefabs/Player.prefab");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<EchoRecorder>(), Is.Not.Null);
            var input = prefab.GetComponent<EchoRecordInput>();
            Assert.That(input, Is.Not.Null);
            var serialized = new SerializedObject(input);
            Assert.That(serialized.FindProperty("actions").objectReferenceValue, Is.SameAs(actions));
        }

        [Test]
        public void PlaybackActionActorAndPrefabAreConnectedWithoutPhysics()
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");
            Assert.That(actions, Is.Not.Null);
            var play = actions.FindAction("Player/EchoPlay");
            Assert.That(play, Is.Not.Null);
            var hasE = false;
            foreach (var binding in play.bindings)
                hasE |= binding.path == "<Keyboard>/e";
            Assert.That(hasE, Is.True);

            var actor = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Echoryx/Prefabs/EchoActor.prefab");
            Assert.That(actor, Is.Not.Null);
            Assert.That(actor.GetComponent<EchoActor>(), Is.Not.Null);
            Assert.That(actor.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(actor.GetComponentsInChildren<Rigidbody>(true), Is.Empty);

            var player = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Echoryx/Prefabs/Player.prefab");
            var playback = player.GetComponent<EchoPlayback>();
            var input = player.GetComponent<EchoPlaybackInput>();
            Assert.That(playback, Is.Not.Null);
            Assert.That(input, Is.Not.Null);
            Assert.That(new SerializedObject(playback).FindProperty("actorPrefab").objectReferenceValue,
                Is.SameAs(actor.GetComponent<EchoActor>()));
            Assert.That(new SerializedObject(input).FindProperty("actions").objectReferenceValue,
                Is.SameAs(actions));

            var material = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Echoryx/Materials/EchoHologram.mat");
            Assert.That(material, Is.Not.Null);
            Assert.That(material.GetFloat("_Surface"), Is.EqualTo(1f));
            Assert.That(material.color.a, Is.LessThan(1f));
        }
    }
}

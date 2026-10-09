using Echoryx.Echo;
using Echoryx.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoryx.Interactions.Tests
{
    public sealed class InteractionWiringTests
    {
        [Test]
        public void PrefabsCarrySeparateIdentityWithoutEchoPhysics()
        {
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Echoryx/Prefabs/Player.prefab");
            var echo = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Echoryx/Prefabs/EchoActor.prefab");
            Assert.That(player.GetComponent<PlayerMotor>(), Is.Not.Null);
            Assert.That(player.GetComponent<MechanismActor>().Kind, Is.EqualTo(MechanismActorKind.Player));
            Assert.That(echo.GetComponent<EchoActor>(), Is.Not.Null);
            Assert.That(echo.GetComponent<MechanismActor>().Kind, Is.EqualTo(MechanismActorKind.Echo));
            Assert.That(echo.GetComponentsInChildren<Collider>(true), Is.Empty);
            Assert.That(echo.GetComponentsInChildren<Rigidbody>(true), Is.Empty);
        }

        [Test]
        public void PrototypeKeepsS01CircuitAndHasWiredPuzzle()
        {
            var previous = SceneManager.GetActiveScene().path;
            try
            {
                EditorSceneManager.OpenScene("Assets/_Echoryx/Scenes/Prototype.unity", OpenSceneMode.Single);
                Assert.That(GameObject.Find("S01 Test Circuit"), Is.Not.Null);
                var root = GameObject.Find("S02-C Echo Puzzle");
                Assert.That(root, Is.Not.Null);
                var plate = root.GetComponentInChildren<PressurePlate>();
                var door = root.GetComponentInChildren<DoorController>();
                Assert.That(plate, Is.Not.Null);
                Assert.That(door, Is.Not.Null);
                Assert.That(new SerializedObject(door).FindProperty("plate").objectReferenceValue,
                    Is.SameAs(plate));
                var panel = (Transform)new SerializedObject(door).FindProperty("doorPanel").objectReferenceValue;
                var collider = panel.GetComponent<BoxCollider>();
                Assert.That(collider, Is.Not.Null);
                Assert.That(collider.isTrigger, Is.False);
            }
            finally
            {
                if (!string.IsNullOrEmpty(previous))
                    EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }
        }
    }
}

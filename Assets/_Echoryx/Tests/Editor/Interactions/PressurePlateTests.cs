using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Echoryx.Interactions.Tests
{
    public sealed class PressurePlateTests
    {
        private GameObject plateObject;
        private GameObject playerObject;
        private GameObject echoObject;
        private PressurePlate plate;

        [SetUp]
        public void SetUp()
        {
            plateObject = new GameObject("Plate");
            plate = plateObject.AddComponent<PressurePlate>();
        }

        [TearDown]
        public void TearDown()
        {
            if (echoObject != null) Object.DestroyImmediate(echoObject);
            if (playerObject != null) Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(plateObject);
        }

        [Test]
        public void PlayerEnteringAndLeavingChangesPlateState()
        {
            playerObject = Actor("Player", MechanismActorKind.Player, Vector3.zero);
            plate.RefreshFrom(new[] { playerObject.GetComponent<MechanismActor>() });
            Assert.That(plate.IsPressed, Is.True);
            Assert.That(plate.PlayerPresent, Is.True);

            playerObject.transform.position = new Vector3(3f, 0f, 0f);
            plate.RefreshFrom(new[] { playerObject.GetComponent<MechanismActor>() });
            Assert.That(plate.IsPressed, Is.False);
            Assert.That(plate.PlayerPresent, Is.False);
        }

        [Test]
        public void PlayerAndEchoCanOccupyTogetherAndLeavingOneKeepsPlatePressed()
        {
            playerObject = Actor("Player", MechanismActorKind.Player, Vector3.zero);
            echoObject = Actor("Echo", MechanismActorKind.Echo, Vector3.zero);
            var actors = new[] { playerObject.GetComponent<MechanismActor>(),
                echoObject.GetComponent<MechanismActor>() };
            plate.RefreshFrom(actors);
            Assert.That(plate.PlayerPresent, Is.True);
            Assert.That(plate.EchoPresent, Is.True);

            playerObject.transform.position = new Vector3(3f, 0f, 0f);
            plate.RefreshFrom(actors);
            Assert.That(plate.PlayerPresent, Is.False);
            Assert.That(plate.EchoPresent, Is.True);
            Assert.That(plate.IsPressed, Is.True);
        }

        [Test]
        public void DisablingOrDestroyingEchoClearsOccupancy()
        {
            echoObject = Actor("Echo", MechanismActorKind.Echo, Vector3.zero);
            var actors = new[] { echoObject.GetComponent<MechanismActor>() };
            plate.RefreshFrom(actors);
            Assert.That(plate.IsPressed, Is.True);

            echoObject.SetActive(false);
            plate.RefreshFrom(actors);
            Assert.That(plate.IsPressed, Is.False);
            echoObject.SetActive(true);
            plate.RefreshFrom(actors);
            Assert.That(plate.IsPressed, Is.True);

            Object.DestroyImmediate(echoObject);
            plate.RefreshFrom(actors);
            Assert.That(plate.EchoPresent, Is.False);
            Assert.That(plate.IsPressed, Is.False);
        }

        [Test]
        public void VolumeRejectsActorsAboveOrOutsideThePlate()
        {
            Assert.That(PlateOccupancy.Contains(new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0.65f, 0f), new Vector3(2f, 1.5f, 2f)), Is.True);
            Assert.That(PlateOccupancy.Contains(new Vector3(0f, 2f, 0f),
                new Vector3(0f, 0.65f, 0f), new Vector3(2f, 1.5f, 2f)), Is.False);
            Assert.That(PlateOccupancy.Contains(new Vector3(2f, 0f, 0f),
                new Vector3(0f, 0.65f, 0f), new Vector3(2f, 1.5f, 2f)), Is.False);
        }

        private static GameObject Actor(string name, MechanismActorKind kind, Vector3 position)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.position = position;
            var actor = gameObject.AddComponent<MechanismActor>();
            var data = new SerializedObject(actor);
            data.FindProperty("kind").enumValueIndex = (int)kind;
            data.ApplyModifiedPropertiesWithoutUndo();
            return gameObject;
        }
    }
}

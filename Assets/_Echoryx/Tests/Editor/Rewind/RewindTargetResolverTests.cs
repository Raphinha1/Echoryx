using NUnit.Framework;
using UnityEngine;

namespace Echoryx.Rewind.Tests
{
    public sealed class RewindTargetResolverTests
    {
        private GameObject cameraObject;
        private GameObject playerObject;
        private GameObject mechanismObject;
        private GameObject blockerObject;
        private Camera selectionCamera;
        private RewindTestRotor mechanism;
        private readonly RaycastHit[] hits = new RaycastHit[16];

        [SetUp]
        public void SetUp()
        {
            cameraObject = new GameObject("Selection Camera");
            cameraObject.transform.position = new Vector3(1000f, 0f, -5f);
            selectionCamera = cameraObject.AddComponent<Camera>();

            playerObject = new GameObject("Player Collider");
            playerObject.transform.position = new Vector3(1000f, 0f, -3f);
            playerObject.AddComponent<BoxCollider>();

            mechanismObject = new GameObject("Mechanism");
            mechanismObject.transform.position = new Vector3(1000f, 0f, 0f);
            mechanism = mechanismObject.AddComponent<RewindTestRotor>();
            var child = new GameObject("Child Collider");
            child.transform.SetParent(mechanismObject.transform, false);
            child.AddComponent<BoxCollider>();
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            if (blockerObject != null) Object.DestroyImmediate(blockerObject);
            Object.DestroyImmediate(mechanismObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(cameraObject);
        }

        [Test]
        public void CameraRayIgnoresPlayerAndResolvesChildColliderToParent()
        {
            var target = RewindTargetResolver.Raycast(selectionCamera, playerObject.transform,
                10f, ~0, hits);
            Assert.That(target, Is.SameAs(mechanism));
        }

        [Test]
        public void DistanceAndOccludingGeometryPreventSelection()
        {
            Assert.That(RewindTargetResolver.Raycast(selectionCamera, playerObject.transform,
                3f, ~0, hits), Is.Null);
            blockerObject = new GameObject("Wall");
            blockerObject.transform.position = new Vector3(1000f, 0f, -1f);
            blockerObject.AddComponent<BoxCollider>();
            Physics.SyncTransforms();
            Assert.That(RewindTargetResolver.Raycast(selectionCamera, playerObject.transform,
                10f, ~0, hits), Is.Null);
        }

        [Test]
        public void MissingCameraColliderOrBufferIsSafe()
        {
            Assert.That(RewindTargetResolver.FromCollider(null), Is.Null);
            Assert.That(RewindTargetResolver.Raycast(null, null, 10f, ~0, hits), Is.Null);
            Assert.That(RewindTargetResolver.Raycast(selectionCamera, null, 10f, ~0, null), Is.Null);
        }
    }
}

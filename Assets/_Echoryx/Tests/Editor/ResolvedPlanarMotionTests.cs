using Echoryx.Player;
using NUnit.Framework;
using UnityEngine;

namespace Echoryx.Player.Tests
{
    /// <summary>PlayerMotor.PlanarSpeed excludes platform transport and wall-blocked motion.</summary>
    public sealed class ResolvedPlanarMotionTests
    {
        [Test]
        public void TransportAndContactsDoNotInventOwnSpeed()
        {
            var transport = new Vector3(0f, 0f, 0.1f);
            Assert.That(ResolvedPlanarMotion.Measure(transport, transport, Vector3.zero, 0.02f), Is.EqualTo(Vector3.zero));
            Assert.That(ResolvedPlanarMotion.Measure(Vector3.zero, Vector3.zero, Vector3.zero, 0f), Is.EqualTo(Vector3.zero));
            var own = Vector3.forward * 0.12f;
            var blockedOwn = ResolvedPlanarMotion.RemoveIntoSurface(own, Vector3.back);
            var blockedCarrier = ResolvedPlanarMotion.RemoveIntoSurface(transport, Vector3.back);
            Assert.That(ResolvedPlanarMotion.Measure(Vector3.zero, blockedCarrier, blockedOwn, 0.02f), Is.EqualTo(Vector3.zero));
            var sliding = ResolvedPlanarMotion.RemoveIntoSurface(own + Vector3.right * 0.1f, Vector3.back);
            Assert.That(sliding, Is.EqualTo(Vector3.right * 0.1f));
            Assert.That(ResolvedPlanarMotion.Measure(Vector3.right * 0.5f, Vector3.zero, sliding, 0.02f).magnitude, Is.EqualTo(5f).Within(0.001f));
            // Opposing carrier travel still permits locomotion relative to its surface.
            Assert.That(ResolvedPlanarMotion.Measure(Vector3.zero, -own, own, 0.02f).z, Is.EqualTo(6f).Within(0.001f));
        }
    }
}

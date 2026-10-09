using NUnit.Framework;

namespace Echoryx.Interactions.Tests
{
    public sealed class DoorGateStateTests
    {
        [Test]
        public void ClosedDoorDoesNotOpenForPlayerAlone()
        {
            var gate = new DoorGateState();
            Assert.That(gate.ShouldOpen(false, true, true), Is.False);
        }

        [Test]
        public void PlateOpensDoorAndThresholdKeepsItOpenAfterRelease()
        {
            var gate = new DoorGateState();
            Assert.That(gate.ShouldOpen(true, false, true), Is.True);
            Assert.That(gate.ShouldOpen(false, true, false), Is.True);
            Assert.That(gate.ShouldOpen(false, false, false), Is.False);
            Assert.That(gate.ShouldOpen(false, true, false), Is.True,
                "A player returning while the panel is closing must make it reopen.");
        }

        [Test]
        public void FullyClosedDoorClearsPreviousPlateActivation()
        {
            var gate = new DoorGateState();
            gate.ShouldOpen(true, false, true);
            Assert.That(gate.ShouldOpen(false, false, true), Is.False);
            Assert.That(gate.ShouldOpen(false, true, true), Is.False);
        }
    }
}

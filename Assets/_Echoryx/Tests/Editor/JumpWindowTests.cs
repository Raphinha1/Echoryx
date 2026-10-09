using NUnit.Framework;

namespace Echoryx.Player.Tests
{
    public sealed class JumpWindowTests
    {
        [Test]
        public void GroundedPressCanOnlyBeConsumedOnce()
        {
            var window = new JumpWindow();
            window.SetGrounded(true, 1f);
            window.Press(1f);
            Assert.That(window.TryConsume(1f, 0.12f, 0.12f), Is.True);
            window.SetGrounded(true, 1.01f);
            window.Press(1.01f);
            Assert.That(window.TryConsume(1.01f, 0.12f, 0.12f), Is.False);
        }

        [Test]
        public void CoyoteJumpExpiresAfterWindow()
        {
            var window = new JumpWindow();
            window.SetGrounded(true, 1f);
            window.Press(1.1f);
            Assert.That(window.TryConsume(1.1f, 0.12f, 0.12f), Is.True);

            var expired = new JumpWindow();
            expired.SetGrounded(true, 1f);
            expired.Press(1.13f);
            Assert.That(expired.TryConsume(1.13f, 0.12f, 0.12f), Is.False);
        }

        [Test]
        public void BufferedPressIsConsumedOnLandingWithinWindow()
        {
            var window = new JumpWindow();
            window.Press(2f);
            Assert.That(window.TryConsume(2.05f, 0.12f, 0.12f), Is.False);
            window.SetGrounded(true, 2.08f);
            Assert.That(window.TryConsume(2.08f, 0.12f, 0.12f), Is.True);

            var expired = new JumpWindow();
            expired.Press(2f);
            expired.SetGrounded(true, 2.13f);
            Assert.That(expired.TryConsume(2.13f, 0.12f, 0.12f), Is.False);
        }
    }
}

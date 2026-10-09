namespace Echoryx.Player
{
    /// <summary>Tracks the two timing windows without depending on scene or input state.</summary>
    public sealed class JumpWindow
    {
        private float lastGrounded = float.NegativeInfinity;
        private float lastPressed = float.NegativeInfinity;
        private bool consumed;
        private bool wasGrounded;

        public void SetGrounded(bool grounded, float time)
        {
            if (grounded)
            {
                if (!wasGrounded) consumed = false;
                lastGrounded = time;
            }
            wasGrounded = grounded;
        }

        public void Press(float time)
        {
            lastPressed = time;
        }

        public bool TryConsume(float time, float coyoteTime, float bufferTime)
        {
            if (consumed || time - lastGrounded > coyoteTime || time - lastPressed > bufferTime)
                return false;

            consumed = true;
            lastPressed = float.NegativeInfinity;
            return true;
        }
    }
}

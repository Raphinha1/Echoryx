using UnityEngine;

namespace Echoryx.Interactions
{
    /// <summary>Pure plate state; simultaneous occupants do not cancel one another.</summary>
    public sealed class PlateOccupancy
    {
        public bool PlayerPresent { get; private set; }
        public bool EchoPresent { get; private set; }
        public bool IsPressed => PlayerPresent || EchoPresent;

        public void Set(bool playerPresent, bool echoPresent)
        {
            PlayerPresent = playerPresent;
            EchoPresent = echoPresent;
        }

        public void Clear() => Set(false, false);

        public static bool Contains(Vector3 localPosition, Vector3 center, Vector3 size)
        {
            var delta = localPosition - center;
            return Mathf.Abs(delta.x) <= size.x * 0.5f
                && Mathf.Abs(delta.y) <= size.y * 0.5f
                && Mathf.Abs(delta.z) <= size.z * 0.5f;
        }
    }
}

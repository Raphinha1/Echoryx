using UnityEngine;

namespace Echoryx.Player
{
    /// <summary>Feet must be over the top face; side contacts do not count as riding.</summary>
    public static class PlatformRidePolicy
    {
        public static bool IsSupported(Vector3 feet, Bounds bounds,
            float verticalTolerance, float horizontalMargin)
        {
            return Mathf.Abs(feet.y - bounds.max.y) <= verticalTolerance
                && feet.x >= bounds.min.x - horizontalMargin
                && feet.x <= bounds.max.x + horizontalMargin
                && feet.z >= bounds.min.z - horizontalMargin
                && feet.z <= bounds.max.z + horizontalMargin;
        }
    }
}

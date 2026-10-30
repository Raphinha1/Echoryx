using UnityEngine;

namespace Echoryx.Player
{
    /// <summary>Presentation attribution for the motor's single combined controller Move.</summary>
    public static class ResolvedPlanarMotion
    {
        public static Vector3 RemoveIntoSurface(Vector3 displacement, Vector3 normal)
        {
            var into = Vector3.Dot(displacement, normal);
            return into < 0f ? displacement - normal * into : displacement;
        }

        public static Vector3 Measure(Vector3 resolved, Vector3 unblockedTransport,
            Vector3 unblockedOwnMotion, float deltaTime)
        {
            if (deltaTime <= 0f) return Vector3.zero;
            unblockedOwnMotion.y = 0f;
            var own = resolved - unblockedTransport;
            own.y = 0f;
            // Idle transport and depenetration must not invent locomotion. Contacts also
            // bound the metric when the carrier and character both push into a wall.
            return Vector3.ClampMagnitude(own, unblockedOwnMotion.magnitude) / deltaTime;
        }
    }
}

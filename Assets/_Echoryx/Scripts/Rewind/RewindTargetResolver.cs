using UnityEngine;

namespace Echoryx.Rewind
{
    /// <summary>Finds the nearest visible collider, then resolves its mechanism parent.</summary>
    public static class RewindTargetResolver
    {
        public static RewindableMechanism FromCollider(Collider collider) =>
            collider != null ? collider.GetComponentInParent<RewindableMechanism>() : null;

        public static RewindableMechanism Raycast(
            Camera camera, Transform ignoreRoot, float maximumDistance, LayerMask layers,
            RaycastHit[] hitBuffer)
        {
            if (camera == null || maximumDistance <= 0f || hitBuffer == null || hitBuffer.Length == 0)
                return null;

            var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            var count = Physics.RaycastNonAlloc(ray, hitBuffer, maximumDistance, layers,
                QueryTriggerInteraction.Ignore);
            var nearestDistance = float.PositiveInfinity;
            Collider nearest = null;
            for (var i = 0; i < count; i++)
            {
                var collider = hitBuffer[i].collider;
                if (collider == null || (ignoreRoot != null
                    && collider.transform.IsChildOf(ignoreRoot))) continue;
                if (hitBuffer[i].distance >= nearestDistance) continue;
                nearestDistance = hitBuffer[i].distance;
                nearest = collider;
            }

            var mechanism = FromCollider(nearest);
            return mechanism != null && mechanism.IsAvailable ? mechanism : null;
        }
    }
}

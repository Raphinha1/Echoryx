using Echoryx.Interactions;
using UnityEngine;

namespace Echoryx.Level
{
    /// <summary>Spatial player test shared by level triggers (same approach as CheckpointTrigger).</summary>
    internal static class PlayerProbe
    {
        /// <summary>World position of the live player actor, if any.</summary>
        public static bool TryGetPlayerPosition(out Vector3 position)
        {
            var actors = MechanismActor.ActiveActors;
            for (var i = 0; i < actors.Count; i++)
            {
                var actor = actors[i];
                if (actor == null || !actor.isActiveAndEnabled || actor.Kind != MechanismActorKind.Player) continue;
                position = actor.transform.position;
                return true;
            }
            position = default;
            return false;
        }

        public static bool PlayerInside(BoxCollider volume)
        {
            if (volume == null || !volume.enabled || !volume.gameObject.activeInHierarchy) return false;
            var actors = MechanismActor.ActiveActors;
            for (var i = 0; i < actors.Count; i++)
            {
                var actor = actors[i];
                if (actor == null || !actor.isActiveAndEnabled || actor.Kind != MechanismActorKind.Player) continue;
                var local = volume.transform.InverseTransformPoint(actor.transform.position) - volume.center;
                var half = volume.size * 0.5f;
                if (Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z)
                    return true;
            }
            return false;
        }
    }
}

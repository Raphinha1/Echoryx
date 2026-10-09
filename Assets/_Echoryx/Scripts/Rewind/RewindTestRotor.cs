using UnityEngine;

namespace Echoryx.Rewind
{
    /// <summary>Small kinematic target for S03-B Play Mode checks, not a puzzle platform.</summary>
    public sealed class RewindTestRotor : RewindableMechanism
    {
        [SerializeField] private float degreesPerSecond = 90f;

        protected override void AdvanceNormal(float deltaTime)
        {
            transform.Rotate(Vector3.up, degreesPerSecond * deltaTime, Space.Self);
        }
    }
}

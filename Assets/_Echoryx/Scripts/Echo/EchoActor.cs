using UnityEngine;

namespace Echoryx.Echo
{
    /// <summary>Marker for the Echo entity; S02-C can detect it without using PlayerMotor.</summary>
    [DisallowMultipleComponent]
    public sealed class EchoActor : MonoBehaviour
    {
        public void ApplyPose(EchoSample pose)
        {
            transform.SetPositionAndRotation(pose.Position, pose.Rotation);
        }
    }
}

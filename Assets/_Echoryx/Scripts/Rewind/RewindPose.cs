using UnityEngine;

namespace Echoryx.Rewind
{
    /// <summary>World-space state supported by kinematic mechanisms in S03-A.</summary>
    public readonly struct RewindPose
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public RewindPose(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public static RewindPose Interpolate(RewindPose from, RewindPose to, float fraction)
        {
            return new RewindPose(
                Vector3.Lerp(from.Position, to.Position, fraction),
                Quaternion.Slerp(from.Rotation, to.Rotation, fraction));
        }
    }
}

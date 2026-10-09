using UnityEngine;

namespace Echoryx.Echo
{
    /// <summary>One pose at a time relative to the start of an Echo recording.</summary>
    public readonly struct EchoSample
    {
        public readonly double Time;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public EchoSample(double time, Vector3 position, Quaternion rotation)
        {
            Time = time;
            Position = position;
            Rotation = rotation;
        }
    }
}

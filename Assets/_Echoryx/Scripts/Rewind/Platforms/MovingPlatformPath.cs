using UnityEngine;

namespace Echoryx.Rewind.Platforms
{
    public readonly struct MovingPlatformPathPoint
    {
        public readonly Vector3 Position;
        public readonly float Progress;
        public readonly int Direction;

        public MovingPlatformPathPoint(Vector3 position, float progress, int direction)
        {
            Position = position;
            Progress = progress;
            Direction = direction;
        }
    }

    /// <summary>One-way path driven by the rewindable virtual clock.</summary>
    public static class MovingPlatformPath
    {
        public static double TravelSeconds(Vector3 start, Vector3 end, float speed)
        {
            var distance = Vector3.Distance(start, end);
            return distance <= 1e-5f || speed <= 0f ? 0d : distance / speed;
        }

        public static bool HasRemainingTravel(
            Vector3 start, Vector3 end, float speed, double timelineTime)
        {
            var duration = TravelSeconds(start, end, speed);
            return duration > 0d && timelineTime < duration;
        }

        public static MovingPlatformPathPoint Evaluate(
            Vector3 start, Vector3 end, float speed, double timelineTime)
        {
            var duration = TravelSeconds(start, end, speed);
            if (duration <= 0d)
                return new MovingPlatformPathPoint(start, 0f, 0);

            var progress = Mathf.Clamp01((float)(timelineTime / duration));
            return new MovingPlatformPathPoint(
                Vector3.Lerp(start, end, progress), progress, progress < 1f ? 1 : 0);
        }
    }
}

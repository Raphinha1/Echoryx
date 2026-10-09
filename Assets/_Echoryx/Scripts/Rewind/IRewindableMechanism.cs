namespace Echoryx.Rewind
{
    /// <summary>Control surface for S03-B to operate one selected mechanism.</summary>
    public interface IRewindableMechanism
    {
        RewindMode Mode { get; }
        bool IsAvailable { get; }
        bool CanRewind { get; }
        bool BeginRewind();
        bool EndRewind();
        void ResetHistory();
    }
}

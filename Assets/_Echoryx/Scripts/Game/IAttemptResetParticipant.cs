namespace Echoryx.Game
{
    /// <summary>All releases finish before any participant restores its initial state.</summary>
    public interface IAttemptResetParticipant
    {
        void ReleaseForReset();
        void ResetForAttempt();
    }
}

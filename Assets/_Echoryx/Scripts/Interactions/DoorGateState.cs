namespace Echoryx.Interactions
{
    /// <summary>Keeps a previously opened door clear until the player leaves its threshold.</summary>
    public sealed class DoorGateState
    {
        private bool openedByPlate;

        public bool ShouldOpen(bool platePressed, bool playerInThreshold, bool fullyClosed)
        {
            if (fullyClosed && !platePressed) openedByPlate = false;
            if (platePressed) openedByPlate = true;
            return platePressed || (openedByPlate && playerInThreshold);
        }

        public void Reset() => openedByPlate = false;
    }
}

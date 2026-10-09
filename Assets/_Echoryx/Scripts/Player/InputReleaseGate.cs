using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Echoryx.Player
{
    /// <summary>Old attempt inputs require a neutral state before they can be used again.</summary>
    public sealed class InputReleaseGate
    {
        private readonly HashSet<InputAction> blocked = new HashSet<InputAction>();
        private int blockedThroughFrame = -1;

        public void Reset(InputActionMap map, int frame)
        {
            blocked.Clear();
            if (map != null)
                foreach (var action in map.actions)
                    if (action.inProgress) blocked.Add(action);
            blockedThroughFrame = frame + 1;
        }

        public bool CanRead(InputAction action, int frame)
        {
            if (action == null || frame <= blockedThroughFrame) return false;
            if (!blocked.Contains(action)) return true;
            if (action.inProgress) return false;
            blocked.Remove(action);
            return true;
        }
    }
}

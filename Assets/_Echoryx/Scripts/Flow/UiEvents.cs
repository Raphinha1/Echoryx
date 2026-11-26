using System;
using UnityEngine.UI;

namespace Echoryx.Flow
{
    /// <summary>Notifications from the code-built menus, for presentation layers such as audio.</summary>
    public static class UiEvents
    {
        public static event Action<Button> ButtonCreated;
        internal static void RaiseButtonCreated(Button button) => ButtonCreated?.Invoke(button);
    }
}

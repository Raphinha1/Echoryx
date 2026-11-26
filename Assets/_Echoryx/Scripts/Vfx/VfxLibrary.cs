using UnityEngine;

namespace Echoryx.Vfx
{
    /// <summary>The project's own URP particle materials, kept in Resources so builds include them.</summary>
    public sealed class VfxLibrary : ScriptableObject
    {
        public const string ResourceName = "Vfx/VfxLibrary";
        public Material glow;
        public Material dust;
        public Material ring;
    }
}

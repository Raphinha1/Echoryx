using System.Collections.Generic;
using UnityEngine;

namespace Echoryx.Interactions
{
    public enum MechanismActorKind { Player, Echo }

    /// <summary>Identity and live registry for actors that can operate mechanisms.</summary>
    [DisallowMultipleComponent]
    public sealed class MechanismActor : MonoBehaviour
    {
        [SerializeField] private MechanismActorKind kind;
        private static readonly List<MechanismActor> Active = new List<MechanismActor>(2);

        public MechanismActorKind Kind => kind;
        public static IReadOnlyList<MechanismActor> ActiveActors => Active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => Active.Clear();

        private void OnEnable()
        {
            if (!Active.Contains(this)) Active.Add(this);
        }

        private void OnDisable() => Active.Remove(this);
    }
}

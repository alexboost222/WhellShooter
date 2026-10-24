using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.WeaponsFeature.Components
{
    /// <summary>
    /// "A shot left this body's muzzle this tick." Added to the shooter by <c>WeaponFireSystem</c>;
    /// consumed — read, then removed — by Combat's hitscan, listed after Weapons (Docs/08 §Weapons).
    /// </summary>
    public struct ShotRequest : IComponent
    {
        /// <summary>The eye, world space — shots come from where the view is, not from the model.</summary>
        public float3 Origin;

        /// <summary>Unit vector.</summary>
        public float3 Direction;

        public int WeaponId;

        /// <summary>Ticks to rewind the targets by — where the shooter saw them (Docs/05). 0 = now.</summary>
        public int RewindTicks;
    }
}

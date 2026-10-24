using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Combat.Components
{
    /// <summary>
    /// One hit, as an entity of its own so any number can land on one body in one tick. Created by
    /// the hitscan (or anything else that hurts), consumed and tagged <c>Destroy</c> by
    /// <c>DamageApplySystem</c> the same tick.
    /// </summary>
    public struct Damage : IComponent
    {
        public EntityId Target;

        /// <summary>The body that dealt it; <c>EntityId.Invalid</c> for the world (falls, hazards).</summary>
        public EntityId Source;

        public float Amount;

        /// <summary>Where the hit came from — the shooter's eye for a shot.</summary>
        public float3 Origin;

        /// <summary>Unit direction of travel.</summary>
        public float3 Direction;
    }
}

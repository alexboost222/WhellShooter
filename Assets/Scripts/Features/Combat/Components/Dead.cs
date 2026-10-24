using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Combat.Components
{
    /// <summary>
    /// "This body is down." Added by <c>DamageApplySystem</c> when health reaches zero, removed by a
    /// <see cref="ReviveRequest"/>. A dead body keeps its entity — it still falls under gravity and
    /// can be teleported — but gets no input (<c>DeadIntentSystem</c>) and can't be hit.
    /// </summary>
    public struct Dead : IComponent
    {
        /// <summary>Who landed the last hit; <c>EntityId.Invalid</c> when it wasn't a body.</summary>
        public EntityId Killer;

        /// <summary>Unit direction the killing shot travelled, world space — for ragdolls and death clips.</summary>
        public float3 Direction;
    }
}

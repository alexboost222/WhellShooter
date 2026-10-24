using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Combat.Messages
{
    /// <summary>
    /// A shot was resolved: where it started and where it stopped. Published by <c>HitscanSystem</c>
    /// for views — tracers, the hit marker. Nothing in the simulation reads it.
    /// </summary>
    public struct ShotTraced
    {
        public EntityId Shooter;
        public float3 Origin;

        /// <summary>The first thing it met — a wall or a body — or the end of its range.</summary>
        public float3 End;

        /// <summary>It stopped in a living body.</summary>
        public bool HitBody;
    }
}

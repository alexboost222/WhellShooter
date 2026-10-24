using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Replication.Components
{
    /// <summary>
    /// Client: the local body's predicted feet and velocity after each of its last
    /// <see cref="Capacity"/> ticks, what a snapshot is compared against (Docs/05 §Prediction).
    /// </summary>
    public struct Prediction : IComponent
    {
        public const int Capacity = 64;
        public const int Mask = Capacity - 1;

        /// <summary>Ring keyed by tick &amp; <see cref="Mask"/>; <see cref="Ticks"/> says which tick a slot holds.</summary>
        public int[] Ticks;
        public float3[] Feet;
        public float3[] Velocity;

        /// <summary>Newest snapshot tick already compared.</summary>
        public int CheckedTick;
    }
}

using System;
using Unity.Mathematics;

namespace Engine.Motion
{
    /// <summary>
    /// A damped spring on three independent axes: where it is and how fast it is going. Pure data,
    /// so it sits in a component as is; <see cref="Springs.Step"/> moves it.
    /// </summary>
    /// <remarks>
    /// Used for anything that should <i>settle</i> rather than snap — a weapon lagging behind the
    /// view, a recoil kick, a camera punch. A kick is a velocity: add to <see cref="Velocity"/> and
    /// the spring carries it out and back.
    /// </remarks>
    [Serializable]
    public struct Spring3
    {
        public float3 Value;
        public float3 Velocity;

        /// <summary>Adds a kick. Nothing else about the spring changes.</summary>
        public void Impulse(float3 velocity)
        {
            Velocity += velocity;
        }
    }
}

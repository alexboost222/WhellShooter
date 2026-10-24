using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.CharacterFeature.Components
{
    /// <summary>
    /// The body's simulated motion, owned by <c>CharacterMotorSystem</c> on the fixed tick.
    /// </summary>
    /// <remarks>
    /// <b>Events are counters, not flags.</b> <see cref="Jumps"/> and <see cref="Landings"/> only ever
    /// go up; a view remembers the last value it saw and reacts to the difference. A flag would need
    /// someone to clear it, and the fixed tick can run zero or three times between two frames — a
    /// counter is right either way.
    /// </remarks>
    public struct CharacterMotion : IComponent
    {
        /// <summary>Feet, world space, after the last tick.</summary>
        public float3 Position;

        /// <summary><see cref="Position"/> before the last tick — the other end of interpolation.</summary>
        public float3 PreviousPosition;

        public float3 Velocity;
        public bool Grounded;
        public bool Sprinting;

        public uint Jumps;
        public uint Landings;

        /// <summary>Downward speed at the last landing, m/s, positive.</summary>
        public float LandingSpeed;
    }
}

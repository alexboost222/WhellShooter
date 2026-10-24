using Scellecs.Morpeh;

namespace Features.WeaponsFeature.Components
{
    /// <summary>
    /// The weapon a body holds, and its firing state. Simulation, on the fixed tick.
    /// </summary>
    /// <remarks>
    /// <see cref="Shots"/> only ever goes up — the viewmodel reacts to the difference from the last
    /// value it saw, so it is right however many ticks ran between two frames. The hit itself travels
    /// as a <see cref="ShotRequest"/>.
    /// </remarks>
    public struct Weapon : IComponent
    {
        /// <summary>Hash of a <c>WeaponsConfig</c> row's name.</summary>
        public int ConfigId;

        /// <summary>Seconds until the next shot may leave. Never banks below zero.</summary>
        public float Cooldown;

        public uint Shots;

        /// <summary>Aiming down sights: wanted, and not sprinting.</summary>
        public bool Aiming;
    }
}

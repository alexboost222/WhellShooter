using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.CharacterFeature.Components
{
    /// <summary>
    /// What the body's driver is asking for — the player's input or a bot's brain, written the same
    /// way. Systems decide what it means.
    /// </summary>
    /// <remarks>
    /// <b>Held vs pressed.</b> Held buttons are overwritten by the driver every frame. The
    /// <c>…Pressed</c> flags are <b>latches</b>: the driver sets them, the fixed-tick system that acts
    /// on them clears them, so a tap shorter than a tick is still a jump or a shot.
    /// <para>
    /// No look in here: <see cref="Look"/> is written directly — a bot sets where it faces, the
    /// player's look system spends pointer movement on it.
    /// </para>
    /// </remarks>
    public struct CharacterIntent : IComponent
    {
        /// <summary>Strafe (x) and forward (y), body-local, length ≤ 1.</summary>
        public float2 Move;

        public bool Sprint;
        public bool Fire;
        public bool Aim;

        /// <summary>Latched; the motor clears it.</summary>
        public bool JumpPressed;

        /// <summary>Latched; the weapon clears it.</summary>
        public bool FirePressed;
    }
}

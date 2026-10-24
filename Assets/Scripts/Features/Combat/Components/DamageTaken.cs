using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Combat.Components
{
    /// <summary>
    /// The last hit a body took, kept on the body. <see cref="Hits"/> only goes up: a reader (a bot's
    /// senses, a hit-marker view) remembers the count it last saw and reacts to the difference.
    /// </summary>
    public struct DamageTaken : IComponent
    {
        public uint Hits;
        public EntityId LastSource;

        /// <summary>Where the last hit came from (the shooter's eye).</summary>
        public float3 LastOrigin;
    }
}

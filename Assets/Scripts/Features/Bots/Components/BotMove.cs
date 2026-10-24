using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bots.Components
{
    /// <summary>
    /// Where the current action wants the bot to go. Written by action executors, followed by
    /// <c>BotPathSystem</c> and <c>BotSteerSystem</c>, which report back through
    /// <see cref="Arrived"/> and <see cref="Failed"/>.
    /// </summary>
    public struct BotMove : IComponent
    {
        public float3 Destination;

        /// <summary>False = stand still.</summary>
        public bool Active;

        public bool Sprint;

        /// <summary>Reached the destination (within the arrive distance).</summary>
        public bool Arrived;

        /// <summary>No path, or stuck for good — the action should pick somewhere else.</summary>
        public bool Failed;
    }
}

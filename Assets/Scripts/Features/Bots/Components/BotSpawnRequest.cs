using Scellecs.Morpeh;

namespace Features.Bots.Components
{
    /// <summary>
    /// Request: add bots to the match. Consumed (read-then-remove) by <c>BotSpawnSystem</c> once the
    /// arena has spawn points — until then it waits.
    /// </summary>
    public struct BotSpawnRequest : IComponent
    {
        public int Count;

        /// <summary>Two teams (alternating 0 / 1) instead of free-for-all.</summary>
        public bool Teams;

        /// <summary>Profile id; 0 = the config's default profile.</summary>
        public int ProfileId;
    }
}

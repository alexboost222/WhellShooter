namespace Features.Bots.Messages
{
    /// <summary>
    /// Asks for bots. Published from outside the world (scene providers, dev tools);
    /// <c>BotSpawnSystem</c> turns it into a <c>BotSpawnRequest</c>.
    /// </summary>
    public struct SpawnBots
    {
        public int Count;
        public bool Teams;

        /// <summary>Profile name hash (<c>ConfigIds.Hash</c>); 0 = default.</summary>
        public int ProfileId;
    }
}

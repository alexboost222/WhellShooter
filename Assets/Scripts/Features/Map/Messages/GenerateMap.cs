namespace Features.Map.Messages
{
    /// <summary>
    /// Asks for a new arena. Published by anything outside the world (scene providers, dev tools);
    /// consumed by <c>MapGenerateSystem</c> the tick after.
    /// </summary>
    /// <remarks>
    /// The seed is chosen by the publisher, so the system stays deterministic: same seed, same arena.
    /// </remarks>
    public struct GenerateMap
    {
        public int Seed;
    }
}

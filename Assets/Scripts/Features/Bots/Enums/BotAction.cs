namespace Features.Bots.Enums
{
    /// <summary>
    /// What a bot is doing. Each value has a score system that bids for it, a state component that
    /// is present while it runs, and an executor system that acts on that component (Docs/08 §Bots).
    /// </summary>
    /// <remarks>
    /// Adding a behaviour = a value here, a <c>BotXxx</c> component, a <c>BotXxxScoreSystem</c> and a
    /// <c>BotXxxSystem</c>, and one case in <c>BotDecideSystem</c>. Nothing else changes.
    /// </remarks>
    public enum BotAction : byte
    {
        None = 0,
        Wander = 1,
        Attack = 2,
        Chase = 3,
    }
}

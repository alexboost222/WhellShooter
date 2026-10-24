using Scellecs.Morpeh;

namespace Features.Bots.Components
{
    /// <summary>"This body is driven by the AI", and with which skill profile.</summary>
    public struct Bot : IComponent
    {
        /// <summary><c>ConfigIds.Hash</c> of a <c>BotProfile</c> name in <c>BotsConfig</c>.</summary>
        public int ProfileId;
    }
}

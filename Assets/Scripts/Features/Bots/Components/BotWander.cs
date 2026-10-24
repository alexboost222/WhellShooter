using Scellecs.Morpeh;

namespace Features.Bots.Components
{
    /// <summary>State of <c>BotAction.Wander</c>: roam the arena between random points.</summary>
    public struct BotWander : IComponent
    {
        /// <summary>Seconds to idle before picking the next point.</summary>
        public float Wait;

        public bool HasDestination;
    }
}

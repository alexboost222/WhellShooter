using Scellecs.Morpeh;

namespace Features.Bots.Components
{
    /// <summary>State of <c>BotAction.Chase</c>: target lost — run to where it was last seen.</summary>
    public struct BotChase : IComponent
    {
        /// <summary>Reached the last known position and found nothing; the chase scores zero after that.</summary>
        public bool Exhausted;
    }
}

using Scellecs.Morpeh;

namespace Features.Bots.Components
{
    /// <summary>On a dead bot: seconds until it comes back. Added and consumed by <c>BotRespawnSystem</c>.</summary>
    public struct BotRespawn : IComponent
    {
        public float Remaining;
    }
}

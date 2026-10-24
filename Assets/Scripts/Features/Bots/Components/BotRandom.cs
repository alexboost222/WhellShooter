using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bots.Components
{
    /// <summary>
    /// The bot's own seeded random stream. Every roll a bot makes (where to wander, how its aim
    /// wobbles, which death clip) comes from here — no <c>UnityEngine.Random</c>, so a seed replays.
    /// </summary>
    public struct BotRandom : IComponent
    {
        public Random Value;
    }
}

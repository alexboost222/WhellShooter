using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bots.Components
{
    /// <summary>
    /// What the bot knows about its enemy, written only by <c>BotSensesSystem</c>. Scorers and
    /// executors read it; nobody else writes it.
    /// </summary>
    public struct BotSenses : IComponent
    {
        /// <summary>The enemy being tracked. Meaningful only while <see cref="HasTarget"/>.</summary>
        public EntityId Target;

        public bool HasTarget;

        /// <summary>In sight at the last scan.</summary>
        public bool Visible;

        /// <summary>Where the target's feet were when last seen (or where a shot came from).</summary>
        public float3 TargetPosition;

        /// <summary>Seconds the target has been continuously visible — drives reaction and aim settling.</summary>
        public float SeenFor;

        /// <summary>Seconds since the target was last visible.</summary>
        public float SinceSeen;

        /// <summary>Seconds until the next scan; scans are staggered between bots.</summary>
        public float NextScan;

        /// <summary>The <c>DamageTaken.Hits</c> value last reacted to.</summary>
        public uint HitsSeen;
    }
}

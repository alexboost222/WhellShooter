using Scellecs.Morpeh;

namespace Features.Bots.Components
{
    /// <summary>State of <c>BotAction.Attack</c>: target in sight — aim, shoot, keep moving.</summary>
    public struct BotAttack : IComponent
    {
        /// <summary>Seconds until the next strafe point.</summary>
        public float NextStrafe;

        /// <summary>+1 / −1: which side the last strafe went, so the next goes the other way.</summary>
        public float StrafeSide;
    }
}

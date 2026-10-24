using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bots.Components
{
    /// <summary>
    /// What the bot wants to look at, and the human error on top. Written by executors
    /// (<see cref="Point"/>, <see cref="Active"/>), turned into <c>Look</c> by <c>BotAimSystem</c>.
    /// </summary>
    public struct BotAim : IComponent
    {
        /// <summary>World point to look at. When not <see cref="Active"/>, the bot looks where it walks.</summary>
        public float3 Point;

        public bool Active;

        /// <summary>Current aim error, radians (yaw, pitch); drifts towards <see cref="ErrorTarget"/>.</summary>
        public float2 Error;

        public float2 ErrorTarget;

        /// <summary>Seconds until a new <see cref="ErrorTarget"/> is rolled.</summary>
        public float NextErrorRoll;

        /// <summary>Set by <c>BotAimSystem</c>: the look has caught up with the (erroneous) aim — fire is allowed.</summary>
        public bool OnTarget;
    }
}

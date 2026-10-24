using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.CharacterFeature.Components
{
    /// <summary>
    /// "Put this body here, facing this way, at rest." Anyone may add it (a respawn, a spawner);
    /// <c>CharacterTeleportSystem</c> consumes it — read, then removed — before the motor runs.
    /// </summary>
    public struct CharacterTeleportRequest : IComponent
    {
        /// <summary>Feet, world space.</summary>
        public float3 Position;

        /// <summary>Radians. Pitch is levelled.</summary>
        public float Yaw;
    }
}

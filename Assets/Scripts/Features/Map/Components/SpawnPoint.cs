using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Map.Components
{
    /// <summary>
    /// A place a player may spawn, from the current arena. Replaced wholesale on every generation.
    /// </summary>
    public struct SpawnPoint : IComponent
    {
        public float3 Position;

        /// <summary>Facing, degrees around Y (0 = +Z).</summary>
        public float Yaw;

        /// <summary>Team index, or <see cref="FreeForAll"/> for a spawn any player may use.</summary>
        public int Team;

        public const int FreeForAll = -1;
    }
}

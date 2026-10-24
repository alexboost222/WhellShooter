using Unity.Mathematics;

namespace Features.Map.Utils
{
    /// <summary>A spawn point in a <see cref="MapLayout"/>.</summary>
    public struct MapSpawn
    {
        public int2 Cell;
        public float3 Position;
        public float Yaw;
        public int Team;
    }
}

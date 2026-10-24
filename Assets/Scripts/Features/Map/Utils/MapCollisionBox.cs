using Unity.Mathematics;

namespace Features.Map.Utils
{
    /// <summary>One static collision box of the arena.</summary>
    public readonly struct MapCollisionBox
    {
        public readonly float3 Center;
        public readonly float3 HalfExtents;

        /// <summary>Radians about +Y; at 0 the box's local X runs along world X.</summary>
        public readonly float Yaw;

        public MapCollisionBox(float3 center, float3 halfExtents, float yaw)
        {
            Center = center;
            HalfExtents = halfExtents;
            Yaw = yaw;
        }

        public override string ToString()
        {
            return $"box {Center} ±{HalfExtents} yaw {math.degrees(Yaw):0}";
        }
    }
}

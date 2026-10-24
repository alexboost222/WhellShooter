using Unity.Mathematics;

namespace Engine.Physics
{
    /// <summary>A <see cref="PhysicsWorld.Raycast(float3, float3, float, out PhysicsRayHit)"/> result, in floats.</summary>
    public readonly struct PhysicsRayHit
    {
        /// <summary>The native body hit — compare with <c>PhysicsBody.Handle</c>.</summary>
        public readonly ulong Body;

        public readonly float Distance;
        public readonly float3 Point;
        public readonly float3 Normal;

        /// <summary>The hit point in the body's local space (hit zones: Y above a capsule's centre).</summary>
        public readonly float3 LocalPoint;

        public PhysicsRayHit(ulong body, float distance, float3 point, float3 normal, float3 localPoint)
        {
            Body = body;
            Distance = distance;
            Point = point;
            Normal = normal;
            LocalPoint = localPoint;
        }
    }
}

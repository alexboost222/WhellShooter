using Engine.Physics;
using PhysR;
using Unity.Mathematics;

namespace Features.Combat.Systems
{
    /// <summary>
    /// <see cref="IShotOccluder"/> over the phys-r world (Docs/11 §Queries): the first <b>static</b>
    /// body along the ray. Bodies that move — characters included — are left to the hitscan.
    /// </summary>
    public sealed class WorldShotOccluder : IShotOccluder
    {
        private readonly PhysicsWorld _physics;

        public WorldShotOccluder(PhysicsWorld physics)
        {
            _physics = physics;
        }

        public bool Raycast(float3 origin, float3 direction, float maxDistance, out float distance)
        {
            var filter = new QueryFilter(uint.MaxValue, default, QueryFlags.ExcludeMoving);
            if (_physics.Raycast(origin, direction, maxDistance, filter, out var hit))
            {
                distance = hit.Distance;
                return true;
            }

            distance = 0f;
            return false;
        }
    }
}

using Features.Replication.Enums;
using Unity.Mathematics;

namespace Features.Replication.Utils
{
    /// <summary>One body in one <c>Snapshot</c> (Docs/05 §Snapshot), decoded.</summary>
    public struct BodySnapshot
    {
        public int Id;
        public SnapshotFlags Flags;
        public sbyte Team;
        public float3 Feet;
        public float3 Velocity;
        public float Yaw;
        public float Pitch;
        public float Health;
        /// <summary>Low 16 bits of <c>DamageTaken.Hits</c>.</summary>
        public ushort Hits;
        /// <summary>Low 16 bits of <c>Weapon.Shots</c>.</summary>
        public ushort Shots;
        /// <summary>Net id of the killer while dead, else 0.</summary>
        public int Killer;

        public bool Has(SnapshotFlags flag)
        {
            return (Flags & flag) != 0;
        }
    }
}

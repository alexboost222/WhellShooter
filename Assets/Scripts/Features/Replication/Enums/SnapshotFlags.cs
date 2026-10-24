using System;

namespace Features.Replication.Enums
{
    /// <summary>Per-body bits of a <c>Snapshot</c>. Wire format — never renumber.</summary>
    [Flags]
    public enum SnapshotFlags : byte
    {
        None = 0,
        Bot = 1 << 0,
        Dead = 1 << 1,
        Aiming = 1 << 2,
        Grounded = 1 << 3,
    }
}

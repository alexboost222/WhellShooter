using Features.Network.Constants;

namespace Features.Replication.Utils
{
    /// <summary>One decoded <c>Snapshot</c>: the server's tick, arena seed, your body and every body.</summary>
    public sealed class SnapshotFrame
    {
        public readonly BodySnapshot[] Bodies = new BodySnapshot[SnapshotPacket.MaxBodies];

        public int Tick = NetConstants.NoTick;
        public int Seed;
        /// <summary>Net id of the receiving client's own body, 0 = none yet.</summary>
        public int You;
        public int Count;

        public bool TryFind(int id, out BodySnapshot body)
        {
            for (var i = 0; i < Count; i++)
            {
                if (Bodies[i].Id == id)
                {
                    body = Bodies[i];
                    return true;
                }
            }

            body = default;
            return false;
        }
    }
}

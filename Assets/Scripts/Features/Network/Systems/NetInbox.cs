using System.Collections.Generic;

namespace Features.Network.Systems
{
    /// <summary>
    /// Client: received datagrams the network feature does not handle itself (<c>Snapshot</c>), in
    /// arrival order. Filled by <c>ClientReceiveSystem</c>, read-then-cleared by replication.
    /// </summary>
    public sealed class NetInbox
    {
        public List<byte[]> Packets { get; } = new List<byte[]>();
    }
}

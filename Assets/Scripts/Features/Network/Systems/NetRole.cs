namespace Features.Network.Systems
{
    /// <summary>
    /// Which side this process runs, for features that install only their side (Docs/05 §Roles).
    /// Registered by <c>NetworkFeature</c>, first in the list.
    /// </summary>
    public sealed class NetRole
    {
        public NetRole(bool isServer)
        {
            IsServer = isServer;
        }

        public bool IsServer { get; }

        public bool IsClient => !IsServer;
    }
}

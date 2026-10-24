namespace Features.Network.Enums
{
    public enum ClientSyncState : byte
    {
        /// <summary>No connection.</summary>
        Connecting,

        /// <summary>Connected, collecting RTT samples before picking a first tick.</summary>
        Measuring,

        /// <summary>Ticking and sending input.</summary>
        Running,
    }
}

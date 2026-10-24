namespace Engine.Net
{
    public enum NetEventType : byte
    {
        Connected,
        Disconnected,
        Data,
    }

    /// <summary>
    /// One thing a transport has to say, handed out by <see cref="INetTransport.Poll"/> — the only
    /// way network news reaches the game.
    /// </summary>
    /// <remarks>
    /// <see cref="Data"/> is a copy owned by the event: transports reuse their receive buffers, and a
    /// copy per packet is cheap at a few packets per tick.
    /// </remarks>
    public readonly struct NetEvent
    {
        public NetEvent(NetEventType type, int peer, byte[] data = null)
        {
            Type = type;
            Peer = peer;
            Data = data;
        }

        public NetEventType Type { get; }

        /// <summary>Transport-local peer id. A client's only peer — the server — is 0.</summary>
        public int Peer { get; }

        public byte[] Data { get; }
    }
}

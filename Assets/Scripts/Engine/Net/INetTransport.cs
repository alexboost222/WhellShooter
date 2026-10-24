using System;
using System.Collections.Generic;

namespace Engine.Net
{
    /// <summary>
    /// Unreliable datagrams between one server and its clients (Docs/05). Polled once per tick by
    /// the network systems; nothing is raised from a callback.
    /// </summary>
    public interface INetTransport : IDisposable
    {
        bool IsServer { get; }

        /// <summary>Server: listen. Client: start connecting (and keep retrying).</summary>
        void Start();

        /// <summary>
        /// Advances the transport's own clock by <paramref name="deltaTime"/> and appends everything
        /// that arrived since the last poll.
        /// </summary>
        void Poll(float deltaTime, List<NetEvent> into);

        /// <summary>Unreliable, unordered. <paramref name="data"/> may be reused once this returns.</summary>
        void Send(int peer, byte[] data, int length);
    }
}

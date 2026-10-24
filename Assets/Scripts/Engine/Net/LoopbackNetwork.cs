using System;
using System.Collections.Generic;
using Engine.Diagnostics;

namespace Engine.Net
{
    /// <summary>
    /// An in-process "network" of one server and any number of clients, for headless tests. Instant
    /// and lossless — wrap the ends in <see cref="LinkConditioner"/> to make it bad.
    /// </summary>
    public sealed class LoopbackNetwork
    {
        private readonly List<Endpoint> _clients = new List<Endpoint>();

        private Endpoint _server;

        public INetTransport CreateServer()
        {
            Guard.ThrowIf(_server != null, "A loopback network has one server.");

            _server = new Endpoint(this, -1);
            return _server;
        }

        public INetTransport CreateClient()
        {
            var client = new Endpoint(this, _clients.Count);
            _clients.Add(client);
            return client;
        }

        private sealed class Endpoint : INetTransport
        {
            private readonly LoopbackNetwork _network;

            // Server: -1. Client: its peer id on the server.
            private readonly int _clientId;

            private readonly List<NetEvent> _inbox = new List<NetEvent>();

            private bool _started;
            private bool _connected;
            private bool _disposed;

            public Endpoint(LoopbackNetwork network, int clientId)
            {
                _network = network;
                _clientId = clientId;
            }

            public bool IsServer => _clientId < 0;

            public void Start()
            {
                _started = true;
            }

            public void Poll(float deltaTime, List<NetEvent> into)
            {
                // A client connects on its first poll after both ends have started, like a real
                // client retrying until the server is up.
                if (!IsServer && _started && !_connected && !_disposed)
                {
                    var server = _network._server;
                    if (server != null && server._started && !server._disposed)
                    {
                        _connected = true;
                        server._inbox.Add(new NetEvent(NetEventType.Connected, _clientId));
                        _inbox.Add(new NetEvent(NetEventType.Connected, 0));
                    }
                }

                into.AddRange(_inbox);
                _inbox.Clear();
            }

            public void Send(int peer, byte[] data, int length)
            {
                var target = IsServer ? Client(peer) : _network._server;
                if (target == null || target._disposed || (!IsServer && !_connected))
                {
                    return;
                }

                var copy = new byte[length];
                Buffer.BlockCopy(data, 0, copy, 0, length);
                target._inbox.Add(new NetEvent(NetEventType.Data, IsServer ? 0 : _clientId, copy));
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                if (!IsServer && _connected)
                {
                    _network._server?._inbox.Add(new NetEvent(NetEventType.Disconnected, _clientId));
                }
            }

            private Endpoint Client(int peer)
            {
                var clients = _network._clients;
                if (peer < 0 || peer >= clients.Count || !clients[peer]._connected)
                {
                    return null;
                }

                return clients[peer];
            }
        }
    }
}

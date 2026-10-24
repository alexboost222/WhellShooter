using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Engine.Diagnostics.Logs;
using LiteNetLib;

namespace Engine.Net
{
    /// <summary>
    /// <see cref="INetTransport"/> over LiteNetLib, unreliable channel only. Events are queued by the
    /// listener during <c>PollEvents</c> (main thread, <c>UnsyncedEvents</c> off) and handed out
    /// from <see cref="Poll"/>.
    /// </summary>
    /// <remarks>
    /// A client that loses or never gets its connection retries every
    /// <see cref="ReconnectDelaySeconds"/>, so start order between server and clients does not matter.
    /// </remarks>
    public sealed class LiteNetTransport : INetTransport
    {
        public const float ReconnectDelaySeconds = 1f;

        private readonly string _address;
        private readonly int _port;
        private readonly string _key;

        private readonly List<NetEvent> _queue = new List<NetEvent>();
        private readonly Dictionary<int, NetPeer> _peers = new Dictionary<int, NetPeer>();

        private EventBasedNetListener _listener;
        private NetManager _manager;
        private NetPeer _server;
        private float _reconnectIn;

        public LiteNetTransport(bool isServer, string address, int port, string key)
        {
            IsServer = isServer;
            _address = address;
            _port = port;
            _key = key;
        }

        public bool IsServer { get; }

        public void Start()
        {
            _listener = new EventBasedNetListener();
            _listener.ConnectionRequestEvent += OnConnectionRequest;
            _listener.PeerConnectedEvent += OnPeerConnected;
            _listener.PeerDisconnectedEvent += OnPeerDisconnected;
            _listener.NetworkReceiveEvent += OnNetworkReceive;

            _manager = new NetManager(_listener)
            {
                AutoRecycle = true,
                UnsyncedEvents = false,
                DisconnectTimeout = 5000,
            };

            if (IsServer)
            {
                if (!IPAddress.TryParse(_address, out var bind) || bind.AddressFamily != AddressFamily.InterNetwork)
                {
                    throw Log.Fail(LogChannels.Net, $"Server bind address '{_address}' is not an IPv4 address.");
                }

                _manager.IPv6Enabled = false;
                if (!_manager.Start(bind, IPAddress.IPv6Any, _port))
                {
                    throw Log.Fail(LogChannels.Net, $"Server could not listen on UDP {_address}:{_port}.");
                }

                Log.Info(LogChannels.Net, $"Server listening on UDP {_address}:{_port}.");
            }
            else
            {
                _manager.Start();
                Connect();
            }
        }

        public void Poll(float deltaTime, List<NetEvent> into)
        {
            if (_manager == null)
            {
                return;
            }

            if (!IsServer && _server == null)
            {
                _reconnectIn -= deltaTime;
                if (_reconnectIn <= 0f)
                {
                    Connect();
                }
            }

            _manager.PollEvents();
            into.AddRange(_queue);
            _queue.Clear();
        }

        public void Send(int peer, byte[] data, int length)
        {
            if (IsServer)
            {
                if (_peers.TryGetValue(peer, out var target))
                {
                    target.Send(data, 0, length, DeliveryMethod.Unreliable);
                }
            }
            else if (_server != null && _server.ConnectionState == ConnectionState.Connected)
            {
                _server.Send(data, 0, length, DeliveryMethod.Unreliable);
            }
        }

        public void Dispose()
        {
            _manager?.Stop();
            _manager = null;
            _server = null;
            _peers.Clear();
            _queue.Clear();
        }

        private void Connect()
        {
            _reconnectIn = ReconnectDelaySeconds;
            _manager.Connect(_address, _port, _key);
        }

        private void OnConnectionRequest(ConnectionRequest request)
        {
            if (IsServer)
            {
                request.AcceptIfKey(_key);
            }
            else
            {
                request.Reject();
            }
        }

        private void OnPeerConnected(NetPeer peer)
        {
            if (IsServer)
            {
                _peers[peer.Id] = peer;
                _queue.Add(new NetEvent(NetEventType.Connected, peer.Id));
                Log.Info(LogChannels.Net, $"Client {peer.Id} connected from {peer.Address}:{peer.Port}.");
            }
            else
            {
                _server = peer;
                _queue.Add(new NetEvent(NetEventType.Connected, 0));
                Log.Info(LogChannels.Net, $"Connected to {_address}:{_port}.");
            }
        }

        private void OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
        {
            if (IsServer)
            {
                if (_peers.Remove(peer.Id))
                {
                    _queue.Add(new NetEvent(NetEventType.Disconnected, peer.Id));
                    Log.Info(LogChannels.Net, $"Client {peer.Id} disconnected: {info.Reason}.");
                }
            }
            else
            {
                var wasConnected = _server != null;
                _server = null;
                _reconnectIn = ReconnectDelaySeconds;
                if (wasConnected)
                {
                    _queue.Add(new NetEvent(NetEventType.Disconnected, 0));
                    Log.Info(LogChannels.Net, $"Disconnected from server: {info.Reason}.");
                }
            }
        }

        private void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod method)
        {
            var data = reader.GetRemainingBytes();
            _queue.Add(new NetEvent(NetEventType.Data, IsServer ? peer.Id : 0, data));
        }
    }
}

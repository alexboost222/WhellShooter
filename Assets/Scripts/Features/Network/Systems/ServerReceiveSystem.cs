using System.Collections.Generic;
using Engine.DI;
using Engine.Diagnostics.Logs;
using Engine.Ecs;
using Engine.Net;
using Features.Network.Components;
using Features.Network.Configs;
using Features.Network.Constants;
using Features.Network.Enums;
using Features.Network.Utils;
using Scellecs.Morpeh;

namespace Features.Network.Systems
{
    /// <summary>
    /// Server: polls the transport, creates / tags-destroy client entities, and files every
    /// received input into its client's ring with the server tick it first arrived on (Docs/05).
    /// </summary>
    /// <remarks>
    /// Inputs more than <see cref="NetConfig.MaxInputLeadMs"/> ahead of the server tick are dropped
    /// and not acked: the client resends them, and they are taken once the server is close enough.
    /// </remarks>
    public sealed class ServerReceiveSystem : ISystem
    {
        [Inject] private Stash<NetClock> _netClock;
        [Inject] private Stash<ServerClient> _serverClient;
        [Inject] private Stash<ServerClientStats> _serverClientStats;
        [Inject] private Stash<ConsumedInput> _consumedInput;
        [Inject] private Stash<Destroy> _destroy;

        [Inject] private INetTransport _transport;
        [Inject] private NetConfig _config;

        public World World { get; set; }

        private readonly List<NetEvent> _events = new List<NetEvent>();

        // Peer → entity. Filters only see an entity after a commit, and a client's first data can
        // arrive in the same poll as its connect.
        private readonly Dictionary<int, Entity> _clients = new Dictionary<int, Entity>();

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<NetClock>()
                .Build();
        }

        public void Dispose()
        {
            _clients.Clear();
        }

        public void OnUpdate(float deltaTime)
        {
            var tick = _netClock.Get(_filter.First()).Tick;
            var maxLead = SlackMath.MaxLeadTicks(_config.MaxInputLeadMs, deltaTime);

            foreach (var pair in _clients)
            {
                _serverClient.Get(pair.Value).SampleTick = NetConstants.NoTick;
            }

            _events.Clear();
            _transport.Poll(deltaTime, _events);

            for (var i = 0; i < _events.Count; i++)
            {
                var netEvent = _events[i];
                switch (netEvent.Type)
                {
                    case NetEventType.Connected:
                        Connect(netEvent.Peer);
                        break;
                    case NetEventType.Disconnected:
                        Disconnect(netEvent.Peer);
                        break;
                    case NetEventType.Data:
                        if (_clients.TryGetValue(netEvent.Peer, out var entity))
                        {
                            Receive(entity, netEvent.Data, tick, maxLead);
                        }

                        break;
                }
            }
        }

        private void Connect(int peer)
        {
            if (_clients.ContainsKey(peer))
            {
                return;
            }

            var entity = World.CreateEntity();
            var ticks = new int[NetConstants.RingSize];
            for (var i = 0; i < ticks.Length; i++)
            {
                ticks[i] = NetConstants.NoTick;
            }

            _serverClient.Set(entity, new ServerClient
            {
                Peer = peer,
                Inputs = new PlayerInput[NetConstants.RingSize],
                InputTicks = ticks,
                ArrivalTicks = new int[NetConstants.RingSize],
                FirstTick = NetConstants.NoTick,
                NextTick = NetConstants.NoTick,
                ReceivedThrough = NetConstants.NoTick,
                NewestTick = NetConstants.NoTick,
                SampleTick = NetConstants.NoTick,
                LastMissedTick = NetConstants.NoTick,
                LossRate = -1f,
            });
            _serverClientStats.Set(entity, new ServerClientStats());
            _consumedInput.Set(entity, new ConsumedInput { Tick = NetConstants.NoTick });
            _clients.Add(peer, entity);

            Log.Info(LogChannels.Net, $"[Server] client {peer} joined.");
        }

        private void Disconnect(int peer)
        {
            if (!_clients.TryGetValue(peer, out var entity))
            {
                return;
            }

            _clients.Remove(peer);
            _destroy.Set(entity);

            ref var stats = ref _serverClientStats.Get(entity);
            Log.Info(LogChannels.Net,
                $"[Server] client {peer} left: consumed {stats.Consumed}, missed {stats.Missed}, late {stats.Late}.");
        }

        private void Receive(Entity entity, byte[] data, int tick, int maxLead)
        {
            var reader = new PacketReader(data);
            if ((PacketType)reader.ReadByte() != PacketType.Input)
            {
                return;
            }

            var frame = reader.ReadUInt();
            var firstTick = reader.ReadInt();
            var count = reader.ReadByte();
            if (!reader.IsValid || reader.Remaining != count * PlayerInput.Size)
            {
                return;
            }

            ref var client = ref _serverClient.Get(entity);
            ref var stats = ref _serverClientStats.Get(entity);
            stats.IntervalPackets++;
            TrackFrame(ref client, frame, tick);

            if (count == 0)
            {
                return;
            }

            if (client.FirstTick == NetConstants.NoTick)
            {
                client.FirstTick = firstTick;
                client.NextTick = firstTick;
                client.ReceivedThrough = firstTick - 1;
                client.NewestTick = firstTick - 1;
            }

            for (var i = 0; i < count; i++)
            {
                var input = Packets.ReadInput(ref reader);
                var inputTick = firstTick + i;
                var slot = inputTick & NetConstants.RingMask;

                if (inputTick <= client.ReceivedThrough || client.InputTicks[slot] == inputTick)
                {
                    continue;
                }

                if (inputTick > tick + maxLead || inputTick >= client.NextTick + NetConstants.RingSize)
                {
                    stats.Rejected++;
                    stats.IntervalRejected++;
                    continue;
                }

                client.Inputs[slot] = input;
                client.InputTicks[slot] = inputTick;
                client.ArrivalTicks[slot] = tick;

                if (inputTick < client.NextTick)
                {
                    stats.Late++;
                    stats.IntervalLate++;
                }
            }

            while (client.InputTicks[(client.ReceivedThrough + 1) & NetConstants.RingMask] == client.ReceivedThrough + 1)
            {
                client.ReceivedThrough++;
            }

            // Only a packet that brings a new newest tick carries that tick's first copy, so its
            // slack measures the lead and the jitter, not the loss.
            var newest = firstTick + count - 1;
            if (newest > client.NewestTick)
            {
                client.NewestTick = newest;
                var slack = newest - tick;
                if (client.SampleTick == NetConstants.NoTick || slack < client.SampleSlack)
                {
                    client.SampleTick = newest;
                    client.SampleSlack = slack;
                }
            }
        }

        private void TrackFrame(ref ServerClient client, uint frame, int tick)
        {
            if (!client.HasFrame)
            {
                client.HasFrame = true;
                client.LastFrame = frame;
                client.LastFrameTick = tick;
                client.LossExpected = 1;
                return;
            }

            if (frame > client.LastFrame)
            {
                var gap = (int)(frame - client.LastFrame - 1);
                client.LossLost += gap;
                client.LossExpected += gap + 1;
                client.LastFrame = frame;
                client.LastFrameTick = tick;
            }
            else if (client.LossLost > 0)
            {
                // Reordered, not lost.
                client.LossLost--;
            }

            if (client.LossExpected >= _config.LossWindow)
            {
                client.LossRate = (float)client.LossLost / client.LossExpected;
                client.LossLost = 0;
                client.LossExpected = 0;
            }
        }
    }
}

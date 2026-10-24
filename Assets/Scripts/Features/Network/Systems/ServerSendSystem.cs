using System;
using Engine.DI;
using Engine.Ecs;
using Engine.Net;
using Features.Network.Components;
using Features.Network.Constants;
using Features.Network.Utils;
using Scellecs.Morpeh;

namespace Features.Network.Systems
{
    /// <summary>Server: one <c>Sync</c> packet per client per tick — tick, ack, echo, slack sample, loss (Docs/05).</summary>
    public sealed class ServerSendSystem : ISystem
    {
        [Inject] private Stash<NetClock> _netClock;
        [Inject] private Stash<ServerClient> _serverClient;

        [Inject] private INetTransport _transport;

        public World World { get; set; }

        private readonly PacketWriter _writer = new PacketWriter(64);

        private Filter _filter;
        private Filter _clockFilter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<ServerClient>()
                .Without<Destroy>()
                .Build();
            _clockFilter = World.Filter
                .With<NetClock>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            var tick = _netClock.Get(_clockFilter.First()).Tick;

            foreach (var entity in _filter)
            {
                ref var client = ref _serverClient.Get(entity);

                var packet = new SyncPacket
                {
                    ServerTick = tick,
                    EchoFrame = client.HasFrame ? client.LastFrame : 0u,
                    EchoAge = (byte)Math.Max(0, Math.Min(255, tick - client.LastFrameTick)),
                    AckTick = client.FirstTick == NetConstants.NoTick ? NetConstants.NoTick : client.ReceivedThrough,
                    SampleTick = client.SampleTick,
                    Slack = (sbyte)Math.Max(sbyte.MinValue, Math.Min(sbyte.MaxValue, client.SampleSlack)),
                    LossHalfPercent = client.LossRate < 0f
                        ? SyncPacket.LossUnknown
                        : (byte)Math.Round(Math.Min(1f, client.LossRate) * 200f),
                    LastMissedTick = client.LastMissedTick,
                };

                Packets.WriteSync(_writer, in packet);
                _transport.Send(client.Peer, _writer.Buffer, _writer.Length);
            }
        }
    }
}

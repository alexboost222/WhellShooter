using System;
using Engine.DI;
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
    /// Client: one <c>Input</c> packet per step carrying <b>every unacked input</b> (newest
    /// <see cref="NetConfig.MaxInputsPerPacket"/> if over) — the redundancy (Docs/05). While
    /// measuring it sends empty ones so the server has frames to echo.
    /// </summary>
    public sealed class ClientSendSystem : ISystem
    {
        [Inject] private Stash<NetClock> _netClock;
        [Inject] private Stash<ClientSync> _clientSync;

        [Inject] private INetTransport _transport;
        [Inject] private NetConfig _config;

        public World World { get; set; }

        private readonly PacketWriter _writer = new PacketWriter();

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<NetClock>()
                .With<ClientSync>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            var session = _filter.First();
            ref var clock = ref _netClock.Get(session);
            ref var sync = ref _clientSync.Get(session);

            if (sync.State == ClientSyncState.Connecting)
            {
                return;
            }

            if (sync.State != ClientSyncState.Running)
            {
                Packets.WriteInputHeader(_writer, sync.Frame, NetConstants.NoTick, 0);
                _transport.Send(0, _writer.Buffer, _writer.Length);
                return;
            }

            var maxInputs = Math.Max(1, Math.Min(Math.Min(_config.MaxInputsPerPacket, 255), NetConstants.RingSize - 1));
            var first = Math.Max(sync.AckTick + 1, clock.Tick - maxInputs + 1);
            var count = clock.Tick - first + 1;

            Packets.WriteInputHeader(_writer, sync.Frame, first, count);
            for (var tick = first; tick <= clock.Tick; tick++)
            {
                Packets.WriteInput(_writer, in sync.Inputs[tick & NetConstants.RingMask]);
            }

            _transport.Send(0, _writer.Buffer, _writer.Length);
        }
    }
}

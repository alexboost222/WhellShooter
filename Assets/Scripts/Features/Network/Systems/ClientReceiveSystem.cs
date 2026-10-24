using System;
using System.Collections.Generic;
using Engine.DI;
using Engine.Diagnostics.Logs;
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
    /// Client: polls the transport, applies <c>Sync</c> packets (ack, RTT, loss) and decides from
    /// the server's slack samples whether the tick must jump or hold (Docs/05). The tick system
    /// executes the decision in the same step.
    /// </summary>
    public sealed class ClientReceiveSystem : ISystem
    {
        [Inject] private Stash<NetClock> _netClock;
        [Inject] private Stash<ClientSync> _clientSync;

        [Inject] private INetTransport _transport;
        [Inject] private NetConfig _config;

        public World World { get; set; }

        private readonly List<NetEvent> _events = new List<NetEvent>();

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
            var maxLead = SlackMath.MaxLeadTicks(_config.MaxInputLeadMs, deltaTime);

            _events.Clear();
            _transport.Poll(deltaTime, _events);

            for (var i = 0; i < _events.Count; i++)
            {
                var netEvent = _events[i];
                switch (netEvent.Type)
                {
                    case NetEventType.Connected:
                        sync.State = ClientSyncState.Measuring;
                        sync.RttSamples = 0;
                        sync.WorstRtt = 0;
                        break;
                    case NetEventType.Disconnected:
                        // The tick keeps growing; a reconnect re-measures and starts past it.
                        sync.State = ClientSyncState.Connecting;
                        sync.AckTick = clock.Tick;
                        break;
                    case NetEventType.Data:
                        Receive(ref clock, ref sync, netEvent.Data, maxLead);
                        break;
                }
            }
        }

        private void Receive(ref NetClock clock, ref ClientSync sync, byte[] data, int maxLead)
        {
            var reader = new PacketReader(data);
            if ((PacketType)reader.ReadByte() != PacketType.Sync || !Packets.TryReadSync(ref reader, out var packet))
            {
                return;
            }

            if (sync.State == ClientSyncState.Connecting)
            {
                return;
            }

            if (packet.ServerTick > sync.ServerTick)
            {
                sync.ServerTick = packet.ServerTick;
                sync.ServerTickFrame = sync.Frame;
            }

            if (packet.EchoFrame != 0u && packet.EchoFrame <= sync.Frame)
            {
                sync.Rtt = Math.Max(0, (int)(sync.Frame - packet.EchoFrame) - packet.EchoAge);
                if (sync.State == ClientSyncState.Measuring)
                {
                    sync.RttSamples++;
                    sync.WorstRtt = Math.Max(sync.WorstRtt, sync.Rtt);
                }
            }

            if (sync.State != ClientSyncState.Running)
            {
                return;
            }

            if (packet.AckTick > sync.AckTick)
            {
                sync.AckTick = Math.Min(packet.AckTick, clock.Tick);
            }

            var slackCap = Math.Min(_config.MaxSlack, maxLead);
            if (packet.LossHalfPercent == SyncPacket.LossUnknown)
            {
                // Not measured yet: be generous rather than miss the first seconds.
                sync.LossRate = -1f;
                sync.NeedSlack = Math.Min(Math.Max(_config.StartSlack, _config.MinSlack), slackCap);
            }
            else
            {
                sync.LossRate = packet.LossHalfPercent / 200f;
                sync.NeedSlack = SlackMath.NeedSlack(sync.LossRate, _config.TargetMissChance, _config.MinSlack,
                    slackCap);
            }

            if (sync.PendingJump > 0 || sync.PendingHolds > 0)
            {
                return;
            }

            if (packet.LastMissedTick != NetConstants.NoTick && packet.LastMissedTick >= sync.Barrier &&
                packet.LastMissedTick > sync.LastMissedTick)
            {
                sync.LastMissedTick = packet.LastMissedTick;
                sync.PendingJump = Math.Max(1, sync.NeedSlack + 1);
                Log.Warning(LogChannels.Net,
                    $"[Client] server missed tick {packet.LastMissedTick}; jumping {sync.PendingJump} ticks.");
                return;
            }

            if (packet.SampleTick == NetConstants.NoTick || packet.SampleTick < sync.Barrier)
            {
                return;
            }

            sync.LastSlack = packet.Slack;
            if (sync.WindowCount == 0)
            {
                sync.WindowMin = packet.Slack;
                sync.WindowMax = packet.Slack;
            }
            else
            {
                sync.WindowMin = Math.Min(sync.WindowMin, packet.Slack);
                sync.WindowMax = Math.Max(sync.WindowMax, packet.Slack);
            }

            sync.WindowSum += packet.Slack;
            sync.WindowCount++;

            // A young window has not seen the jitter's low end yet: estimate it from the mean and
            // the spread of the last full window, whichever is lower.
            var mean = (int)Math.Floor((double)sync.WindowSum / sync.WindowCount);
            var floor = Math.Min(sync.WindowMin, mean - (sync.JitterSpread + 1) / 2);

            if (floor < sync.NeedSlack)
            {
                // Never aim past the server's lead cap — unless that is the only way not to miss.
                var jump = Math.Max(Math.Min(sync.NeedSlack - floor, maxLead - sync.WindowMax), -sync.WindowMin);
                if (jump > 0)
                {
                    sync.PendingJump = jump;
                    return;
                }
            }

            if (sync.WindowMax > maxLead)
            {
                sync.PendingHolds = Math.Max(0, Math.Min(sync.WindowMax - maxLead, sync.WindowMin - sync.NeedSlack));
                if (sync.PendingHolds > 0)
                {
                    return;
                }
            }

            if (sync.WindowCount >= _config.SlackWindow)
            {
                sync.JitterSpread = sync.WindowMax - sync.WindowMin;
                sync.PendingHolds = Math.Max(0, sync.WindowMin - sync.NeedSlack - _config.Hysteresis);
                ResetWindow(ref sync);
            }
        }

        private static void ResetWindow(ref ClientSync sync)
        {
            sync.WindowCount = 0;
            sync.WindowSum = 0;
            sync.WindowMin = 0;
            sync.WindowMax = 0;
        }
    }
}

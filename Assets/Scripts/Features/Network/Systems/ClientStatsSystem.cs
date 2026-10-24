using Engine.DI;
using Engine.Diagnostics.Logs;
using Features.Network.Components;
using Features.Network.Configs;
using Features.Network.Enums;
using Scellecs.Morpeh;

namespace Features.Network.Systems
{
    /// <summary>Client: one summary line every <see cref="NetConfig.StatsIntervalTicks"/> frames.</summary>
    public sealed class ClientStatsSystem : ISystem
    {
        [Inject] private Stash<NetClock> _netClock;
        [Inject] private Stash<ClientSync> _clientSync;

        [Inject] private NetConfig _config;

        public World World { get; set; }

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

            if (sync.State != ClientSyncState.Running || _config.StatsIntervalTicks <= 0 ||
                sync.Frame % (uint)_config.StatsIntervalTicks != 0)
            {
                return;
            }

            var serverNow = sync.ServerTick + (int)(sync.Frame - sync.ServerTickFrame);
            Log.Info(LogChannels.Net,
                $"[Client] tick {clock.Tick} lead {clock.Tick - serverNow} rtt {sync.Rtt} " +
                $"slack {sync.LastSlack} need {sync.NeedSlack} loss {(sync.LossRate < 0f ? "?" : sync.LossRate.ToString("P1"))} " +
                $"unacked {clock.Tick - sync.AckTick} jumps {sync.Jumps} (+{sync.JumpedTicks}) holds {sync.Holds}");
        }
    }
}

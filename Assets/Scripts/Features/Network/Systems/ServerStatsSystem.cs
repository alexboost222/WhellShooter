using Engine.DI;
using Engine.Diagnostics.Logs;
using Engine.Ecs;
using Features.Network.Components;
using Features.Network.Configs;
using Scellecs.Morpeh;

namespace Features.Network.Systems
{
    /// <summary>Server: one summary line per client every <see cref="NetConfig.StatsIntervalTicks"/>.</summary>
    public sealed class ServerStatsSystem : ISystem
    {
        [Inject] private Stash<NetClock> _netClock;
        [Inject] private Stash<ServerClient> _serverClient;
        [Inject] private Stash<ServerClientStats> _serverClientStats;

        [Inject] private NetConfig _config;

        public World World { get; set; }

        private Filter _filter;
        private Filter _clockFilter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<ServerClient>()
                .With<ServerClientStats>()
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
            if (_config.StatsIntervalTicks <= 0 || tick % _config.StatsIntervalTicks != 0)
            {
                return;
            }

            foreach (var entity in _filter)
            {
                ref var client = ref _serverClient.Get(entity);
                ref var stats = ref _serverClientStats.Get(entity);

                Log.Info(LogChannels.Net,
                    $"[Server] tick {tick} client {client.Peer}: inputs {stats.IntervalConsumed} " +
                    $"missed {stats.IntervalMissed} late {stats.IntervalLate} rejected {stats.IntervalRejected} " +
                    $"packets {stats.IntervalPackets} slack {stats.IntervalSlackMin}..{stats.IntervalSlackMax} " +
                    $"loss {(client.LossRate < 0f ? "?" : client.LossRate.ToString("P1"))} ack {client.ReceivedThrough} | total {stats.Consumed} missed {stats.Missed}");

                stats.IntervalConsumed = 0;
                stats.IntervalMissed = 0;
                stats.IntervalLate = 0;
                stats.IntervalRejected = 0;
                stats.IntervalPackets = 0;
                stats.IntervalSlackMin = 0;
                stats.IntervalSlackMax = 0;
            }
        }
    }
}

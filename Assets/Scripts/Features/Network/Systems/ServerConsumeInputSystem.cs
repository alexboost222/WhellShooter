using Engine.DI;
using Engine.Diagnostics.Logs;
using Engine.Ecs;
using Features.Network.Components;
using Features.Network.Configs;
using Features.Network.Constants;
using Scellecs.Morpeh;

namespace Features.Network.Systems
{
    /// <summary>
    /// Server: takes each client's input for the current tick into <see cref="ConsumedInput"/>.
    /// Missing → missed, last input repeated (Docs/05).
    /// </summary>
    public sealed class ServerConsumeInputSystem : ISystem
    {
        [Inject] private Stash<NetClock> _netClock;
        [Inject] private Stash<ServerClient> _serverClient;
        [Inject] private Stash<ServerClientStats> _serverClientStats;
        [Inject] private Stash<ConsumedInput> _consumedInput;

        [Inject] private NetConfig _config;

        public World World { get; set; }

        private Filter _filter;
        private Filter _clockFilter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<ServerClient>()
                .With<ServerClientStats>()
                .With<ConsumedInput>()
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
                if (client.FirstTick == NetConstants.NoTick)
                {
                    continue;
                }

                ref var stats = ref _serverClientStats.Get(entity);
                ref var consumed = ref _consumedInput.Get(entity);

                // Normally one tick; more only right after the first packet, if it came in late.
                while (client.NextTick <= tick)
                {
                    Consume(ref client, ref stats, ref consumed, client.NextTick);
                    client.NextTick++;
                }
            }
        }

        private void Consume(ref ServerClient client, ref ServerClientStats stats, ref ConsumedInput consumed, int inputTick)
        {
            var slot = inputTick & NetConstants.RingMask;
            var present = client.InputTicks[slot] == inputTick && client.ArrivalTicks[slot] <= inputTick;

            consumed.Tick = inputTick;
            consumed.Missed = !present;

            if (present)
            {
                var slack = inputTick - client.ArrivalTicks[slot];
                client.LastInput = client.Inputs[slot];
                consumed.Input = client.LastInput;

                if (stats.IntervalConsumed == 0 || slack < stats.IntervalSlackMin)
                {
                    stats.IntervalSlackMin = slack;
                }

                if (stats.IntervalConsumed == 0 || slack > stats.IntervalSlackMax)
                {
                    stats.IntervalSlackMax = slack;
                }

                stats.Consumed++;
                stats.IntervalConsumed++;

                if (_config.LogEveryInput)
                {
                    Log.Info(LogChannels.Net,
                        $"[Server] tick {inputTick} client {client.Peer}: {consumed.Input} (arrived {slack} ticks early)");
                }
            }
            else
            {
                consumed.Input = client.LastInput;
                client.LastMissedTick = inputTick;
                stats.Missed++;
                stats.IntervalMissed++;

                Log.Warning(LogChannels.Net,
                    $"[Server] tick {inputTick} client {client.Peer}: input MISSED, repeating last.");
            }
        }
    }
}

using System;
using Engine.DI;
using Engine.Diagnostics.Logs;
using Features.Network.Components;
using Features.Network.Configs;
using Features.Network.Constants;
using Features.Network.Enums;
using Features.Network.Providers;
using Scellecs.Morpeh;

namespace Features.Network.Systems
{
    /// <summary>
    /// Client: advances the tick — <c>+1</c>, a jump <c>+1+k</c> or a hold <c>+0</c> — and samples
    /// input for every tick it produces (Docs/05). The tick never decreases.
    /// </summary>
    public sealed class ClientTickSystem : ISystem
    {
        [Inject] private Stash<NetClock> _netClock;
        [Inject] private Stash<ClientSync> _clientSync;

        [Inject] private IPlayerInputSource _input;
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

            sync.Frame++;

            if (sync.State == ClientSyncState.Measuring && sync.RttSamples >= _config.SyncSamples)
            {
                Start(ref clock, ref sync);
                return;
            }

            if (sync.State != ClientSyncState.Running)
            {
                return;
            }

            // One held step per pending hold; the window restarts after the last.
            if (sync.PendingHolds > 0)
            {
                sync.PendingHolds--;
                sync.Holds++;
                sync.Barrier = clock.Tick + 1;
                ResetWindow(ref sync);
                return;
            }

            var jump = sync.PendingJump;
            var input = _input.Sample();
            for (var i = 0; i <= jump; i++)
            {
                clock.Tick++;
                sync.Inputs[clock.Tick & NetConstants.RingMask] = input;
            }

            if (jump > 0)
            {
                sync.PendingJump = 0;
                sync.Jumps++;
                sync.JumpedTicks += jump;
                sync.Barrier = clock.Tick;
                ResetWindow(ref sync);
            }
        }

        private void Start(ref NetClock clock, ref ClientSync sync)
        {
            var serverNow = sync.ServerTick + (int)(sync.Frame - sync.ServerTickFrame);
            var start = serverNow + sync.WorstRtt + _config.StartSlack;
            if (clock.Tick != NetConstants.NoTick && start <= clock.Tick)
            {
                start = clock.Tick + 1;
            }

            clock.Tick = start;
            sync.FirstTick = start;
            sync.AckTick = start - 1;
            sync.Barrier = start;
            sync.NeedSlack = Math.Max(_config.StartSlack, _config.MinSlack);

            // Unknown jitter until the first full window: assume the start lead's worth.
            sync.JitterSpread = 2 * _config.StartSlack;
            sync.PendingJump = 0;
            sync.PendingHolds = 0;
            sync.State = ClientSyncState.Running;
            ResetWindow(ref sync);
            sync.Inputs[start & NetConstants.RingMask] = _input.Sample();

            Log.Info(LogChannels.Net,
                $"[Client] started at tick {start} (server ~{serverNow}, worst rtt {sync.WorstRtt} ticks).");
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

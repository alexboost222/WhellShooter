using Engine.DI;
using Features.CharacterFeature.Components;
using Features.Network.Components;
using Features.Network.Constants;
using Features.Network.Enums;
using Features.PlayerFeature.Components;
using Features.PlayerFeature.Utils;
using Scellecs.Morpeh;

namespace Features.PlayerFeature.Systems
{
    /// <summary>
    /// Client, on the tick: the input the client tick sampled for this tick → the local body's
    /// <see cref="CharacterIntent"/> — the same input, decoded the same way, as the server will
    /// simulate (Docs/05 §Prediction). Look is not touched: the client's own is per frame.
    /// </summary>
    /// <remarks>
    /// A step that produced no new tick (a hold, or not running yet) keeps the held input and drops
    /// the latches, so a jump or shot is never acted on twice.
    /// </remarks>
    public sealed class PlayerTickInputSystem : ISystem
    {
        [Inject] private Stash<NetClock> _netClock;
        [Inject] private Stash<ClientSync> _clientSync;
        [Inject] private Stash<CharacterIntent> _characterIntent;

        public World World { get; set; }

        private Filter _filter;
        private Filter _sessionFilter;
        private int _lastTick = NetConstants.NoTick;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Player>()
                .With<CharacterIntent>()
                .Build();
            _sessionFilter = World.Filter
                .With<NetClock>()
                .With<ClientSync>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            var session = _sessionFilter.First();
            var tick = _netClock.Get(session).Tick;
            ref var sync = ref _clientSync.Get(session);
            var running = sync.State == ClientSyncState.Running;
            var fresh = running && tick != _lastTick;
            _lastTick = tick;

            foreach (var entity in _filter)
            {
                ref var intent = ref _characterIntent.Get(entity);
                if (!running)
                {
                    intent = default;
                    continue;
                }

                PlayerInputCodec.ApplyIntent(sync.Inputs[tick & NetConstants.RingMask], ref intent, fresh);
            }
        }
    }
}

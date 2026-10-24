using Engine.DI;
using Features.CharacterFeature.Components;
using Features.Network.Components;
using Features.Network.Enums;
using Features.PlayerFeature.Components;
using Features.Replication.Components;
using Scellecs.Morpeh;

namespace Features.Replication.Systems
{
    /// <summary>
    /// Client, right after the physics step: the local body's feet and velocity under this tick, for
    /// <c>LocalBodySystem</c> to compare with the server's (Docs/05 §Prediction).
    /// </summary>
    public sealed class PredictionRecordSystem : ISystem
    {
        [Inject] private Stash<NetClock> _netClock;
        [Inject] private Stash<ClientSync> _clientSync;
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<Prediction> _prediction;

        public World World { get; set; }

        private Filter _filter;
        private Filter _sessionFilter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Player>()
                .With<Character>()
                .With<CharacterMotion>()
                .With<Prediction>()
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
            if (_clientSync.Get(session).State != ClientSyncState.Running)
            {
                return;
            }

            var tick = _netClock.Get(session).Tick;
            var slot = tick & Prediction.Mask;
            foreach (var entity in _filter)
            {
                var motion = _characterMotion.Get(entity);
                ref var prediction = ref _prediction.Get(entity);
                prediction.Ticks[slot] = tick;
                prediction.Feet[slot] = motion.Position;
                prediction.Velocity[slot] = motion.Velocity;
            }
        }
    }
}

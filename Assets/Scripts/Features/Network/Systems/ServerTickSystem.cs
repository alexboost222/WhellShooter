using Engine.DI;
using Features.Network.Components;
using Scellecs.Morpeh;

namespace Features.Network.Systems
{
    /// <summary>Server: starts the next tick. First in the tick, so everything after sees the tick being simulated.</summary>
    public sealed class ServerTickSystem : ISystem
    {
        [Inject] private Stash<NetClock> _netClock;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<NetClock>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            _netClock.Get(_filter.First()).Tick++;
        }
    }
}

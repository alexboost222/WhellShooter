using Engine.DI;
using Engine.Ecs;
using Features.CharacterFeature.Components;
using Features.Replication.Components;
using Scellecs.Morpeh;

namespace Features.Replication.Systems
{
    /// <summary>Server: every <see cref="Character"/> without a <see cref="NetEntity"/> gets the next id.</summary>
    public sealed class NetEntityIdSystem : ISystem
    {
        [Inject] private Stash<NetEntity> _netEntity;

        public World World { get; set; }

        private Filter _filter;
        private int _next = NetEntity.None;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Character>()
                .Without<NetEntity>()
                .Without<Destroy>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                _netEntity.Set(entity, new NetEntity { Id = ++_next });
            }
        }
    }
}

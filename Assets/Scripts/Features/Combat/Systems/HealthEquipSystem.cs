using Engine.DI;
using Features.CharacterFeature.Components;
using Features.Combat.Components;
using Features.Combat.Configs;
using Scellecs.Morpeh;

namespace Features.Combat.Systems
{
    /// <summary>Gives every body that has none a full <see cref="Health"/> — player and bots alike.</summary>
    public sealed class HealthEquipSystem : ISystem
    {
        [Inject] private Stash<Health> _health;

        [Inject] private CombatConfig _config;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Character>()
                .Without<Health>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                _health.Set(entity, new Health { Current = _config.MaxHealth, Max = _config.MaxHealth });
            }
        }
    }
}

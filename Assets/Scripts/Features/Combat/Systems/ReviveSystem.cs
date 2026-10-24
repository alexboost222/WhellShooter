using Engine.DI;
using Features.Combat.Components;
using Features.Combat.Configs;
using Scellecs.Morpeh;

namespace Features.Combat.Systems
{
    /// <summary>
    /// Consumes <see cref="ReviveRequest"/> (read-then-remove): full health, no longer
    /// <see cref="Dead"/>, and a fresh <see cref="DamageTaken"/> history is kept — its counter only
    /// goes up, so readers stay correct across lives.
    /// </summary>
    public sealed class ReviveSystem : ISystem
    {
        [Inject] private Stash<ReviveRequest> _reviveRequest;
        [Inject] private Stash<Health> _health;
        [Inject] private Stash<Dead> _dead;

        [Inject] private CombatConfig _config;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<ReviveRequest>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                _reviveRequest.Remove(entity);
                _dead.Remove(entity);
                _health.Set(entity, new Health { Current = _config.MaxHealth, Max = _config.MaxHealth });
            }
        }
    }
}

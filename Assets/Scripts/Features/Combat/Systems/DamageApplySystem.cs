using Engine.DI;
using Engine.Ecs;
using Features.Combat.Components;
using Features.Combat.Configs;
using Features.Combat.Utils;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Combat.Systems
{
    /// <summary>
    /// Consumes every <see cref="Damage"/> entity: lowers the target's <see cref="Health"/>, records
    /// <see cref="DamageTaken"/>, and marks the body <see cref="Dead"/> at zero. The damage entity
    /// itself is tagged <see cref="Destroy"/>.
    /// </summary>
    /// <remarks>
    /// The one place health goes down. A hit on a target that is gone, already dead, or a team-mate
    /// with friendly fire off is dropped silently — none of those is an error, they're races a
    /// shooter has all the time.
    /// </remarks>
    public sealed class DamageApplySystem : ISystem
    {
        [Inject] private Stash<Damage> _damage;
        [Inject] private Stash<Health> _health;
        [Inject] private Stash<DamageTaken> _damageTaken;
        [Inject] private Stash<Dead> _dead;
        [Inject] private Stash<Team> _team;
        [Inject] private Stash<Destroy> _destroy;

        [Inject] private CombatConfig _config;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Damage>()
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
                Apply(_damage.Get(entity));
                _destroy.Set(entity);
            }
        }

        private void Apply(Damage damage)
        {
            if (!World.TryGetEntity(damage.Target, out var target) || !_health.Has(target) || _dead.Has(target))
            {
                return;
            }

            if (!_config.FriendlyFire && damage.Source != damage.Target &&
                World.TryGetEntity(damage.Source, out var source) &&
                !CombatRules.AreHostile(TeamOf(source), TeamOf(target)))
            {
                return;
            }

            ref var health = ref _health.Get(target);
            health.Current = math.max(0f, health.Current - math.max(0f, damage.Amount));

            if (!_damageTaken.Has(target))
            {
                _damageTaken.Add(target);
            }

            ref var taken = ref _damageTaken.Get(target);
            taken.Hits++;
            taken.LastSource = damage.Source;
            taken.LastOrigin = damage.Origin;

            if (health.Current <= 0f)
            {
                _dead.Set(target, new Dead { Killer = damage.Source, Direction = damage.Direction });
            }
        }

        private int TeamOf(Entity entity)
        {
            return _team.Has(entity) ? _team.Get(entity).Index : Team.FreeForAll;
        }
    }
}

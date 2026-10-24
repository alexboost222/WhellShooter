using Engine.DI;
using Features.Bots.Components;
using Features.Bots.Configs;
using Features.Bots.Utils;
using Features.CharacterFeature.Components;
using Features.CharacterFeature.Configs;
using Features.Combat.Components;
using Features.Combat.Utils;
using Features.Navigation.Systems;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bots.Systems
{
    /// <summary>
    /// What each bot knows: picks the enemy it can see (field of view, view distance, line of sight
    /// over the arena grid), keeps tracking it, remembers it for a while once lost, and turns
    /// towards whoever just shot it. Writes <see cref="BotSenses"/> and nothing else.
    /// </summary>
    /// <remarks>
    /// Sight is scanned every <see cref="BotsConfig.ScanInterval"/> (staggered per bot); timers and a
    /// visible target's position update every tick, so the aim never lags a scan behind.
    /// </remarks>
    public sealed class BotSensesSystem : ISystem
    {
        [Inject] private Stash<Bot> _bot;
        [Inject] private Stash<BotSenses> _botSenses;
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<Look> _look;
        [Inject] private Stash<Team> _team;
        [Inject] private Stash<Dead> _dead;
        [Inject] private Stash<DamageTaken> _damageTaken;

        [Inject] private NavStorage _nav;
        [Inject] private BotsConfig _config;
        [Inject] private CharacterConfig _characterConfig;

        public World World { get; set; }

        private Filter _filter;
        private Filter _bodies;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Bot>()
                .With<BotSenses>()
                .With<CharacterMotion>()
                .With<Look>()
                .Without<Dead>()
                .Build();

            _bodies = World.Filter
                .With<Character>()
                .With<CharacterMotion>()
                .With<Health>()
                .Without<Dead>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                ref var senses = ref _botSenses.Get(entity);
                var profile = _config.GetProfile(_bot.Get(entity).ProfileId);

                Track(entity, ref senses, deltaTime);
                ReactToHits(entity, ref senses);

                senses.NextScan -= deltaTime;
                if (senses.NextScan > 0f)
                {
                    continue;
                }

                senses.NextScan = math.max(senses.NextScan + _config.ScanInterval, 0f);
                Scan(entity, ref senses, profile);
            }
        }

        /// <summary>
        /// Timers, and the live position of a target still in sight; drops a target that died. Sight
        /// of the tracked target is re-checked every tick — between scans a bot must not keep
        /// following someone who has stepped behind a wall.
        /// </summary>
        private void Track(Entity entity, ref BotSenses senses, float deltaTime)
        {
            if (!senses.HasTarget)
            {
                return;
            }

            if (!TryGetLiving(senses.Target, out var target))
            {
                senses.HasTarget = false;
                senses.Visible = false;
                return;
            }

            if (senses.Visible && !CanSee(entity, target))
            {
                senses.Visible = false;
            }

            if (senses.Visible)
            {
                senses.SeenFor += deltaTime;
                senses.SinceSeen = 0f;
                senses.TargetPosition = _characterMotion.Get(target).Position;
            }
            else
            {
                senses.SinceSeen += deltaTime;
            }
        }

        /// <summary>A new hit from an enemy we don't have in sight: we know roughly where it came from.</summary>
        private void ReactToHits(Entity entity, ref BotSenses senses)
        {
            if (!_damageTaken.Has(entity))
            {
                return;
            }

            var taken = _damageTaken.Get(entity);
            if (taken.Hits == senses.HitsSeen)
            {
                return;
            }

            senses.HitsSeen = taken.Hits;
            if (senses.HasTarget && senses.Visible)
            {
                return;
            }

            if (!TryGetLiving(taken.LastSource, out var source) || !IsHostile(entity, source))
            {
                return;
            }

            senses.Target = taken.LastSource;
            senses.HasTarget = true;
            senses.Visible = false;
            senses.SeenFor = 0f;
            senses.SinceSeen = 0f;
            senses.TargetPosition = _characterMotion.Get(source).Position;
            // Look now: the next scan confirms whether the shooter is actually in sight.
            senses.NextScan = 0f;
        }

        private void Scan(Entity entity, ref BotSenses senses, BotProfile profile)
        {
            var position = _characterMotion.Get(entity).Position;
            var eye = position + new float3(0f, _characterConfig.EyeHeight, 0f);
            var yaw = _look.Get(entity).Yaw;
            var halfFov = math.radians(profile.FieldOfView) * 0.5f;

            Entity best = null;
            var bestScore = float.MaxValue;

            foreach (var body in _bodies)
            {
                if (body.ID == entity.ID || !IsHostile(entity, body))
                {
                    continue;
                }

                var other = _characterMotion.Get(body).Position;
                var offset = other - position;
                var distance = math.length(offset);
                if (distance > profile.ViewDistance)
                {
                    continue;
                }

                var inFront = math.abs(BotMath.DeltaAngle(yaw, BotMath.YawOf(offset))) <= halfFov;
                if (!inFront && distance > profile.AwarenessRadius)
                {
                    continue;
                }

                if (!_nav.HasLineOfSight(eye, other + new float3(0f, _characterConfig.EyeHeight, 0f)))
                {
                    continue;
                }

                // Nearest wins, but the one already being fought gets a head start: no target hopping.
                var isCurrent = senses.HasTarget && body.ID == senses.Target;
                var score = isCurrent ? distance * 0.7f : distance;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = body;
                }
            }

            if (best == null)
            {
                senses.Visible = false;
                if (senses.HasTarget && senses.SinceSeen > profile.Memory)
                {
                    senses.HasTarget = false;
                }

                return;
            }

            // Reaction time restarts whenever a target comes (back) into view.
            if (!senses.Visible || !senses.HasTarget || best.ID != senses.Target)
            {
                senses.SeenFor = 0f;
            }

            senses.Target = best.ID;
            senses.HasTarget = true;
            senses.Visible = true;
            senses.SinceSeen = 0f;
            senses.TargetPosition = _characterMotion.Get(best).Position;
        }

        private bool CanSee(Entity entity, Entity target)
        {
            var eye = new float3(0f, _characterConfig.EyeHeight, 0f);
            return _nav.HasLineOfSight(_characterMotion.Get(entity).Position + eye, _characterMotion.Get(target).Position + eye);
        }

        private bool TryGetLiving(EntityId id, out Entity entity)
        {
            return World.TryGetEntity(id, out entity) && !_dead.Has(entity) && _characterMotion.Has(entity);
        }

        private bool IsHostile(Entity a, Entity b)
        {
            var teamA = _team.Has(a) ? _team.Get(a).Index : Team.FreeForAll;
            var teamB = _team.Has(b) ? _team.Get(b).Index : Team.FreeForAll;
            return CombatRules.AreHostile(teamA, teamB);
        }
    }
}

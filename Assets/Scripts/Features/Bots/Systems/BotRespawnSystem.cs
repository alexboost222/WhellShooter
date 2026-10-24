using Engine.DI;
using Features.Bots.Components;
using Features.Bots.Configs;
using Features.Bots.Enums;
using Features.CharacterFeature.Components;
using Features.Combat.Components;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bots.Systems
{
    /// <summary>
    /// Brings dead bots back after <see cref="BotsConfig.RespawnDelay"/>: a teleport to a safe spawn
    /// point and a <see cref="ReviveRequest"/> (Combat refills the health), and a clean slate for the
    /// brain.
    /// </summary>
    public sealed class BotRespawnSystem : ISystem
    {
        [Inject] private Stash<BotRespawn> _botRespawn;
        [Inject] private Stash<BotRandom> _botRandom;
        [Inject] private Stash<BotSenses> _botSenses;
        [Inject] private Stash<BotBrain> _botBrain;
        [Inject] private Stash<BotPath> _botPath;
        [Inject] private Stash<BotAim> _botAim;
        [Inject] private Stash<Team> _team;
        [Inject] private Stash<DamageTaken> _damageTaken;
        [Inject] private Stash<ReviveRequest> _reviveRequest;
        [Inject] private Stash<CharacterTeleportRequest> _characterTeleportRequest;

        [Inject] private BotActionSet _actions;
        [Inject] private BotSpawnSelector _selector;
        [Inject] private BotsConfig _config;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Bot>()
                .With<Dead>()
                .With<BotRandom>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                if (!_botRespawn.Has(entity))
                {
                    _botRespawn.Set(entity, new BotRespawn { Remaining = _config.RespawnDelay });
                    continue;
                }

                ref var respawn = ref _botRespawn.Get(entity);
                respawn.Remaining -= deltaTime;
                if (respawn.Remaining > 0f)
                {
                    continue;
                }

                ref var random = ref _botRandom.Get(entity).Value;
                var team = _team.Has(entity) ? _team.Get(entity).Index : Team.FreeForAll;
                if (!_selector.TryPick(team, entity.ID, ref random, out var point))
                {
                    continue;
                }

                _botRespawn.Remove(entity);
                _characterTeleportRequest.Set(entity, new CharacterTeleportRequest
                {
                    Position = point.Position,
                    Yaw = math.radians(point.Yaw),
                });
                _reviveRequest.Set(entity);
                Reset(entity);
            }
        }

        private void Reset(Entity entity)
        {
            ref var brain = ref _botBrain.Get(entity);
            _actions.Exit(entity, brain.Action);
            brain.Action = BotAction.None;
            brain.ActionTime = 0f;
            brain.NextThink = 0f;

            // The hits of the last life are not news in this one.
            var hits = _damageTaken.Has(entity) ? _damageTaken.Get(entity).Hits : 0u;
            _botSenses.Set(entity, new BotSenses { HitsSeen = hits });

            ref var path = ref _botPath.Get(entity);
            path.Valid = false;
            path.StuckCount = 0;
            _botAim.Set(entity, default);
        }
    }
}

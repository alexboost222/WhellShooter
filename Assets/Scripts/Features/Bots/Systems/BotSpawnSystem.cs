using Engine.DI;
using Engine.Ecs;
using Engine.Messages;
using Features.Bots.Components;
using Features.Bots.Configs;
using Features.Bots.Messages;
using Features.CharacterFeature.Components;
using Features.Combat.Components;
using Features.Map.Systems;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bots.Systems
{
    /// <summary>
    /// Turns <see cref="SpawnBots"/> into <see cref="BotSpawnRequest"/>s and consumes them once the
    /// arena has spawn points: each bot is a full body (the same components as the player's) plus
    /// the AI's own, placed by a teleport so the body feature does the moving.
    /// </summary>
    public sealed class BotSpawnSystem : ISystem
    {
        [Inject] private Stash<BotSpawnRequest> _botSpawnRequest;
        [Inject] private Stash<Bot> _bot;
        [Inject] private Stash<BotRandom> _botRandom;
        [Inject] private Stash<BotSenses> _botSenses;
        [Inject] private Stash<BotBrain> _botBrain;
        [Inject] private Stash<BotMove> _botMove;
        [Inject] private Stash<BotPath> _botPath;
        [Inject] private Stash<BotAim> _botAim;
        [Inject] private Stash<Character> _character;
        [Inject] private Stash<CharacterIntent> _characterIntent;
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<CharacterTeleportRequest> _characterTeleportRequest;
        [Inject] private Stash<Look> _look;
        [Inject] private Stash<Team> _team;
        [Inject] private Stash<Destroy> _destroy;

        [Inject] private SubscriberQueue<SpawnBots> _spawnBots;
        [Inject] private BotSpawnSelector _selector;
        [Inject] private BotsConfig _config;
        [Inject] private MapStorage _mapStorage;

        public World World { get; set; }

        private Filter _filter;
        private int _spawned;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<BotSpawnRequest>()
                .Without<Destroy>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var message in _spawnBots)
            {
                _botSpawnRequest.Set(World.CreateEntity(), new BotSpawnRequest
                {
                    Count = message.Count,
                    Teams = message.Teams,
                    ProfileId = message.ProfileId,
                });
            }

            if (_filter.FirstOrDefault() == null || !_selector.HasSpawnPoints)
            {
                return;
            }

            foreach (var entity in _filter)
            {
                var request = _botSpawnRequest.Get(entity);
                _botSpawnRequest.Remove(entity);
                _destroy.Set(entity);

                var profile = request.ProfileId != 0 ? request.ProfileId : _config.DefaultProfileId;
                for (var i = 0; i < request.Count; i++)
                {
                    Spawn(request.Teams ? _spawned % 2 : Team.FreeForAll, profile);
                }
            }
        }

        private void Spawn(int team, int profile)
        {
            var index = _spawned++;
            var seed = _mapStorage.Layout != null ? _mapStorage.Layout.Seed : 0;
            var hash = math.hash(new int3(seed, index, 0x0B07));
            var random = new Random(hash == 0 ? 1u : hash);

            var entity = World.CreateEntity();
            if (!_selector.TryPick(team, entity.ID, ref random, out var point))
            {
                _destroy.Set(entity);
                return;
            }

            var yaw = math.radians(point.Yaw);
            _character.Set(entity);
            _characterIntent.Set(entity, default);
            _characterMotion.Set(entity, new CharacterMotion { Position = point.Position, PreviousPosition = point.Position });
            _look.Set(entity, new Look { Yaw = yaw });
            _characterTeleportRequest.Set(entity, new CharacterTeleportRequest { Position = point.Position, Yaw = yaw });
            _team.Set(entity, new Team { Index = team });

            _bot.Set(entity, new Bot { ProfileId = profile });
            // Staggered, so a dozen bots don't all scan and think on the same tick.
            _botSenses.Set(entity, new BotSenses { NextScan = random.NextFloat(0f, _config.ScanInterval) });
            _botBrain.Set(entity, new BotBrain { NextThink = random.NextFloat(0f, _config.ThinkInterval) });
            _botMove.Set(entity, default);
            _botPath.Set(entity, new BotPath { Corners = new float3[16] });
            _botAim.Set(entity, default);
            _botRandom.Set(entity, new BotRandom { Value = random });
        }
    }
}

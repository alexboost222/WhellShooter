using Engine.Diagnostics.Logs;
using Engine.DI;
using Engine.Ecs;
using Engine.Messages;
using Features.Map.Components;
using Features.Map.Configs;
using Features.Map.Constants;
using Features.Map.Messages;
using Features.Map.Utils;
using Features.Network.Systems;
using Scellecs.Morpeh;

namespace Features.Map.Systems
{
    /// <summary>
    /// Consumes <see cref="GenerateMap"/> messages and <see cref="MapGenerateRequest"/>s and replaces
    /// the arena: new layout into <see cref="MapStorage"/>, new
    /// <see cref="SpawnPoint"/> entities, old ones tagged <see cref="Destroy"/>.
    /// </summary>
    public sealed class MapGenerateSystem : ISystem
    {
        [Inject] private Stash<MapGenerateRequest> _mapGenerateRequest;
        [Inject] private Stash<SpawnPoint> _spawnPoint;
        [Inject] private Stash<Destroy> _destroy;

        [Inject] private SubscriberQueue<GenerateMap> _generateMap;
        [Inject] private MapConfig _config;
        [Inject] private MapStorage _storage;
        [Inject] private NetRole _role;

        public World World { get; set; }

        private Filter _filter;
        private Filter _spawnPoints;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<MapGenerateRequest>()
                .Build();

            _spawnPoints = World.Filter
                .With<SpawnPoint>()
                .Without<Destroy>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            // Both ways in are read this tick. A message is not turned into a request entity:
            // the filter would only see it after the next commit, a tick late.
            var requested = false;
            var seed = 0;

            foreach (var entity in _filter)
            {
                seed = _mapGenerateRequest.Get(entity).Seed;
                _mapGenerateRequest.Remove(entity);
                _destroy.Set(entity);
                requested = true;
            }

            // A client builds the server's arena: its seed comes from the snapshot as a request.
            foreach (var message in _generateMap)
            {
                if (_role.IsClient)
                {
                    continue;
                }

                seed = message.Seed;
                requested = true;
            }

            // Several asks in one tick: the last one wins, the rest are consumed unanswered.
            if (requested)
            {
                Generate(seed);
            }
        }

        private void Generate(int seed)
        {
            var layout = MapGenerator.Generate(seed, _config.Settings);

            foreach (var entity in _spawnPoints)
            {
                _destroy.Set(entity);
            }

            foreach (var spawn in layout.Spawns)
            {
                _spawnPoint.Set(World.CreateEntity(), new SpawnPoint
                {
                    Position = spawn.Position,
                    Yaw = spawn.Yaw,
                    Team = spawn.Team,
                });
            }

            _storage.Set(layout);
            Log.Info(MapLog.Channel,
                $"Map generated: seed {seed}, {layout.Buildings.Count} buildings, {layout.Pieces.Count} pieces, " +
                $"{layout.Spawns.Count} spawns.");
        }
    }
}

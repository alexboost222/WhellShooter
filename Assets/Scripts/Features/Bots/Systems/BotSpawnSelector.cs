using Engine.DI;
using Engine.Ecs;
using Features.CharacterFeature.Components;
using Features.Combat.Components;
using Features.Combat.Utils;
using Features.Map.Components;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bots.Systems
{
    /// <summary>
    /// Picks where a bot (re)appears: among the arena's spawn points for its side, the one farthest
    /// from every living enemy — nobody spawns in someone's sights.
    /// </summary>
    public sealed class BotSpawnSelector : IInitializer
    {
        [Inject] private Stash<SpawnPoint> _spawnPoint;
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<Team> _team;

        public World World { get; set; }

        private Filter _spawnPoints;
        private Filter _bodies;

        public void OnAwake()
        {
            _spawnPoints = World.Filter
                .With<SpawnPoint>()
                .Without<Destroy>()
                .Build();

            _bodies = World.Filter
                .With<Character>()
                .With<CharacterMotion>()
                .Without<Dead>()
                .Build();
        }

        public void Dispose()
        {
        }

        /// <summary>Whether the arena has any spawn point yet.</summary>
        public bool HasSpawnPoints => _spawnPoints.FirstOrDefault() != null;

        /// <summary>
        /// The best spawn point for <paramref name="team"/>: its own team's points in a team game,
        /// the free-for-all points otherwise; any point when there is none of the right kind.
        /// </summary>
        public bool TryPick(int team, EntityId self, ref Random random, out SpawnPoint picked)
        {
            picked = default;
            var bestScore = float.MinValue;
            var found = false;

            for (var pass = 0; pass < 2 && !found; pass++)
            {
                foreach (var entity in _spawnPoints)
                {
                    var point = _spawnPoint.Get(entity);
                    if (pass == 0 && point.Team != team)
                    {
                        continue;
                    }

                    // A little noise, so equally safe points don't always resolve the same way.
                    var score = NearestEnemy(point.Position, team, self) + random.NextFloat(0f, 0.5f);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        picked = point;
                        found = true;
                    }
                }
            }

            return found;
        }

        private float NearestEnemy(float3 position, int team, EntityId self)
        {
            var nearest = 1000f;
            foreach (var body in _bodies)
            {
                if (body.ID == self)
                {
                    continue;
                }

                var other = _team.Has(body) ? _team.Get(body).Index : Team.FreeForAll;
                if (!CombatRules.AreHostile(team, other))
                {
                    continue;
                }

                nearest = math.min(nearest, math.distance(position, _characterMotion.Get(body).Position));
            }

            return nearest;
        }
    }
}

using System.Collections.Generic;
using Engine.DI;
using Features.Bots.Components;
using Features.Bots.Configs;
using Features.CharacterFeature.Components;
using Features.CharacterFeature.Configs;
using Features.Combat.Components;
using Features.Navigation.Systems;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bots.Systems
{
    /// <summary>
    /// Keeps each moving bot's <see cref="BotPath"/> current: computes it when the destination moves,
    /// when the arena changed, when the follower flagged it stuck, and every
    /// <see cref="BotsConfig.RepathInterval"/> anyway. No path → <see cref="BotMove.Failed"/>.
    /// </summary>
    public sealed class BotPathSystem : ISystem
    {
        /// <summary>A destination that moved less than this keeps its path, metres.</summary>
        private const float DestinationSlack = 1f;

        [Inject] private Stash<BotMove> _botMove;
        [Inject] private Stash<BotPath> _botPath;
        [Inject] private Stash<CharacterMotion> _characterMotion;

        [Inject] private NavStorage _nav;
        [Inject] private BotsConfig _config;
        [Inject] private CharacterConfig _characterConfig;

        public World World { get; set; }

        private Filter _filter;
        private readonly List<float3> _corners = new List<float3>();

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Bot>()
                .With<BotMove>()
                .With<BotPath>()
                .With<CharacterMotion>()
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
                ref var move = ref _botMove.Get(entity);
                ref var path = ref _botPath.Get(entity);

                if (!move.Active)
                {
                    path.Valid = false;
                    continue;
                }

                path.Repath -= deltaTime;
                var stale = !path.Valid || path.NavVersion != _nav.Version || path.Repath <= 0f ||
                            math.distance(path.Destination, move.Destination) > DestinationSlack;
                if (!stale)
                {
                    continue;
                }

                if (math.distance(path.Destination, move.Destination) > DestinationSlack)
                {
                    move.Arrived = false;
                }

                path.Repath = _config.RepathInterval;
                var position = _characterMotion.Get(entity).Position;
                if (!_nav.TryFindPath(position, move.Destination, _characterConfig.Radius, _corners))
                {
                    path.Valid = false;
                    move.Failed = true;
                    continue;
                }

                Store(ref path, _corners);
                path.Destination = move.Destination;
                path.NavVersion = _nav.Version;
                move.Failed = false;
            }
        }

        private static void Store(ref BotPath path, List<float3> corners)
        {
            if (path.Corners == null || path.Corners.Length < corners.Count)
            {
                path.Corners = new float3[math.max(corners.Count, 16)];
            }

            for (var i = 0; i < corners.Count; i++)
            {
                path.Corners[i] = corners[i];
            }

            path.Count = corners.Count;
            path.Next = corners.Count > 1 ? 1 : 0;
            path.Valid = true;
        }
    }
}

using Engine.DI;
using Features.Bots.Components;
using Features.Bots.Configs;
using Features.Bots.Utils;
using Features.CharacterFeature.Components;
using Features.Combat.Components;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bots.Systems
{
    /// <summary>
    /// Walks the path: turns the next corner into the body's local move input, slows into the last
    /// corner, reports <see cref="BotMove.Arrived"/>, and watches progress — a bot that stops getting
    /// anywhere forces a re-path, and after a few of those gives the move up
    /// (<see cref="BotMove.Failed"/>).
    /// </summary>
    /// <remarks>
    /// The move is local to the body's <c>Look</c> yaw, exactly as the player's stick input is, so a
    /// bot looking at its enemy while walking sideways is just the same input as a strafing player.
    /// </remarks>
    public sealed class BotSteerSystem : ISystem
    {
        /// <summary>Within this of an intermediate corner, head for the next one, metres.</summary>
        private const float CornerRadius = 0.6f;

        /// <summary>Start easing off this far from the final corner, metres.</summary>
        private const float SlowRadius = 1.5f;

        [Inject] private Stash<BotMove> _botMove;
        [Inject] private Stash<BotPath> _botPath;
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<CharacterIntent> _characterIntent;
        [Inject] private Stash<Look> _look;

        [Inject] private BotsConfig _config;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Bot>()
                .With<BotMove>()
                .With<BotPath>()
                .With<CharacterMotion>()
                .With<CharacterIntent>()
                .With<Look>()
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
                ref var intent = ref _characterIntent.Get(entity);
                var position = _characterMotion.Get(entity).Position;

                intent.Move = float2.zero;
                intent.Sprint = false;

                if (!move.Active || !path.Valid || path.Count == 0)
                {
                    path.ProgressTimer = 0f;
                    path.ProgressPosition = position;
                    continue;
                }

                while (path.Next < path.Count - 1 &&
                       math.distance(position.xz, path.Corners[path.Next].xz) < CornerRadius)
                {
                    path.Next++;
                }

                var corner = path.Corners[path.Next];
                var offset = corner - position;
                offset.y = 0f;
                var distance = math.length(offset);
                var last = path.Next == path.Count - 1;

                if (last && distance <= _config.ArriveDistance)
                {
                    move.Arrived = true;
                    path.ProgressTimer = 0f;
                    continue;
                }

                var speed = last ? math.clamp(distance / SlowRadius, 0.3f, 1f) : 1f;
                intent.Move = BotMath.ToLocalMove(offset / math.max(distance, 1e-4f) * speed, _look.Get(entity).Yaw);
                // Sprint only works running forward; sideways it would just be refused.
                intent.Sprint = move.Sprint && intent.Move.y > 0.7f;

                Watch(ref move, ref path, position, deltaTime);
            }
        }

        private void Watch(ref BotMove move, ref BotPath path, float3 position, float deltaTime)
        {
            path.ProgressTimer += deltaTime;
            if (path.ProgressTimer < _config.StuckCheckInterval)
            {
                return;
            }

            var moved = math.distance(position.xz, path.ProgressPosition.xz);
            path.ProgressTimer = 0f;
            path.ProgressPosition = position;
            if (moved >= _config.StuckDistance)
            {
                path.StuckCount = 0;
                return;
            }

            path.StuckCount++;
            path.Valid = false;
            if (path.StuckCount >= _config.StuckLimit)
            {
                path.StuckCount = 0;
                move.Failed = true;
            }
        }
    }
}

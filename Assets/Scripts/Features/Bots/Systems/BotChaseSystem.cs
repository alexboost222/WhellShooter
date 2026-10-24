using Engine.DI;
using Features.Bots.Components;
using Features.CharacterFeature.Components;
using Features.CharacterFeature.Configs;
using Features.Combat.Components;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bots.Systems
{
    /// <summary>
    /// Runs <see cref="BotChase"/>: sprint to where the enemy was last seen, looking that way, and
    /// give up on reaching it — the chase then scores zero and the brain moves on.
    /// </summary>
    public sealed class BotChaseSystem : ISystem
    {
        /// <summary>Closer than this to the spot, stop staring at it and look where you go, metres.</summary>
        private const float LookAheadDistance = 3f;

        [Inject] private Stash<BotChase> _botChase;
        [Inject] private Stash<BotSenses> _botSenses;
        [Inject] private Stash<BotMove> _botMove;
        [Inject] private Stash<BotAim> _botAim;
        [Inject] private Stash<CharacterMotion> _characterMotion;

        [Inject] private CharacterConfig _characterConfig;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<BotChase>()
                .With<BotSenses>()
                .With<BotMove>()
                .With<BotAim>()
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
                ref var chase = ref _botChase.Get(entity);
                ref var move = ref _botMove.Get(entity);
                ref var aim = ref _botAim.Get(entity);
                var senses = _botSenses.Get(entity);

                if (chase.Exhausted)
                {
                    move.Active = false;
                    aim.Active = false;
                    continue;
                }

                if (move.Active && (move.Arrived || move.Failed))
                {
                    chase.Exhausted = true;
                    move.Active = false;
                    aim.Active = false;
                    continue;
                }

                move.Destination = senses.TargetPosition;
                move.Active = true;
                move.Sprint = true;

                var position = _characterMotion.Get(entity).Position;
                aim.Active = math.distance(position, senses.TargetPosition) > LookAheadDistance;
                aim.Point = senses.TargetPosition + new float3(0f, _characterConfig.EyeHeight, 0f);
            }
        }
    }
}

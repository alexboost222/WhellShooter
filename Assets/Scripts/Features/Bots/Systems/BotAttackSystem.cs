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
    /// Runs <see cref="BotAttack"/>: aim at the target's chest, pull the trigger once the reaction
    /// time has passed and the look is on the (wobbling) aim point, and never stand still — close
    /// in when too far, back off when too close, strafe side to side otherwise.
    /// </summary>
    /// <remarks>
    /// The bot fires through the same weapon as the player, via <see cref="CharacterIntent.Fire"/>;
    /// it misses because its <c>Look</c> carries the profile's aim error, not because a dice roll
    /// says so.
    /// </remarks>
    public sealed class BotAttackSystem : ISystem
    {
        /// <summary>Where on the body a bot aims, as a share of the capsule height.</summary>
        private const float ChestHeight = 0.7f;

        [Inject] private Stash<Bot> _bot;
        [Inject] private Stash<BotAttack> _botAttack;
        [Inject] private Stash<BotSenses> _botSenses;
        [Inject] private Stash<BotMove> _botMove;
        [Inject] private Stash<BotAim> _botAim;
        [Inject] private Stash<BotRandom> _botRandom;
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<CharacterIntent> _characterIntent;

        [Inject] private NavStorage _nav;
        [Inject] private BotsConfig _config;
        [Inject] private CharacterConfig _characterConfig;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<BotAttack>()
                .With<BotSenses>()
                .With<BotMove>()
                .With<BotAim>()
                .With<BotRandom>()
                .With<CharacterMotion>()
                .With<CharacterIntent>()
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
                var profile = _config.GetProfile(_bot.Get(entity).ProfileId);
                var senses = _botSenses.Get(entity);
                ref var aim = ref _botAim.Get(entity);

                aim.Point = senses.TargetPosition + new float3(0f, _characterConfig.Height * ChestHeight, 0f);
                aim.Active = true;

                Manoeuvre(entity, senses, profile, deltaTime);

                ref var intent = ref _characterIntent.Get(entity);
                var fire = senses.Visible && senses.SeenFor >= profile.ReactionTime && aim.OnTarget;
                if (fire && !intent.Fire)
                {
                    intent.FirePressed = true;
                }

                intent.Fire = fire;
            }
        }

        private void Manoeuvre(Entity entity, BotSenses senses, BotProfile profile, float deltaTime)
        {
            ref var attack = ref _botAttack.Get(entity);
            ref var move = ref _botMove.Get(entity);

            attack.NextStrafe -= deltaTime;
            if (attack.NextStrafe > 0f && move.Active && !move.Arrived && !move.Failed)
            {
                return;
            }

            ref var random = ref _botRandom.Get(entity).Value;
            attack.NextStrafe = random.NextFloat(profile.StrafeInterval.x, profile.StrafeInterval.y);

            var position = _characterMotion.Get(entity).Position;
            var toTarget = senses.TargetPosition - position;
            toTarget.y = 0f;
            var distance = math.length(toTarget);
            var forward = distance > 1e-3f ? toTarget / distance : new float3(0f, 0f, 1f);

            if (distance > profile.PreferredRange * 1.4f)
            {
                move = new BotMove { Destination = senses.TargetPosition, Active = true };
                return;
            }

            // Side to side, alternating; drift back when the enemy is in our face.
            var side = new float3(forward.z, 0f, -forward.x);
            var back = distance < profile.PreferredRange * 0.5f ? -forward * 2f : float3.zero;
            var length = random.NextFloat(profile.StrafeDistance.x, profile.StrafeDistance.y);

            for (var attempt = 0; attempt < 2; attempt++)
            {
                attack.StrafeSide = -attack.StrafeSide;
                var candidate = position + side * (attack.StrafeSide * length) + back;
                var grid = _nav.Grid;
                if (grid != null && grid.IsWalkable(grid.CellOf(candidate)) && grid.CanWalkStraight(position, candidate, 0f))
                {
                    move = new BotMove { Destination = candidate, Active = true };
                    return;
                }
            }

            move.Active = false;
        }
    }
}

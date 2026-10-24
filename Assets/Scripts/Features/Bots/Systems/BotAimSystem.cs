using Engine.DI;
using Features.Bots.Components;
using Features.Bots.Configs;
using Features.Bots.Utils;
using Features.CharacterFeature.Components;
using Features.CharacterFeature.Configs;
using Features.Combat.Components;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Bots.Systems
{
    /// <summary>
    /// Turns the body's <see cref="Look"/> — the same component the player's mouse drives — towards
    /// what the bot wants to see: the aim point plus a human wobble while aiming, the next path
    /// corner while walking. Turning is rate-limited per profile.
    /// </summary>
    /// <remarks>
    /// <b>The wobble is the whole accuracy model.</b> It is large when a target is first acquired and
    /// settles the longer the bot tracks it (<see cref="BotProfile.AimSettleTime"/>); the weapon
    /// fires along the look, so a wobbly look is a missed shot.
    /// </remarks>
    public sealed class BotAimSystem : ISystem
    {
        private const float MaxPitch = 1.4f;

        [Inject] private Stash<Bot> _bot;
        [Inject] private Stash<BotAim> _botAim;
        [Inject] private Stash<BotSenses> _botSenses;
        [Inject] private Stash<BotMove> _botMove;
        [Inject] private Stash<BotPath> _botPath;
        [Inject] private Stash<BotRandom> _botRandom;
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<Look> _look;

        [Inject] private BotsConfig _config;
        [Inject] private CharacterConfig _characterConfig;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Bot>()
                .With<BotAim>()
                .With<BotSenses>()
                .With<BotRandom>()
                .With<CharacterMotion>()
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
                var profile = _config.GetProfile(_bot.Get(entity).ProfileId);
                ref var aim = ref _botAim.Get(entity);
                ref var look = ref _look.Get(entity);
                var position = _characterMotion.Get(entity).Position;

                float yaw;
                float pitch;
                if (aim.Active)
                {
                    var eye = position + new float3(0f, _characterConfig.EyeHeight, 0f);
                    var toPoint = aim.Point - eye;
                    Wobble(entity, ref aim, profile, deltaTime);
                    yaw = BotMath.YawOf(toPoint) + aim.Error.x;
                    pitch = BotMath.PitchOf(toPoint) + aim.Error.y;
                }
                else
                {
                    aim.Error = float2.zero;
                    yaw = WalkingYaw(entity, position, look.Yaw);
                    pitch = 0f;
                }

                pitch = math.clamp(pitch, -MaxPitch, MaxPitch);
                var maxSpeed = math.radians(profile.TurnSpeed);
                look.Yaw = BotMath.TurnTowards(look.Yaw, yaw, profile.TurnSharpness, maxSpeed, deltaTime);
                look.Pitch = math.clamp(
                    look.Pitch + math.clamp((pitch - look.Pitch) * (1f - math.exp(-profile.TurnSharpness * deltaTime)),
                        -maxSpeed * deltaTime, maxSpeed * deltaTime),
                    -MaxPitch, MaxPitch);

                aim.OnTarget = aim.Active &&
                               BotMath.AngleBetween(look.Yaw, look.Pitch, yaw, pitch) <= math.radians(profile.FireTolerance);
            }
        }

        /// <summary>
        /// Drifts the aim error towards a target error re-rolled a few times a second; the size of
        /// that target shrinks as the bot settles on its enemy.
        /// </summary>
        private void Wobble(Entity entity, ref BotAim aim, BotProfile profile, float deltaTime)
        {
            aim.NextErrorRoll -= deltaTime;
            if (aim.NextErrorRoll <= 0f)
            {
                ref var random = ref _botRandom.Get(entity).Value;
                aim.NextErrorRoll = random.NextFloat(0.25f, 0.6f);

                var settled = profile.AimSettleTime > 0f
                    ? math.saturate(_botSenses.Get(entity).SeenFor / profile.AimSettleTime)
                    : 1f;
                var size = math.radians(profile.AimError) * math.lerp(1f, profile.SettledAimError, settled);
                var angle = random.NextFloat(0f, 2f * math.PI);
                var radius = math.sqrt(random.NextFloat()) * size;
                aim.ErrorTarget = new float2(math.cos(angle), math.sin(angle)) * radius;
            }

            aim.Error = math.lerp(aim.Error, aim.ErrorTarget, 1f - math.exp(-6f * deltaTime));
        }

        /// <summary>Facing the next path corner while moving; holding the current yaw otherwise.</summary>
        private float WalkingYaw(Entity entity, float3 position, float current)
        {
            var move = _botMove.Get(entity);
            var path = _botPath.Get(entity);
            if (!move.Active || !path.Valid || path.Count == 0)
            {
                return current;
            }

            var offset = path.Corners[path.Next] - position;
            return math.lengthsq(offset.xz) > 0.04f ? BotMath.YawOf(offset) : current;
        }
    }
}

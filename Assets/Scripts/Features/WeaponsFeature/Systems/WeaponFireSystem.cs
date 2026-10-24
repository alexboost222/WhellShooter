using Engine.DI;
using Features.CharacterFeature.Components;
using Features.CharacterFeature.Configs;
using Features.WeaponsFeature.Components;
using Features.WeaponsFeature.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.WeaponsFeature.Systems
{
    /// <summary>
    /// The trigger, on the fixed tick, for every body: aiming, fire rate, automatic vs single.
    /// A shot is a <see cref="Weapon.Shots"/> increment plus a <see cref="ShotRequest"/>.
    /// </summary>
    /// <remarks>
    /// Listed after the motor, so <c>CharacterMotion.Sprinting</c> is this tick's answer — the motor
    /// already drops a sprint when the trigger is held, so a sprinting body here is one that is not
    /// trying to shoot.
    /// <para>
    /// At most one shot per tick (one request slot): the ceiling is the tick rate, 3000 rpm at
    /// 50 Hz, well above anything authored.
    /// </para>
    /// </remarks>
    public sealed class WeaponFireSystem : ISystem
    {
        // 600 rpm is 0.1 s = five 0.02 s ticks, which float subtraction lands a hair above zero;
        // without slack the shot would slip a whole tick and the rate would read 500.
        private const float CooldownTolerance = 1e-4f;

        [Inject] private Stash<CharacterIntent> _characterIntent;
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<Look> _look;
        [Inject] private Stash<Weapon> _weapon;
        [Inject] private Stash<ShotRequest> _shotRequest;
        [Inject] private Stash<ShotRewind> _shotRewind;

        [Inject] private WeaponsConfig _config;
        [Inject] private CharacterConfig _characterConfig;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Character>()
                .With<CharacterIntent>()
                .With<CharacterMotion>()
                .With<Look>()
                .With<Weapon>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                ref var intent = ref _characterIntent.Get(entity);
                ref var motion = ref _characterMotion.Get(entity);
                ref var weapon = ref _weapon.Get(entity);

                var pressed = intent.FirePressed;
                intent.FirePressed = false;

                weapon.Aiming = intent.Aim && !motion.Sprinting;

                // Only a running cooldown counts down. Idle time is not credit: a weapon picked up
                // after a pause fires its second shot a full interval after its first.
                if (weapon.Cooldown > 0f)
                {
                    weapon.Cooldown -= deltaTime;
                }

                if (!_config.TryGet(weapon.ConfigId, out var definition))
                {
                    weapon.Cooldown = math.max(weapon.Cooldown, 0f);
                    continue;
                }

                var trigger = definition.Automatic ? intent.Fire || pressed : pressed;

                if (trigger && !motion.Sprinting && weapon.Cooldown <= CooldownTolerance)
                {
                    weapon.Shots++;
                    weapon.Cooldown += definition.ShotInterval;

                    var look = _look.Get(entity);
                    _shotRequest.Set(entity, new ShotRequest
                    {
                        Origin = motion.Position + new float3(0f, _characterConfig.EyeHeight, 0f),
                        Direction = look.Forward,
                        WeaponId = weapon.ConfigId,
                        RewindTicks = _shotRewind.Has(entity) ? _shotRewind.Get(entity).Ticks : 0,
                    });
                }

                // A fired shot carries the overshoot into the next interval, so a rate that is not a
                // whole number of ticks still averages out right; a released (or blocked) trigger
                // drops it, so the next press fires at once and never twice.
                weapon.Cooldown = math.max(weapon.Cooldown, 0f);
            }
        }
    }
}

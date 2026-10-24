using Engine.DI;
using Engine.Motion;
using Features.CharacterFeature.Components;
using Features.ViewmodelFeature.Components;
using Features.ViewmodelFeature.Configs;
using Features.ViewmodelFeature.Utils;
using Features.WeaponsFeature.Components;
using Features.WeaponsFeature.Configs;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.ViewmodelFeature.Systems
{
    /// <summary>
    /// Advances the viewmodel's blends and oscillators from the simulation's state: aim and sprint
    /// blends, how much bob is showing, and the bob and breathing phases.
    /// </summary>
    /// <remarks>
    /// <b>Bob advances with distance, not time</b> — π per footstep — so the weapon's rhythm is the
    /// feet's rhythm at any speed, and standing still freezes it rather than leaving it ticking.
    /// </remarks>
    public sealed class ViewmodelBlendSystem : ISystem
    {
        [Inject] private Stash<ViewmodelState> _viewmodelState;
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<Weapon> _weapon;

        [Inject] private ViewmodelConfig _config;
        [Inject] private WeaponsConfig _weapons;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<ViewmodelState>()
                .With<CharacterMotion>()
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
                ref var state = ref _viewmodelState.Get(entity);
                ref var motion = ref _characterMotion.Get(entity);
                ref var weapon = ref _weapon.Get(entity);

                var aimTime = _weapons.TryGet(weapon.ConfigId, out var definition)
                    ? definition.Viewmodel.AimTime
                    : 0f;

                state.AimBlend = Blend(state.AimBlend, weapon.Aiming, aimTime, deltaTime);
                state.SprintBlend = Blend(state.SprintBlend, motion.Sprinting, _config.SprintBlendTime, deltaTime);

                var speed = math.length(motion.Velocity.xz);
                var bobTarget = motion.Grounded && _config.BobFullSpeed > 0f
                    ? math.saturate(speed / _config.BobFullSpeed)
                    : 0f;
                state.BobWeight = Springs.Damp(state.BobWeight, bobTarget, _config.BobSharpness, deltaTime);

                var stepLength = math.lerp(_config.WalkStepLength, _config.SprintStepLength,
                    ViewmodelMath.Ease(state.SprintBlend));
                if (motion.Grounded && stepLength > 0f)
                {
                    state.BobPhase = ViewmodelMath.WrapPhase(state.BobPhase + speed / stepLength * math.PI * deltaTime);
                }

                state.BreathPhase = ViewmodelMath.WrapPhase(state.BreathPhase + 2f * math.PI * _config.BreathRate * deltaTime);
            }
        }

        /// <summary>Linear towards 0 or 1, taking <paramref name="time"/> seconds end to end.</summary>
        private static float Blend(float current, bool on, float time, float deltaTime)
        {
            var target = on ? 1f : 0f;
            if (time <= 0f)
            {
                return target;
            }

            return Springs.MoveTowards(current, target, deltaTime / time);
        }
    }
}

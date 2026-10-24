using Engine.DI;
using Features.CharacterFeature.Components;
using Features.CharacterFeature.Utils;
using Features.ViewmodelFeature.Components;
using Features.ViewmodelFeature.Configs;
using Features.ViewmodelFeature.Utils;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.ViewmodelFeature.Systems
{
    /// <summary>
    /// Sway and movement: sets where the motion spring should sit from the turn rate and the body's
    /// velocity, and kicks the springs on take-off and landing.
    /// </summary>
    /// <remarks>
    /// <b>Turn rate, not turn delta.</b> Dividing by the frame delta makes a 180°/s turn sway the same
    /// at 30 and at 120 fps; a per-frame delta would sway four times as hard on the slower device.
    /// The spring smooths the noise a rate carries.
    /// <para>
    /// Jumps and landings are the simulation's counters compared with what was last seen, so a
    /// landing that happened between two frames — or two of them — is never missed or doubled.
    /// </para>
    /// </remarks>
    public sealed class ViewmodelMotionSystem : ISystem
    {
        [Inject] private Stash<ViewmodelState> _viewmodelState;
        [Inject] private Stash<ViewmodelSprings> _viewmodelSprings;
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<Look> _look;

        [Inject] private ViewmodelConfig _config;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<ViewmodelState>()
                .With<ViewmodelSprings>()
                .With<CharacterMotion>()
                .With<Look>()
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
                ref var springs = ref _viewmodelSprings.Get(entity);
                ref var motion = ref _characterMotion.Get(entity);
                var look = _look.Get(entity);

                var lookRate = float2.zero;
                if (state.Primed && deltaTime > 0f)
                {
                    lookRate = math.degrees(new float2(
                        LookMath.DeltaAngle(state.LastYaw, look.Yaw),
                        look.Pitch - state.LastPitch)) / deltaTime;
                }

                state.LastYaw = look.Yaw;
                state.LastPitch = look.Pitch;
                state.Primed = true;

                var localVelocity = math.mul(math.inverse(look.Heading), motion.Velocity);
                ViewmodelMath.MotionTargets(lookRate, localVelocity, _config,
                    out springs.MotionTargetPosition, out springs.MotionTargetRotation);

                if (motion.Jumps != state.SeenJumps)
                {
                    state.SeenJumps = motion.Jumps;
                    Kick(ref springs, new float3(0f, -_config.JumpPosition, 0f),
                        new float3(-_config.JumpRotation, 0f, 0f), float3.zero);
                }

                if (motion.Landings != state.SeenLandings)
                {
                    state.SeenLandings = motion.Landings;

                    var strength = _config.LandReferenceSpeed > 0f
                        ? math.saturate(motion.LandingSpeed / _config.LandReferenceSpeed)
                        : 0f;
                    Kick(ref springs, new float3(0f, -_config.LandPosition, 0f) * strength,
                        new float3(_config.LandRotation, 0f, 0f) * strength,
                        new float3(_config.LandCamera, 0f, 0f) * strength);
                }
            }
        }

        /// <param name="rotationDeg">Weapon, degrees.</param>
        /// <param name="cameraDeg">Camera, degrees.</param>
        private void Kick(ref ViewmodelSprings springs, float3 position, float3 rotationDeg, float3 cameraDeg)
        {
            springs.MotionPosition.Impulse(ViewmodelMath.KickVelocity(position, _config.MotionFrequency));
            springs.MotionRotation.Impulse(ViewmodelMath.KickVelocity(math.radians(rotationDeg), _config.MotionFrequency));
            springs.CameraRotation.Impulse(ViewmodelMath.KickVelocity(math.radians(cameraDeg), _config.CameraFrequency));
        }
    }
}

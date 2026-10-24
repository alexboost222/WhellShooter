using Engine.DI;
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
    /// Composes this frame's <see cref="ViewmodelPose"/> from every layer:
    /// <code>
    /// base     hip ⟶ aim ⟶ sprint pose (eased blends)
    /// motion   (sway + lag + landing spring + bob) × aim steadiness
    /// breath   × (1 − bob) × aim steadiness
    /// recoil   spring, × nothing — the kick was already scaled when it was added
    /// camera   punch spring; field of view hip ⟶ aim, + a little at a sprint
    /// </code>
    /// </summary>
    public sealed class ViewmodelPoseSystem : ISystem
    {
        [Inject] private Stash<ViewmodelState> _viewmodelState;
        [Inject] private Stash<ViewmodelSprings> _viewmodelSprings;
        [Inject] private Stash<ViewmodelPose> _viewmodelPose;
        [Inject] private Stash<Weapon> _weapon;

        [Inject] private ViewmodelConfig _config;
        [Inject] private WeaponsConfig _weapons;

        public World World { get; set; }

        private readonly WeaponViewmodel _fallback = new WeaponViewmodel();

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<ViewmodelState>()
                .With<ViewmodelSprings>()
                .With<ViewmodelPose>()
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
                ref var springs = ref _viewmodelSprings.Get(entity);
                ref var pose = ref _viewmodelPose.Get(entity);

                var weapon = _weapons.TryGet(_weapon.Get(entity).ConfigId, out var definition)
                    ? definition.Viewmodel
                    : _fallback;

                var aim = ViewmodelMath.Ease(state.AimBlend);
                var sprint = ViewmodelMath.Ease(state.SprintBlend);

                var basePosition = math.lerp(math.lerp((float3)weapon.HipPosition, weapon.AimPosition, aim),
                    weapon.SprintPosition, sprint);
                var baseRotation = math.slerp(
                    math.slerp(Euler(weapon.HipRotation), Euler(weapon.AimRotation), aim),
                    Euler(weapon.SprintRotation), sprint);

                var steadiness = math.lerp(1f, _config.AimMotionScale, aim);
                var bobAmplitude = state.BobWeight * math.lerp(1f, _config.SprintBobScale, sprint);
                ViewmodelMath.Bob(state.BobPhase, bobAmplitude, _config, out var bobPosition, out var bobRotation);

                var breathAmplitude = (1f - state.BobWeight) * math.lerp(1f, _config.AimBreathScale, aim);
                ViewmodelMath.Breath(state.BreathPhase, breathAmplitude, _config,
                    out var breathPosition, out var breathRotation);

                var offsetPosition = steadiness * (springs.MotionPosition.Value + bobPosition)
                    + breathPosition
                    + springs.RecoilPosition.Value;
                var offsetRotation = steadiness * (springs.MotionRotation.Value + bobRotation)
                    + breathRotation
                    + springs.RecoilRotation.Value;

                pose.WeaponPosition = basePosition + offsetPosition;
                pose.WeaponRotation = math.mul(baseRotation, quaternion.EulerZXY(offsetRotation));
                pose.CameraRotation = quaternion.EulerZXY(springs.CameraRotation.Value);
                pose.FieldOfView = math.lerp(_config.HipFieldOfView, weapon.AimFieldOfView, aim)
                    + _config.SprintFieldOfView * sprint;
            }
        }

        private static quaternion Euler(UnityEngine.Vector3 degrees)
        {
            return quaternion.EulerZXY(math.radians((float3)degrees));
        }
    }
}

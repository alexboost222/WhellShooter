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
    /// Recoil: every shot fired since the last frame kicks the weapon and punches the camera.
    /// </summary>
    /// <remarks>
    /// Visual only — the aim does not move. Each shot's random part comes from its own index
    /// (<see cref="ViewmodelMath.RecoilKick"/>), so two shots in one frame kick twice, differently,
    /// and replaying the same shots replays the same kicks.
    /// </remarks>
    public sealed class ViewmodelRecoilSystem : ISystem
    {
        [Inject] private Stash<ViewmodelState> _viewmodelState;
        [Inject] private Stash<ViewmodelSprings> _viewmodelSprings;
        [Inject] private Stash<Weapon> _weapon;

        [Inject] private ViewmodelConfig _config;
        [Inject] private WeaponsConfig _weapons;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<ViewmodelState>()
                .With<ViewmodelSprings>()
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
                ref var weapon = ref _weapon.Get(entity);

                if (weapon.Shots == state.SeenShots)
                {
                    continue;
                }

                if (_weapons.TryGet(weapon.ConfigId, out var definition))
                {
                    var viewmodel = definition.Viewmodel;
                    var scale = math.lerp(1f, viewmodel.AimKickScale, ViewmodelMath.Ease(state.AimBlend));

                    // Unsigned subtraction: right across a wrap, too.
                    for (var shot = state.SeenShots + 1; shot != weapon.Shots + 1; shot++)
                    {
                        ViewmodelMath.RecoilKick(viewmodel, shot, scale, out var position, out var rotation, out var camera);

                        springs.RecoilPosition.Impulse(ViewmodelMath.KickVelocity(position, _config.RecoilFrequency));
                        springs.RecoilRotation.Impulse(ViewmodelMath.KickVelocity(rotation, _config.RecoilFrequency));
                        springs.CameraRotation.Impulse(ViewmodelMath.KickVelocity(camera, _config.CameraFrequency));
                    }
                }

                state.SeenShots = weapon.Shots;
            }
        }
    }
}

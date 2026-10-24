using Engine.DI;
using Features.CharacterFeature.Components;
using Features.WeaponsFeature.Components;
using Features.WeaponsFeature.Configs;
using Scellecs.Morpeh;

namespace Features.WeaponsFeature.Systems
{
    /// <summary>
    /// Hydration: a body without a weapon is handed the default one. Whoever spawns a body may give
    /// it a different weapon first; this only fills the gap.
    /// </summary>
    public sealed class WeaponEquipSystem : ISystem
    {
        [Inject] private Stash<Weapon> _weapon;

        [Inject] private WeaponsConfig _config;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Character>()
                .Without<Weapon>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                _weapon.Set(entity, new Weapon { ConfigId = _config.DefaultWeaponId });
            }
        }
    }
}

using Engine.DI;
using Engine.Diagnostics;
using Engine.Diagnostics.Logs;
using Features.CharacterFeature.Components;
using Features.PlayerFeature.Components;
using Features.ViewmodelFeature.Components;
using Features.ViewmodelFeature.Providers;
using Features.WeaponsFeature.Components;
using Features.WeaponsFeature.Configs;
using Scellecs.Morpeh;
using UnityEngine;

namespace Features.ViewmodelFeature.Systems
{
    /// <summary>
    /// Hydration: the player's body, once it has a view and a weapon, gets the weapon model built
    /// into its rig and a fresh viewmodel state.
    /// </summary>
    /// <remarks>
    /// The state starts with the simulation's counters as already seen, so the first frame does not
    /// replay every shot and landing that happened before the model existed.
    /// </remarks>
    public sealed class ViewmodelSpawnSystem : ISystem
    {
        [Inject] private Stash<CharacterViewLink> _characterViewLink;
        [Inject] private Stash<CharacterMotion> _characterMotion;
        [Inject] private Stash<Weapon> _weapon;
        [Inject] private Stash<ViewmodelLink> _viewmodelLink;
        [Inject] private Stash<ViewmodelState> _viewmodelState;
        [Inject] private Stash<ViewmodelSprings> _viewmodelSprings;
        [Inject] private Stash<ViewmodelPose> _viewmodelPose;

        [Inject] private WeaponsConfig _weapons;

        public World World { get; set; }

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Player>()
                .With<CharacterViewLink>()
                .With<CharacterMotion>()
                .With<Weapon>()
                .Without<ViewmodelLink>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                var view = _characterViewLink.Get(entity).View;
                var rig = view.GetComponentInChildren<ViewmodelRig>(true);
                Guard.ThrowIf(rig == null, $"{view.name} has no {nameof(ViewmodelRig)} — the player prefab needs one under its head.");

                var weapon = _weapon.Get(entity);
                var model = BuildModel(rig, weapon.ConfigId);
                var motion = _characterMotion.Get(entity);

                _viewmodelLink.Set(entity, new ViewmodelLink { Rig = rig, Model = model, WeaponId = weapon.ConfigId });
                _viewmodelState.Set(entity, new ViewmodelState
                {
                    SeenShots = weapon.Shots,
                    SeenJumps = motion.Jumps,
                    SeenLandings = motion.Landings,
                });
                _viewmodelSprings.Set(entity, new ViewmodelSprings());
                _viewmodelPose.Set(entity, new ViewmodelPose());
            }
        }

        private GameObject BuildModel(ViewmodelRig rig, int weaponId)
        {
            if (_weapons.TryGet(weaponId, out var definition) && definition.ViewPrefab != null)
            {
                var model = Object.Instantiate(definition.ViewPrefab, rig.Socket, false);
                model.name = definition.ViewPrefab.name;
                return model;
            }

            // An unknown weapon still gets a (bare) model, so the link — and everything that animates
            // through it — exists and the hole is visible rather than silent.
            Log.Warning(LogChannels.Render, $"Weapon {weaponId} has no first-person model in {nameof(WeaponsConfig)}.");
            var placeholder = new GameObject("NoWeaponModel");
            placeholder.transform.SetParent(rig.Socket, false);
            return placeholder;
        }
    }
}

using System.Collections.Generic;
using Engine.DI;
using Features.ViewmodelFeature.Components;
using Features.WeaponsFeature.Components;
using Scellecs.Morpeh;
using UnityEngine;

namespace Features.ViewmodelFeature.Systems
{
    /// <summary>
    /// Drops a <see cref="ViewmodelLink"/> that no longer describes what is there: its rig died (the
    /// body's view went with a scene), or the body now holds a different weapon. Runs before the
    /// spawn system, so a swap is rebuilt in the same frame.
    /// </summary>
    public sealed class ViewmodelSweepSystem : ISystem
    {
        [Inject] private Stash<ViewmodelLink> _viewmodelLink;
        [Inject] private Stash<Weapon> _weapon;

        public World World { get; set; }

        private readonly List<Entity> _stale = new List<Entity>();

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<ViewmodelLink>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            _stale.Clear();

            foreach (var entity in _filter)
            {
                ref var link = ref _viewmodelLink.Get(entity);
                var swapped = !_weapon.Has(entity) || _weapon.Get(entity).ConfigId != link.WeaponId;

                if (link.Rig == null || link.Model == null || swapped)
                {
                    if (link.Model != null)
                    {
                        Object.Destroy(link.Model);
                    }

                    _stale.Add(entity);
                }
            }

            for (var i = 0; i < _stale.Count; i++)
            {
                _viewmodelLink.Remove(_stale[i]);
            }
        }
    }
}

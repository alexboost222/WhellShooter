using System.Collections.Generic;
using Engine.DI;
using Features.CharacterFeature.Components;
using Scellecs.Morpeh;

namespace Features.CharacterFeature.Systems
{
    /// <summary>
    /// Drops a <see cref="CharacterViewLink"/> whose object was destroyed by something else — a scene
    /// unload, above all. Keeps "a link present means a live object" true for every system after it
    /// (Docs/03), and lets the driver's hydration build a fresh view.
    /// </summary>
    public sealed class CharacterViewSweepSystem : ISystem
    {
        [Inject] private Stash<CharacterViewLink> _characterViewLink;

        public World World { get; set; }

        private readonly List<Entity> _stale = new List<Entity>();

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<CharacterViewLink>()
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
                if (_characterViewLink.Get(entity).View == null)
                {
                    _stale.Add(entity);
                }
            }

            for (var i = 0; i < _stale.Count; i++)
            {
                _characterViewLink.Remove(_stale[i]);
            }
        }
    }
}

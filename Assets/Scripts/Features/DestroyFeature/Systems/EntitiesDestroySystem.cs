using System.Collections.Generic;
using Engine.Ecs;
using Scellecs.Morpeh;

namespace Features.DestroyFeature.Systems
{
    /// <summary>
    /// Disposes everything tagged <see cref="Destroy"/>. The one place in the game where an entity
    /// actually goes away, and it runs last in the tick (Docs/03).
    /// </summary>
    /// <remarks>
    /// <b>The whole point is that it is late and it is alone.</b> A system that disposed as it
    /// iterated would invalidate the filters of every system after it in the same tick; deferring
    /// to one known point means "destroyed" is observable data for the rest of the tick and a fact
    /// only afterwards.
    /// </remarks>
    public sealed class EntitiesDestroySystem : ISystem
    {
        public World World { get; set; }

        // Collected first, disposed second: Morpeh's filters are archetype views and disposal
        // moves entities between archetypes, which is not a thing to do to a live enumerator.
        private readonly List<Entity> _doomed = new List<Entity>();

        private Filter _filter;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<Destroy>()
                .Build();
        }

        public void Dispose()
        {
        }

        public void OnUpdate(float deltaTime)
        {
            _doomed.Clear();

            foreach (var entity in _filter)
            {
                _doomed.Add(entity);
            }

            for (var i = 0; i < _doomed.Count; i++)
            {
                World.RemoveEntity(_doomed[i]);
            }
        }
    }
}

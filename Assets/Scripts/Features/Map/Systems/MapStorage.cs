using Features.Map.Utils;
using Scellecs.Morpeh;

namespace Features.Map.Systems
{
    /// <summary>
    /// The current arena's layout. Written only by <see cref="MapGenerateSystem"/>; read by the view
    /// and, later, by anything that needs the grid (AI, pickups).
    /// </summary>
    /// <remarks>
    /// The layout is a grid plus a list of a thousand-odd pieces — bulk static data, not per-entity
    /// state — so it lives here rather than in components. What gameplay acts on per item
    /// (<c>SpawnPoint</c>) is in the world.
    /// </remarks>
    public sealed class MapStorage : IInitializer
    {
        public World World { get; set; }

        /// <summary>The current layout, or null before the first generation.</summary>
        public MapLayout Layout { get; private set; }

        /// <summary>Bumped on every generation, so views can tell a new arena from the one they built.</summary>
        public int Version { get; private set; }

        public void OnAwake()
        {
        }

        public void Dispose()
        {
            Layout = null;
        }

        internal void Set(MapLayout layout)
        {
            Layout = layout;
            Version++;
        }
    }
}

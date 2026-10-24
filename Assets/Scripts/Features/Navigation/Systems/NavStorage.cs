using System.Collections.Generic;
using Features.Navigation.Utils;
using Scellecs.Morpeh;
using Unity.Mathematics;

namespace Features.Navigation.Systems
{
    /// <summary>
    /// The current arena's <see cref="NavGrid"/> and a pathfinder over it. Rebuilt by
    /// <see cref="NavGridBuildSystem"/> whenever the map changes; read by anything that moves or
    /// looks (bots).
    /// </summary>
    /// <remarks>
    /// Queries only — it holds no per-agent state. A path belongs to whoever asked for it.
    /// </remarks>
    public sealed class NavStorage : IInitializer
    {
        private NavPathfinder _pathfinder;

        public World World { get; set; }

        /// <summary>The grid, or null before the first map.</summary>
        public NavGrid Grid { get; private set; }

        /// <summary>Bumped on every rebuild: a path computed under an older version is stale.</summary>
        public int Version { get; private set; }

        public void OnAwake()
        {
        }

        public void Dispose()
        {
            Grid = null;
            _pathfinder = null;
        }

        /// <summary>See <see cref="NavPathfinder.TryFindPath"/>. False when there is no grid yet.</summary>
        public bool TryFindPath(float3 from, float3 to, float radius, List<float3> corners)
        {
            if (_pathfinder == null)
            {
                corners.Clear();
                return false;
            }

            return _pathfinder.TryFindPath(from, to, radius, corners);
        }

        /// <summary>Ground-level line of sight; false when there is no grid yet.</summary>
        public bool HasLineOfSight(float3 from, float3 to)
        {
            return Grid != null && Grid.HasLineOfSight(from, to);
        }

        internal void Set(NavGrid grid)
        {
            Grid = grid;
            _pathfinder = grid != null ? new NavPathfinder(grid) : null;
            Version++;
        }
    }
}

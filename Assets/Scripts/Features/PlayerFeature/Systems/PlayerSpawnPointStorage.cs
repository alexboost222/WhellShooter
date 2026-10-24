using System.Collections.Generic;
using Engine.Diagnostics;
using Scellecs.Morpeh;
using UnityEngine;

namespace Features.PlayerFeature.Systems
{
    /// <summary>
    /// The scene's <c>PlayerSpawnPoint</c>s, in registration order. A lookup over hierarchy handles,
    /// which is what a storage is for (Docs/03).
    /// </summary>
    public sealed class PlayerSpawnPointStorage : IInitializer
    {
        private readonly List<Transform> _points = new List<Transform>();

        public World World { get; set; }

        public int Count => _points.Count;

        public void OnAwake()
        {
        }

        public void Dispose()
        {
            _points.Clear();
        }

        public void Register(Transform point)
        {
            Guard.ThrowIfDestroyed(point, nameof(point));

            if (!_points.Contains(point))
            {
                _points.Add(point);
            }
        }

        public void Unregister(Transform point)
        {
            _points.Remove(point);
        }

        /// <summary>The first live point, dropping any whose object died without unregistering.</summary>
        public bool TryGetFirst(out Transform point)
        {
            for (var i = _points.Count - 1; i >= 0; i--)
            {
                if (_points[i] == null)
                {
                    _points.RemoveAt(i);
                }
            }

            point = _points.Count > 0 ? _points[0] : null;
            return point != null;
        }
    }
}

using System.Collections.Generic;
using Engine.Diagnostics;
using Scellecs.Morpeh;
using Unity.Mathematics;
using UnityEngine;

namespace Features.PlayerFeature.Systems
{
    /// <summary>
    /// The scene's <c>PlayerSpawnPoint</c>s, in registration order. A lookup over hierarchy handles,
    /// which is what a storage is for (Docs/03).
    /// </summary>
    public sealed class PlayerSpawnPointStorage : IInitializer
    {
        private const int SpawnSlots = 4;
        private const float SpawnSpacing = 1.5f;

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

        /// <summary>
        /// Where player <paramref name="slot"/> (re)spawns: the first point, shifted sideways by the
        /// slot so players joining together don't share a capsule — slot 0 on the point, then right,
        /// left, further right.
        /// </summary>
        public bool TryGetSpawn(int slot, out float3 position, out float yaw)
        {
            if (!TryGetFirst(out var point))
            {
                position = default;
                yaw = 0f;
                return false;
            }

            float3 forward = point.forward;
            float3 right = point.right;
            var k = slot % SpawnSlots;
            var offset = (k + 1) / 2 * SpawnSpacing * (k % 2 == 1 ? 1f : -1f);
            position = (float3)point.position + right * offset;
            yaw = math.atan2(forward.x, forward.z);
            return true;
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

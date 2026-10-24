using Engine.DI;
using Features.Map.Configs;
using Features.Map.Utils;
using Scellecs.Morpeh;
using UnityEngine;

namespace Features.Map.Systems
{
    /// <summary>
    /// Draws the arena: whenever <see cref="MapStorage.Version"/> moves, the old arena's GameObjects
    /// go and the new layout is built from the <see cref="MapConfig"/> palette, then static-batched.
    /// </summary>
    /// <remarks>
    /// A view: it reads the storage and owns GameObjects, and nothing reads gameplay back from them.
    /// </remarks>
    public sealed class MapViewSystem : ISystem
    {
        [Inject] private MapConfig _config;
        [Inject] private MapStorage _storage;

        public World World { get; set; }

        private MapBuilder _builder;
        private GameObject _root;
        private int _builtVersion;

        public void OnAwake()
        {
            _builder = new MapBuilder(_config);
        }

        public void Dispose()
        {
            Clear();
        }

        public void OnUpdate(float deltaTime)
        {
            if (_storage.Version == _builtVersion)
            {
                return;
            }

            _builtVersion = _storage.Version;
            Clear();

            if (_storage.Layout == null)
            {
                return;
            }

            _root = _builder.Build(_storage.Layout, null);
            StaticBatchingUtility.Combine(_root);
        }

        private void Clear()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }
        }
    }
}

using Engine.DI;
using Engine.Diagnostics;
using Features.CharacterFeature.Providers;
using UnityEngine;

namespace Features.PlayerFeature.Systems
{
    /// <summary>
    /// Builds the player's view with its dependencies in place before <c>Awake</c>. The one thing in
    /// the feature that holds the container, so no system has to.
    /// </summary>
    public sealed class PlayerViewFactory
    {
        private readonly Container _container;

        public PlayerViewFactory(Container container)
        {
            Guard.ThrowIfNull(container, nameof(container));

            _container = container;
        }

        public CharacterView Build(CharacterView prefab, Vector3 position, Quaternion rotation)
        {
            Guard.ThrowIfDestroyed(prefab, nameof(prefab));

            var instance = _container.InstantiateInjected(prefab.gameObject, position, rotation);
            return instance.GetComponent<CharacterView>();
        }
    }
}

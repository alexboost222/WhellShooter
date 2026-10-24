using Engine.DI;
using Engine.Diagnostics;
using Features.Bots.Providers;
using Features.CharacterFeature.Providers;
using UnityEngine;

namespace Features.Bots.Systems
{
    /// <summary>
    /// Builds a bot's view: the prefab through <c>InstantiateInjected</c> (Docs/02), with the
    /// body's <see cref="CharacterView"/> and the <see cref="BotView"/> on its root.
    /// </summary>
    public sealed class BotViewFactory
    {
        private readonly Container _container;

        public BotViewFactory(Container container)
        {
            Guard.ThrowIfNull(container, nameof(container));

            _container = container;
        }

        public bool TryBuild(GameObject prefab, Vector3 position, Quaternion rotation, out CharacterView body,
            out BotView bot)
        {
            body = null;
            bot = null;
            if (prefab == null)
            {
                return false;
            }

            var instance = _container.InstantiateInjected(prefab, position, rotation);
            body = instance.GetComponent<CharacterView>();
            bot = instance.GetComponent<BotView>();
            if (body != null && bot != null)
            {
                return true;
            }

            Object.Destroy(instance);
            return false;
        }
    }
}

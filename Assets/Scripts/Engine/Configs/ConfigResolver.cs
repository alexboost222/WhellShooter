using System;
using Engine.DI;
using Engine.Diagnostics;

namespace Engine.Configs
{
    /// <summary>
    /// Resolves a config type to its asset, so a system writes
    /// <c>[Inject] WeaponsConfig _weapons;</c> and gets the ScriptableObject.
    /// </summary>
    /// <remarks>
    /// Lives beside the catalog rather than in Engine/DI: it names nothing game-specific, and
    /// keeping it here means Configs depends on DI instead of DI depending on Configs
    /// (Docs/01, mechanism vs manifest).
    /// </remarks>
    public sealed class ConfigResolver : IResolver
    {
        private readonly ConfigCatalog _catalog;

        public ConfigResolver(ConfigCatalog catalog)
        {
            Guard.ThrowIfNull(catalog, nameof(catalog));

            _catalog = catalog;
        }

        public bool TryResolve(Type type, out object value)
        {
            // Cheap gate first: most requests reaching this resolver are not configs at all.
            if (!typeof(Config).IsAssignableFrom(type))
            {
                value = null;
                return false;
            }

            if (_catalog.TryGet(type, out var config))
            {
                value = config;
                return true;
            }

            // Pass rather than throw: the container collects this with every other miss and
            // reports them together, which is the whole point of the boot validation pass.
            value = null;
            return false;
        }

        public override string ToString() => $"ConfigResolver ({_catalog.Count} configs)";
    }
}

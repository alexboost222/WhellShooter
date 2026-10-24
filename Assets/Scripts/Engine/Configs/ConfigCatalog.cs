using System;
using System.Collections.Generic;
using Engine.Diagnostics;
using Engine.Diagnostics.Logs;
using UnityEngine;

namespace Engine.Configs
{
    /// <summary>
    /// Every <see cref="Config"/> asset, indexed by its concrete type. Built once at boot from
    /// <c>Resources/Configs</c> and registered with the container, which is what lets a system
    /// write <c>[Inject] WeaponsConfig _weapons;</c> and get the asset.
    /// </summary>
    /// <remarks>
    /// Loading is separated from indexing on purpose: the constructor takes a plain list, so
    /// the whole thing is testable without any assets on disk.
    /// <para>
    /// Lookup is by <b>exact</b> concrete type. Asking for a base class does not find a
    /// subclass — with several subclasses sharing a base, that request has no single right
    /// answer, and guessing would be worse than missing.
    /// </para>
    /// </remarks>
    public sealed class ConfigCatalog
    {
        /// <summary>Folder under <c>Assets/Resources/</c> that holds config assets.</summary>
        public const string DefaultResourcesPath = "Configs";

        private readonly Dictionary<Type, Config> _configs = new Dictionary<Type, Config>();

        public ConfigCatalog(IReadOnlyList<Config> configs)
        {
            Guard.ThrowIfNull(configs, nameof(configs));

            for (var i = 0; i < configs.Count; i++)
            {
                var config = configs[i];
                if (config == null)
                    throw new InvalidOperationException(
                        $"Config at index {i} is null or destroyed — the catalog cannot index it.");

                var type = config.GetType();
                if (_configs.TryGetValue(type, out var existing))
                    throw new InvalidOperationException(
                        $"Two {type.Name} assets in the config catalog: '{existing.name}' and '{config.name}'. " +
                        "One asset per config type — a config describing many things holds a list instead.");

                _configs.Add(type, config);
            }
        }

        /// <summary>Loads every config asset under <c>Assets/Resources/{path}</c>.</summary>
        public static ConfigCatalog FromResources(string path = DefaultResourcesPath)
        {
            Guard.ThrowIfNullOrEmpty(path, nameof(path));
            Guard.ThrowIfNotMainThread();

            var catalog = new ConfigCatalog(Resources.LoadAll<Config>(path));
            Log.Debug(LogChannels.Core, $"Loaded {catalog.Count} config(s) from Resources/{path}.");
            return catalog;
        }

        public int Count => _configs.Count;

        /// <summary>Every config held, in no particular order. Diagnostics and tooling.</summary>
        public IEnumerable<Config> All => _configs.Values;

        /// <summary>
        /// Typed non-throwing lookup, for a config that is allowed to be absent —
        /// <c>DeveloperSettings</c>, or the one boot check that runs before anything can be
        /// injected.
        /// </summary>
        public bool TryGet<T>(out T config) where T : Config
        {
            if (_configs.TryGetValue(typeof(T), out var found))
            {
                config = (T)found;
                return true;
            }

            config = null;
            return false;
        }

        /// <summary>
        /// Non-throwing lookup by runtime type — what <c>ConfigResolver</c> calls, because a
        /// resolver is handed a <see cref="Type"/> and has no generic parameter to work with.
        /// Prefer <see cref="TryGet{T}"/> everywhere else: it hands back the typed asset rather
        /// than a <see cref="Config"/> the caller has to cast.
        /// </summary>
        public bool TryGet(Type type, out Config config)
        {
            Guard.ThrowIfNull(type, nameof(type));

            return _configs.TryGetValue(type, out config);
        }

        /// <summary>Typed lookup, or throws naming the asset you still have to create.</summary>
        public T Get<T>() where T : Config
        {
            if (_configs.TryGetValue(typeof(T), out var config)) return (T)config;

            throw new InvalidOperationException(
                $"No {typeof(T).Name} asset in the config catalog. Create one under " +
                $"Assets/Resources/{DefaultResourcesPath}/ — or check an existing one hasn't been " +
                "moved out of that folder, since only assets there are loaded.");
        }
    }
}

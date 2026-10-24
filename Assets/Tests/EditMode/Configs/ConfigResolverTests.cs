using System;
using System.Collections.Generic;
using Engine.Configs;
using Engine.DI;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Engine.Tests.EditMode.Configs
{
    [TestFixture]
    public sealed class ConfigResolverTests
    {
        private readonly List<Config> _created = new List<Config>();

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < _created.Count; i++)
                if (_created[i] != null)
                    Object.DestroyImmediate(_created[i]);

            _created.Clear();
        }

        [Test]
        public void Resolves_AConfigByItsType()
        {
            var crops = New<CropsConfig>();
            var resolver = new ConfigResolver(new ConfigCatalog(new Config[] { crops }));

            Assert.That(resolver.TryResolve(typeof(CropsConfig), out var value), Is.True);
            Assert.That(value, Is.SameAs(crops));
        }

        [Test]
        public void Passes_OnATypeThatIsNotAConfig()
        {
            var resolver = new ConfigResolver(new ConfigCatalog(Array.Empty<Config>()));

            Assert.That(resolver.TryResolve(typeof(string), out var value), Is.False);
            Assert.That(value, Is.Null);
        }

        [Test]
        public void Passes_OnAConfigTypeWithNoAsset()
        {
            var resolver = new ConfigResolver(new ConfigCatalog(Array.Empty<Config>()));

            // Passing rather than throwing is what lets the container collect every boot miss.
            Assert.That(resolver.TryResolve(typeof(CropsConfig), out _), Is.False);
        }

        [Test]
        public void InjectsIntoASystemThroughTheContainer()
        {
            var crops = New<CropsConfig>();
            var container = new Container();
            container.AddResolver(new ConfigResolver(new ConfigCatalog(new Config[] { crops })));

            var system = container.New<NeedsConfig>();

            Assert.That(system.Crops, Is.SameAs(crops));
        }

        [Test]
        public void MissingConfig_LandsInTheContainersFailFastReport()
        {
            var container = new Container();
            container.AddResolver(new ConfigResolver(new ConfigCatalog(Array.Empty<Config>())));

            var exception = Assert.Throws<DependencyException>(() => container.New<NeedsConfig>());

            Assert.That(exception.Missing, Is.EquivalentTo(new[] { typeof(CropsConfig) }));
        }

        [Test]
        public void Constructor_RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ConfigResolver(null));
        }

        private T New<T>() where T : Config
        {
            var config = ScriptableObject.CreateInstance<T>();
            config.name = typeof(T).Name;
            _created.Add(config);
            return config;
        }

#pragma warning disable 0649

        private sealed class CropsConfig : Config
        {
        }

        private sealed class NeedsConfig
        {
            [Inject] private CropsConfig _crops;

            public CropsConfig Crops => _crops;
        }

#pragma warning restore 0649
    }
}

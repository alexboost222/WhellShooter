using System;
using System.Collections.Generic;
using System.Linq;
using Engine.Configs;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Engine.Tests.EditMode.Configs
{
    [TestFixture]
    public sealed class ConfigCatalogTests
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
        public void Get_ReturnsTheConfigOfThatType()
        {
            var crops = New<CropsConfig>("Crops");
            var catalog = new ConfigCatalog(new Config[] { crops, New<EconomyConfig>("Economy") });

            Assert.That(catalog.Get<CropsConfig>(), Is.SameAs(crops));
        }

        [Test]
        public void TryGetOfT_HandsBackTheTypedAsset()
        {
            var crops = New<CropsConfig>("Crops");
            var catalog = new ConfigCatalog(new Config[] { crops });

            Assert.That(catalog.TryGet<CropsConfig>(out var found), Is.True);
            Assert.That(found, Is.SameAs(crops), "typed, so the caller has nothing to cast");
        }

        [Test]
        public void TryGetOfT_IsFalseAndNullForAMissingConfig()
        {
            var catalog = new ConfigCatalog(new Config[] { New<CropsConfig>("Crops") });

            Assert.That(catalog.TryGet<EconomyConfig>(out var found), Is.False);
            Assert.That(found, Is.Null);
        }

        [Test]
        public void Get_ThrowsForAMissingConfig_AndSaysWhereToPutIt()
        {
            var catalog = new ConfigCatalog(Array.Empty<Config>());

            var exception = Assert.Throws<InvalidOperationException>(() => catalog.Get<CropsConfig>());

            Assert.That(exception.Message, Does.Contain(nameof(CropsConfig)));
            Assert.That(exception.Message, Does.Contain("Resources/Configs"));
        }

        [Test]
        public void TryGet_ReportsPresenceWithoutThrowing()
        {
            var crops = New<CropsConfig>("Crops");
            var catalog = new ConfigCatalog(new Config[] { crops });

            Assert.That(catalog.TryGet(typeof(CropsConfig), out var found), Is.True);
            Assert.That(found, Is.SameAs(crops));
            Assert.That(catalog.TryGet(typeof(EconomyConfig), out var missing), Is.False);
            Assert.That(missing, Is.Null);
        }

        [Test]
        public void TwoAssetsOfTheSameType_ThrowAndNameBoth()
        {
            var first = New<CropsConfig>("Crops");
            var second = New<CropsConfig>("CropsCopy");

            var exception = Assert.Throws<InvalidOperationException>(
                () => new ConfigCatalog(new Config[] { first, second }));

            Assert.That(exception.Message, Does.Contain("Crops"));
            Assert.That(exception.Message, Does.Contain("CropsCopy"));
        }

        [Test]
        public void Lookup_IsByExactType_ASubclassDoesNotAnswerForItsBase()
        {
            var catalog = new ConfigCatalog(new Config[] { New<SeasonalCropsConfig>("Seasonal") });

            Assert.That(catalog.TryGet(typeof(CropsConfig), out _), Is.False);
            Assert.That(catalog.Get<SeasonalCropsConfig>(), Is.Not.Null);
        }

        [Test]
        public void Count_AndAll_ReflectWhatWasIndexed()
        {
            var crops = New<CropsConfig>("Crops");
            var economy = New<EconomyConfig>("Economy");

            var catalog = new ConfigCatalog(new Config[] { crops, economy });

            Assert.That(catalog.Count, Is.EqualTo(2));
            Assert.That(catalog.All.ToArray(), Is.EquivalentTo(new Config[] { crops, economy }));
        }

        [Test]
        public void EmptyCatalog_IsLegal()
        {
            var catalog = new ConfigCatalog(Array.Empty<Config>());

            Assert.That(catalog.Count, Is.Zero);
            Assert.That(catalog.All, Is.Empty);
        }

        [Test]
        public void NullList_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ConfigCatalog(null));
        }

        [Test]
        public void NullEntry_ThrowsNamingTheIndex()
        {
            var exception = Assert.Throws<InvalidOperationException>(
                () => new ConfigCatalog(new Config[] { New<CropsConfig>("Crops"), null }));

            Assert.That(exception.Message, Does.Contain("index 1"));
        }

        [Test]
        public void DestroyedEntry_IsRejectedRatherThanIndexed()
        {
            var doomed = New<CropsConfig>("Doomed");
            Object.DestroyImmediate(doomed);

            // Destroyed but not null in C# terms — indexing it would hand out a dead asset.
            Assert.Throws<InvalidOperationException>(() => new ConfigCatalog(new Config[] { doomed }));
        }

        [Test]
        public void FromResources_LoadsTheConfiguredFolderWithoutThrowing()
        {
            // No config assets exist yet; this pins the path constant and the empty case.
            var catalog = ConfigCatalog.FromResources();

            Assert.That(catalog, Is.Not.Null);
            Assert.That(ConfigCatalog.DefaultResourcesPath, Is.EqualTo("Configs"));
        }

        [Test]
        public void FromResources_RejectsAnEmptyPath()
        {
            Assert.Throws<ArgumentException>(() => ConfigCatalog.FromResources(string.Empty));
        }

        private T New<T>(string name) where T : Config
        {
            var config = ScriptableObject.CreateInstance<T>();
            config.name = name;
            _created.Add(config);
            return config;
        }

        // --- fixtures ----------------------------------------------------------------------

        private class CropsConfig : Config
        {
        }

        private sealed class SeasonalCropsConfig : CropsConfig
        {
        }

        private sealed class EconomyConfig : Config
        {
        }
    }
}

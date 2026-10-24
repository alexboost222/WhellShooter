using System.Collections.Generic;
using System.Linq;
using Engine.Configs;
using NUnit.Framework;
using UnityEngine;

namespace Engine.Tests.EditMode.Configs
{
    /// <summary>
    /// <see cref="Config.Entries"/> — the enumeration the drawers, the validator and the entity
    /// inspector all read.
    /// </summary>
    /// <remarks>
    /// Tested against a fixture subclass rather than the real configs, so that adding a crop does not
    /// turn a test about enumeration red. What is asserted is the contract every override has to
    /// keep: index in order, name as authored, and the id being <see cref="ConfigIds.Hash"/> of that
    /// name and nothing else.
    /// </remarks>
    [TestFixture]
    public sealed class ConfigEntryTests
    {
        private FixtureConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<FixtureConfig>();
            _config.Rows = new[] { "bucket", "spade", string.Empty };
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void RowsComeOutInOrder()
        {
            var entries = _config.Entries.ToArray();

            Assert.That(entries.Select(e => e.Index), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(entries[0].Name, Is.EqualTo("bucket"));
            Assert.That(entries[1].Name, Is.EqualTo("spade"));
        }

        [Test]
        public void TheIdIsTheHashOfTheName()
        {
            var entry = _config.Entries.First();

            Assert.That(entry.Id, Is.EqualTo(ConfigIds.Hash("bucket")));
        }

        /// <summary>
        /// A half-finished row is a finding for the validator, not an exception during enumeration —
        /// which would make the inspector unable to draw the very asset you are fixing.
        /// </summary>
        [Test]
        public void AnUnnamedRowEnumeratesWithAZeroId()
        {
            var entry = _config.Entries.Last();

            Assert.That(entry.Name, Is.Empty);
            Assert.That(entry.Id, Is.Zero, "zero is how 'no id' is spelled everywhere else");
        }

        [Test]
        public void AConfigThatIsJustNumbersHasNoEntries()
        {
            var plain = ScriptableObject.CreateInstance<PlainConfig>();

            try
            {
                Assert.That(plain.Entries, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(plain);
            }
        }

        private sealed class FixtureConfig : Config
        {
            internal string[] Rows = System.Array.Empty<string>();

            public override IEnumerable<ConfigEntry> Entries
            {
                get
                {
                    for (var i = 0; i < Rows.Length; i++)
                    {
                        yield return new ConfigEntry(i, Rows[i], Rows[i]);
                    }
                }
            }
        }

        private sealed class PlainConfig : Config
        {
        }
    }
}

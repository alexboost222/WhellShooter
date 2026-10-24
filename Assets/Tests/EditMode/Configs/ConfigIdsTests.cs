using Engine.Configs;
using NUnit.Framework;

namespace Engine.Tests.EditMode.Configs
{
    /// <summary>
    /// The name→id hash that components store instead of a reference.
    /// </summary>
    /// <remarks>
    /// The property that matters is <b>stability</b>: this number ends up in save files and on the
    /// wire, so it has to be the same on every machine and in every process. That is the whole
    /// reason it is not <c>string.GetHashCode()</c>, which is randomised per process on .NET Core —
    /// and the reason a test can assert a literal at all.
    /// </remarks>
    [TestFixture]
    public sealed class ConfigIdsTests
    {
        [Test]
        public void TheSameNameAlwaysHashesTheSame()
        {
            Assert.That(ConfigIds.Hash("bucket"), Is.EqualTo(ConfigIds.Hash("bucket")));
        }

        /// <summary>
        /// A literal, deliberately. If a change to the algorithm makes this fail, that change has
        /// invalidated every save file in existence, and finding that out here is the point.
        /// </summary>
        [Test]
        public void TheHashIsFnv1a_AndPinned()
        {
            unchecked
            {
                Assert.That(ConfigIds.Hash("bucket"), Is.EqualTo((int)0xF3788F37));
            }
        }

        [Test]
        public void DifferentNamesHashDifferently()
        {
            Assert.That(ConfigIds.Hash("bucket"), Is.Not.EqualTo(ConfigIds.Hash("well")));
        }

        [Test]
        public void CaseMatters()
        {
            Assert.That(ConfigIds.Hash("Bucket"), Is.Not.EqualTo(ConfigIds.Hash("bucket")));
        }

        [Test]
        public void AnEmptyNameIsNoId()
        {
            Assert.That(ConfigIds.Hash(null), Is.Zero);
            Assert.That(ConfigIds.Hash(string.Empty), Is.Zero);
        }
    }
}

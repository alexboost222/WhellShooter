using Engine.DI;
using Engine.Messages;
using Engine.Runtime;
using NUnit.Framework;

namespace Engine.Tests.EditMode.Runtime
{
    /// <summary>
    /// <see cref="FeatureManifest"/> — the record of what each feature installed, which nothing kept
    /// before.
    /// </summary>
    /// <remarks>
    /// A feature instance is constructed and discarded during boot, and its registrations scatter
    /// into the container, the Runner and the snapshot registry, none of which keeps a
    /// back-reference. So the two things worth asserting are that a registration lands on the right
    /// record, and that <see cref="FeatureManifest.Reset"/> works — because domain reload is off in
    /// this editor and a second play session would otherwise report both.
    /// </remarks>
    [TestFixture]
    public sealed class FeatureManifestTests
    {
        private Container _container;
        private Runner _runner;

        [SetUp]
        public void SetUp()
        {
            _container = new Container();
            _runner = new Runner(new MessageBus());
            FeatureManifest.Reset();
        }

        [TearDown]
        public void TearDown() => FeatureManifest.Reset();

        [Test]
        public void AFeatureGetsARecordInInstallOrder()
        {
            new First().Install(_container, _runner);
            new Second().Install(_container, _runner);

            Assert.That(FeatureManifest.All, Has.Count.EqualTo(2));
            Assert.That(FeatureManifest.All[0].Name, Is.EqualTo(nameof(First)));
            Assert.That(FeatureManifest.All[0].Order, Is.Zero);
            Assert.That(FeatureManifest.All[1].Name, Is.EqualTo(nameof(Second)));
            Assert.That(FeatureManifest.All[1].Order, Is.EqualTo(1));
        }

        [Test]
        public void ResetForgetsEverything()
        {
            new First().Install(_container, _runner);
            FeatureManifest.Reset();

            Assert.That(FeatureManifest.All, Is.Empty);
        }

        /// <summary>
        /// The composition root adds a handful of steps of its own, outside any feature. Those belong
        /// to nobody, and attributing them to whichever feature ran last would be worse than dropping
        /// them.
        /// </summary>
        [Test]
        public void ARegistrationOutsideAnyFeatureIsDropped()
        {
            new First().Install(_container, _runner);

            // Install has returned, so the manifest is between features.
            Assert.That(FeatureManifest.All[0].Total, Is.Zero);
            Assert.DoesNotThrow(() => new First().Install(_container, _runner));
        }

        private sealed class First : Feature
        {
            public override void Register()
            {
            }
        }

        private sealed class Second : Feature
        {
            public override void Register()
            {
            }
        }
    }
}

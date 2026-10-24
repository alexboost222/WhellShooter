using System;
using Engine.DI;
using Engine.Ecs;
using NUnit.Framework;
using Scellecs.Morpeh;

namespace Engine.Tests.EditMode.Ecs
{
    /// <summary>
    /// Runs against a headless Morpeh world — no scene, no play mode (Docs/01).
    /// </summary>
    [TestFixture]
    public sealed class StashResolverTests
    {
        private World _world;
        private StashResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _world = World.Create();
            _resolver = new StashResolver(_world);
        }

        [TearDown]
        public void TearDown()
        {
            _world?.Dispose();
            _world = null;
        }

        [Test]
        public void Resolves_AStashToTheWorldsOwn()
        {
            Assert.That(_resolver.TryResolve(typeof(Stash<Growth>), out var value), Is.True);
            Assert.That(value, Is.SameAs(_world.GetStash<Growth>()));
        }

        [Test]
        public void RepeatedRequests_ReturnTheSameStash()
        {
            _resolver.TryResolve(typeof(Stash<Growth>), out var first);
            _resolver.TryResolve(typeof(Stash<Growth>), out var second);

            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void DifferentComponents_GetDifferentStashes()
        {
            _resolver.TryResolve(typeof(Stash<Growth>), out var growth);
            _resolver.TryResolve(typeof(Stash<Ripe>), out var ripe);

            Assert.That(ripe, Is.Not.SameAs(growth));
        }

        [Test]
        public void Passes_OnAnythingThatIsNotAStash()
        {
            Assert.That(_resolver.TryResolve(typeof(string), out _), Is.False);
            Assert.That(_resolver.TryResolve(typeof(World), out _), Is.False);
            Assert.That(_resolver.TryResolve(typeof(System.Collections.Generic.List<Growth>), out _), Is.False);
        }

        [Test]
        public void WiresASystemsStashesThroughTheContainer()
        {
            var container = new Container();
            container.AddResolver(_resolver);

            var system = container.New<GrowthSystem>();

            Assert.That(system.Growth, Is.SameAs(_world.GetStash<Growth>()));
            Assert.That(system.Ripe, Is.SameAs(_world.GetStash<Ripe>()));
        }

        [Test]
        public void ResolvedStash_IsTheOneTheWorldActuallyUses()
        {
            var container = new Container();
            container.AddResolver(_resolver);
            var system = container.New<GrowthSystem>();

            var entity = _world.CreateEntity();
            system.Growth.Set(entity, new Growth { Stage = 2 });
            _world.Commit();

            Assert.That(_world.GetStash<Growth>().Get(entity).Stage, Is.EqualTo(2));
        }

        [Test]
        public void Constructor_RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new StashResolver(null));
        }

#pragma warning disable 0649

        private sealed class GrowthSystem
        {
            [Inject] private Stash<Growth> _growth;
            [Inject] private Stash<Ripe> _ripe;

            public Stash<Growth> Growth => _growth;
            public Stash<Ripe> Ripe => _ripe;
        }

#pragma warning restore 0649

        private struct Growth : IComponent
        {
            public int Stage;
        }

        private struct Ripe : IComponent
        {
        }
    }
}

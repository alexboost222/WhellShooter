using Engine.Configs;
using Engine.DI;
using Engine.Ecs;
using Engine.Messages;
using Engine.Runtime;
using Features.Map.Configs;
using NUnit.Framework;
using Scellecs.Morpeh;
using UnityEngine;

namespace Game.Tests.EditMode.Features.Meta
{
    /// <summary>
    /// The real feature list against a headless world, wired the way Bootstrap wires it. Boot is the
    /// validation pass, so the first thing worth proving is that the graph builds at all.
    /// </summary>
    public sealed class FeaturesTests
    {
        private World _world;
        private Runner _runner;
        private MapConfig _mapConfig;

        [SetUp]
        public void SetUp()
        {
            _world = World.Create();
            var bus = new MessageBus();
            _runner = new Runner(bus);

            var container = new Container();
            _mapConfig = ScriptableObject.CreateInstance<MapConfig>();
            var catalog = new ConfigCatalog(new Config[] { _mapConfig });
            container.Register(catalog);
            container.Register(_world);
            container.Register(bus);
            container.Register(_runner);
            container.AddResolver(new ConfigResolver(catalog));
            container.AddResolver(new StashResolver(_world));
            container.AddResolver(new MessagePipeResolver(bus));

            global::Meta.Features.Register(container, _runner);
            _runner.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
            Object.DestroyImmediate(_mapConfig);
        }

        [Test]
        public void EveryFeatureRegisters()
        {
            Assert.That(global::Meta.Features.Count, Is.GreaterThan(0));
            Assert.That(_runner.StepCount(RunnerPhase.FixedUpdate), Is.GreaterThan(0));
        }

        [Test]
        public void AnEntityTaggedDestroy_IsGoneAfterOneTick()
        {
            var doomed = _world.CreateEntity();
            _world.GetStash<Destroy>().Set(doomed);
            var survivor = _world.CreateEntity();
            _world.GetStash<Probe>().Set(survivor);
            _world.Commit();

            _runner.FixedTick(1f / 50f);

            Assert.That(doomed.IsDisposed(), Is.True);
            Assert.That(survivor.IsDisposed(), Is.False);
        }

        private struct Probe : IComponent
        {
        }
    }
}
